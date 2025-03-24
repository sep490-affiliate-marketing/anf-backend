﻿using ANF.Core;
using ANF.Core.Commons;
using ANF.Core.Enums;
using ANF.Core.Models.Entities;
using ANF.Core.Models.Requests;
using ANF.Core.Services;
using ANF.Infrastructure.Helpers;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ANF.Service
{
    public class TransactionService(IUnitOfWork unitOfWork,
        IPaymentService paymentService,
        ILogger<TransactionService> logger,
        IMapper mapper,
        IUserClaimsService userClaimsService) : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IPaymentService _paymentService = paymentService;
        private readonly ILogger<TransactionService> _logger = logger;
        private readonly IMapper _mapper = mapper;
        private readonly IUserClaimsService _userClaimsService = userClaimsService;
        private readonly string _cancelUrl = string.Empty;  // Cancel url is the default page of platform
        private readonly string _returnUrl = "https://payos.vn";    // Payment successful page

        public async Task<string> CancelTransaction(long transactionId)
        {
            try
            {
                var walletHistoryRepository = _unitOfWork.GetRepository<WalletHistory>();
                var transactionRepository = _unitOfWork.GetRepository<Transaction>();

                var transaction = await transactionRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == transactionId);
                if (transaction is null)
                    throw new KeyNotFoundException("Transaction does not exist!");

                var walletHistory = await walletHistoryRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(wh => wh.TransactionId == transactionId);
                if (walletHistory is null)
                    throw new KeyNotFoundException("No record of wallet history with transaction!");

                walletHistoryRepository.Delete(walletHistory);
                transactionRepository.Delete(transaction);
                await _unitOfWork.SaveAsync();

                return _cancelUrl;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<string> ConfirmPayment(long transactionId)
        {
            try
            {
                var walletHistoryRepository = _unitOfWork.GetRepository<WalletHistory>();
                var transactionRepository = _unitOfWork.GetRepository<Transaction>();
                var walletRepository = _unitOfWork.GetRepository<Wallet>();

                var transaction = await transactionRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == transactionId) ??
                            throw new KeyNotFoundException("Transaction does not exist!");

                var walletHistory = await walletHistoryRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(wh => wh.TransactionId == transactionId) ??
                            throw new KeyNotFoundException("No record of wallet history with this transaction!");

                var wallet = await walletRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.UserCode == transaction.UserCode) ??
                            throw new KeyNotFoundException("Wallet does not exist!");

                // Add the money to the wallet
                wallet.Balance += transaction.Amount;
                walletHistory.BalanceType = walletHistory.CurrentBalance == wallet.Balance
                            ? null
                            : walletHistory.CurrentBalance < wallet.Balance;
                transaction.Status = Core.Enums.TransactionStatus.Success;

                walletHistoryRepository.Update(walletHistory);
                walletRepository.Update(wallet);
                transactionRepository.Update(transaction);
                await _unitOfWork.SaveAsync();

                return _returnUrl;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message);
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<string> CreateDeposit(DepositRequest request)
        {
            try
            {
                var currentUserCode = _userClaimsService.GetClaim(ClaimConstants.NameId);
                var walletRepository = _unitOfWork.GetRepository<Wallet>();
                var transactionRepository = _unitOfWork.GetRepository<Transaction>();
                var walletHistoryRepository = _unitOfWork.GetRepository<WalletHistory>();

                if (string.IsNullOrEmpty(currentUserCode))
                    throw new UnauthorizedAccessException("Cannot perform the action because user's code is empty!");

                var wallet = await walletRepository.GetAll()
                    .AsNoTracking()
                    .Where(w => w.UserCode == currentUserCode)
                    .FirstOrDefaultAsync() ?? throw new KeyNotFoundException("User's wallet does not exist!");

                if (!wallet.IsActive)
                    throw new Exception("The wallet is inactive! Please activate it.");

                var transaction = new Transaction
                {
                    Id = IdHelper.GenerateTransactionId(),
                    UserCode = currentUserCode,
                    WalletId = wallet.Id,
                    Reason = $"Nạp số tiền {request.Amount} vào ví",
                    Amount = request.Amount,
                    CreatedAt = DateTime.Now,
                    Status = Core.Enums.TransactionStatus.Pending,
                };
                transactionRepository.Add(transaction);
                if (await _unitOfWork.SaveAsync() <= 0)
                    throw new Exception("An error occured when storing the data!"); //TODO: Cần check lại error handle ở đây

                var walletHistory = new WalletHistory
                {
                    TransactionId = transaction.Id,
                    CurrentBalance = wallet.Balance
                };
                walletHistoryRepository.Add(walletHistory);
                if (await _unitOfWork.SaveAsync() <= 0)
                    throw new Exception("An error occured when storing the data!"); //TODO: Cần check lại error handle ở đây

                var paymentLink = await _paymentService.CreatePaymentLink(transaction.Id);
                return paymentLink;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message, ex.InnerException);
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> CreateWithdrawalRequest(WithdrawalRequest request)
        {
            try
            {
                var currentUserCode = _userClaimsService.GetClaim(ClaimConstants.NameId);
                if (string.IsNullOrEmpty(currentUserCode))
                    throw new UnauthorizedAccessException("Cannot access to this endpoint because user's code is empty!");

                var transactionRepository = _unitOfWork.GetRepository<Transaction>();
                var transaction = new Transaction
                {
                    Id = IdHelper.GenerateTransactionId(),
                    Amount = request.Amount,
                    Reason = request.Reason,
                    UserCode = currentUserCode,
                    Status = Core.Enums.TransactionStatus.Pending,
                };
                transactionRepository.Add(transaction);
                return await _unitOfWork.SaveAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message, ex.StackTrace);
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateWithdrawalRequestStatus(long transactionId, string status, long bankingNo)
        {
            try
            {
                var transactionRepository = _unitOfWork.GetRepository<Transaction>();
                var batchPaymentRepository = _unitOfWork.GetRepository<BatchPayment>();
                var userBankRepository = _unitOfWork.GetRepository<UserBank>();

                if (!Enum.TryParse(status, true, out TransactionStatus transactionStatus))
                    throw new ArgumentException("Invalid transaction status!");

                var transaction = await transactionRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == transactionId) ??
                    throw new KeyNotFoundException("Transaction does not exist!");

                var currentStatus = (TransactionStatus)Enum.Parse(typeof(TransactionStatus), status);
                if (currentStatus == TransactionStatus.Canceled)
                {
                    transactionRepository.Delete(transaction);
                    return await _unitOfWork.SaveAsync() > 0;
                }
                else if (currentStatus == TransactionStatus.Success)
                {
                    var userCode = transaction.UserCode;
                    transaction.Status = currentStatus;
                    var bankingInfo = await userBankRepository.GetAll()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(ub => ub.UserCode == userCode && ub.BankingNo == bankingNo) ??
                            throw new KeyNotFoundException("Banking information does not exist!");

                    var payment = new BatchPayment
                    {
                        TransactionId = transactionId,
                        FromAccount = string.Empty, // Tài khoản ngân hàng của hệ thống
                        Amount = transaction.Amount,
                        BeneficiaryAccount = bankingInfo.BankingNo.ToString(),
                        BeneficiaryBankCode = string.Empty, // Lấy dữ liệu từ template 
                        BeneficiaryBankName = string.Empty, // Lấy dữ liệu từ template
                        Reason = transaction.Reason ?? string.Empty,
                    };
                    batchPaymentRepository.Add(payment);
                    return await _unitOfWork.SaveAsync() > 0;
                }
                else throw new Exception("Exception");  // Sửa lại message exception
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message, ex.StackTrace);
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }
    }
}
