﻿using ANF.Core;
using ANF.Core.Commons;
using ANF.Core.Enums;
using ANF.Core.Exceptions;
using ANF.Core.Models.Entities;
using ANF.Core.Models.Requests;
using ANF.Core.Models.Responses;
using ANF.Core.Services;
using ANF.Infrastructure.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

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

                return PaymentRedirectedPage.PaymentCanceledPage;
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
                transaction.Status = TransactionStatus.Success;

                walletHistoryRepository.Update(walletHistory);
                walletRepository.Update(wallet);
                transactionRepository.Update(transaction);
                await _unitOfWork.SaveAsync();

                return PaymentRedirectedPage.PaymentSuccessfulPage + $"/{transactionId}";
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message);
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<string> ConfirmSubscriptionPurchase(long transactionId)
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

                var wallet = await walletRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.UserCode == transaction.UserCode) ??
                            throw new KeyNotFoundException("Wallet does not exist!");

                var walletHistory = await walletHistoryRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(wh => wh.TransactionId == transactionId) ??
                            throw new KeyNotFoundException("No record of wallet history with this transaction!");

                wallet.Balance -= transaction.Amount;
                walletHistory.BalanceType = walletHistory.CurrentBalance == wallet.Balance
                            ? null
                            : walletHistory.CurrentBalance < wallet.Balance;
                transaction.Status = TransactionStatus.Success;

                walletHistoryRepository.Update(walletHistory);
                walletRepository.Update(wallet);
                transactionRepository.Update(transaction);
                await _unitOfWork.SaveAsync();

                return PaymentRedirectedPage.PaymentSuccessfulPage + $"/{transactionId}";
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
                    Reason = $"Deposit into wallet {wallet.Id}",
                    Amount = request.Amount,
                    CreatedAt = DateTime.Now,
                    Status = TransactionStatus.Pending,
                };
                transactionRepository.Add(transaction);

                var walletHistory = new WalletHistory
                {
                    TransactionId = transaction.Id,
                    CurrentBalance = wallet.Balance
                };
                walletHistoryRepository.Add(walletHistory);

                //WARNING: Khi tạo link thanh toán gặp lỗi (vượt độ dài ký tự, key k đúng...)
                // thì data đã được save thành công trong database
                //TODO: Trong tương lai có thể tìm 1 workflow khác để handle dữ liệu tốt hơn.
                var affectedRows = await _unitOfWork.SaveAsync();
                if (affectedRows <= 0)
                    throw new Exception("An error occured when storing the data in database!");

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

        public async Task<string> CreatePaymentLinkForSubscription(SubscriptionPurchaseRequest request)
        {
            try
            {
                var currentUserCode = _userClaimsService.GetClaim(ClaimConstants.NameId);

                var walletRepository = _unitOfWork.GetRepository<Wallet>();
                var transactionRepository = _unitOfWork.GetRepository<Transaction>();
                var walletHistoryRepository = _unitOfWork.GetRepository<WalletHistory>();
                var subscriptionRepository = _unitOfWork.GetRepository<Subscription>();

                if (string.IsNullOrEmpty(currentUserCode))
                    throw new UnauthorizedAccessException("Cannot perform the action because user's code is empty!");

                var wallet = await walletRepository.GetAll()
                    .AsNoTracking()
                    .Where(w => w.UserCode == currentUserCode)
                    .FirstOrDefaultAsync() ?? throw new KeyNotFoundException("User's wallet does not exist!");

                if (!wallet.IsActive)
                    throw new InactiveWalletException("Cannot proceed the transaction! The wallet is inactive");

                var isExisted = await subscriptionRepository.GetAll()
                    .AsNoTracking()
                    .AnyAsync(s => s.Id == request.Id);
                if (!isExisted)
                    throw new KeyNotFoundException("Subscription does not exist!");

                var transaction = new Transaction
                {
                    Id = IdHelper.GenerateTransactionId(),
                    UserCode = currentUserCode,
                    WalletId = wallet.Id,
                    Reason = $"Đăng ký subscription {request.Id}",
                    Amount = request.Price,
                    CreatedAt = DateTime.Now,
                    Status = TransactionStatus.Pending,
                };
                transactionRepository.Add(transaction);

                var walletHistory = new WalletHistory
                {
                    TransactionId = transaction.Id,
                    CurrentBalance = wallet.Balance
                };
                walletHistoryRepository.Add(walletHistory);
                if (await _unitOfWork.SaveAsync() <= 0)
                    throw new Exception("An error occured when storing the data in database!");

                var paymentLink = await _paymentService.CreatePaymentLinkForSubscription(transaction.Id);
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

                var userBankRepository = _unitOfWork.GetRepository<UserBank>();
                var transactionRepository = _unitOfWork.GetRepository<Transaction>();
                var walletRepository = _unitOfWork.GetRepository<Wallet>();
                var walletHistory = _unitOfWork.GetRepository<WalletHistory>();
                var batchPaymentRepository = _unitOfWork.GetRepository<BatchPayment>();
                var campaignRepository = _unitOfWork.GetRepository<Campaign>();

                var bankAccount = await userBankRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BankingNo == request.BankingNo)
                    ?? throw new KeyNotFoundException("Banking number does not exist!");

                var wallet = await walletRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.UserCode == currentUserCode)
                    ?? throw new KeyNotFoundException("Wallet does not exist!");

                // Check withdrawal amount and current balance in the wallet
                if (request.Amount > wallet.Balance)
                    throw new ArgumentException("Withdrawal amount exceeds current balance in wallet!");

                var user = await _unitOfWork.GetRepository<User>()
                    .GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserCode == currentUserCode)
                        ?? throw new KeyNotFoundException("User does not exist!");
                // Check withdrawal amount and campaign's budget (For advertiser)
                if (user is not null && user.Role == UserRoles.Advertiser)
                {
                    var totalBudget = await campaignRepository.GetAll()
                        .AsNoTracking()
                        .Where(c => c.AdvertiserCode == user.UserCode &&
                            (c.Status == CampaignStatus.Verified || c.Status == CampaignStatus.Started || c.Status == CampaignStatus.Pending))
                        .SumAsync(c => c.Balance);
                    //if (request.Amount >= totalBudget)
                    //    throw new ArgumentException("Withdrawal amount exceeds current total budget in campaigns!");
                    if (wallet.Balance - request.Amount <= totalBudget)
                        throw new ArgumentException("Withdrawal amount exceeds current total budget in campaigns!");

                    //TODO: Cần review lại campaign balance
                    //if (wallet.Balance - request.Amount <= totalBudget)
                    //    throw new ArgumentException("Withdrawal amount exceeds current total budget in campaigns!");
                }
                var transaction = new Transaction
                {
                    Id = IdHelper.GenerateTransactionId(),
                    WalletId = wallet.Id,
                    Amount = request.Amount,
                    Reason = $"Withdraw money from wallet {wallet.Id} to account {request.BankingNo}",
                    UserCode = currentUserCode,
                    CurrentBankingNo = request.BankingNo,
                    Status = TransactionStatus.Pending,
                    CreatedAt = DateTime.Now,
                    IsWithdrawal = true,    // flag to recognize this is a withdrawal transaction
                };

                // Store the current balance in wallet history
                var walletHistoryData = new WalletHistory
                {
                    TransactionId = transaction.Id,
                    CurrentBalance = wallet.Balance
                    // Don't need to set BalanceType
                };

                var batchPaymentData = new BatchPayment
                {
                    TransactionId = transaction.Id,
                    Amount = request.Amount,
                    Reason = transaction.Reason,
                    FromAccount = PlatformSourceBankingNo.PlatformBankingNo,
                    BeneficiaryAccount = request.BankingNo,
                    BeneficiaryName = bankAccount.UserName,
                    BeneficiaryBankCode = request.BeneficiaryBankCode,
                    BeneficiaryBankName = request.BeneficiaryBankName,
                };
                transactionRepository.Add(transaction);
                batchPaymentRepository.Add(batchPaymentData);
                walletHistory.Add(walletHistoryData);

                return await _unitOfWork.SaveAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message, ex.StackTrace);
                await _unitOfWork.RollbackAsync();
                throw;
            }
        }

        public async Task<IActionResult> ExportBatchPaymentData(List<ExportedBatchDataResponse> data)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var fileContents = await Task.Run(() =>
                {
                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add("Batch Payment Data");
                    // Header row
                    string[] headers =
                    {
                        "Reference number\nSố tham chiếu\n(Nhập ký tự số/chữ. Tránh các ký tự đặc biệt)",
                        "From Account\nTài khoản chuyển tiền\n(Nhập tối đa 14 ký tự số)",
                        "Amount (VND)\nSố tiền\n(Chỉ nhập ký tự số)",
                        "Beneficiary name\nTên người thụ hưởng\n(Nhập ký tự số và chữ)",
                        "Beneficiary Account\nTài khoản đích\n(Nhập ký tự số/chữ)",
                        "Beneficiary Account\nTài khoản đích\n(Nhập ký tự số/chữ)",
                        "Beneficiary Bank code\nMã ngân hàng hưởng\n(Nhập 8 ký tự số)",
                        "Beneficiary Bank name\nTên ngân hàng hưởng"
                    };

                    for (int col = 0; col < headers.Length; col++)
                    {
                        worksheet.Cells[1, col + 1].Value = headers[col];
                        worksheet.Cells[1, col + 1].Style.Font.Bold = true;
                    }

                    // Populate data rows
                    for (int row = 0; row < data.Count; row++)
                    {
                        var item = data[row];
                        worksheet.Cells[row + 2, 1].Value = item.TransactionId;
                        worksheet.Cells[row + 2, 2].Value = item.FromAccount;
                        worksheet.Cells[row + 2, 3].Value = item.Amount;
                        worksheet.Cells[row + 2, 4].Value = item.BeneficiaryName;
                        worksheet.Cells[row + 2, 5].Value = item.BeneficiaryAccount;
                        worksheet.Cells[row + 2, 6].Value = item.Reason;
                        worksheet.Cells[row + 2, 7].Value = item.BeneficiaryBankCode;
                        worksheet.Cells[row + 2, 8].Value = item.BeneficiaryBankName;
                    }

                    // Auto-fit columns
                    worksheet.Cells.AutoFitColumns();

                    return package.GetAsByteArray();
                });

                return new FileContentResult(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    FileDownloadName = "BatchPaymentData.xlsx"
                };
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e.Message, e.StackTrace);
                throw;
            }
        }

        public async Task<PaginationResponse<ExportedBatchDataResponse>> GetBatchPaymentDataForExporting(PaginationRequest request,
            string fromDate,
            string toDate)
        {
            var batchPaymentRepository = _unitOfWork.GetRepository<BatchPayment>();
            if (string.IsNullOrEmpty(fromDate) || string.IsNullOrEmpty(toDate))
                throw new ArgumentException("From date and to date must not be empty!");

            DateTime from = DateTime.Parse(fromDate ?? throw new ArgumentException(nameof(fromDate)));
            DateTime to = DateTime.Parse(toDate ?? throw new ArgumentException(nameof(toDate)));

            var query = batchPaymentRepository.GetAll()
                .AsNoTracking()
                .Where(bp => bp.Date >= from && bp.Date <= to);
            var totalRecord = await query.CountAsync();

            var data = await query
                .Skip((request.pageNumber - 1) * request.pageSize)
                .Take(request.pageSize)
                .ToListAsync();
            if (!data.Any()) throw new NoDataRetrievalException("No data of batch payment!");
            var response = _mapper.Map<List<ExportedBatchDataResponse>>(data);
            return new PaginationResponse<ExportedBatchDataResponse>(response, totalRecord,
                request.pageNumber,
                request.pageSize);
        }

        public Task<decimal> GetCurrentBalanceInWallet(string userCode)
        {
            var walletRepository = _unitOfWork.GetRepository<Wallet>();
            var wallet = walletRepository.GetAll()
                .AsNoTracking()
                .FirstOrDefault(w => w.UserCode == userCode)
                ?? throw new KeyNotFoundException("Wallet does not exist!");

            return Task.FromResult(wallet.Balance);
        }

        public async Task<TransactionResponse?> GetTransactionById(long transactionId)
        {
            var transactionRepository = _unitOfWork.GetRepository<Transaction>();

            var transaction = await transactionRepository.GetAll()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId)
                ?? throw new KeyNotFoundException("Transaction does not exist!");
            return _mapper.Map<TransactionResponse>(transaction);
        }

        public async Task<PaginationResponse<UserTransactionResponse>> GetTransactionOfUser(string userCode,
            int pageNumber,
            int pageSize)
        {
            try
            {
                var transactionRepository = _unitOfWork.GetRepository<Transaction>();

                var query = transactionRepository.GetAll()
                    .AsNoTracking()
                    .Where(t => t.UserCode == userCode)
                    .OrderByDescending(t => t.CreatedAt);
                var count = await query.CountAsync();
                var transactions = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync()
                    ?? throw new NoDataRetrievalException("No data of transactions!");

                var data = _mapper.Map<List<UserTransactionResponse>>(transactions);
                return new PaginationResponse<UserTransactionResponse>(data, count, pageNumber, pageSize);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e.StackTrace);
                throw;
            }
        }

        public async Task<PaginationResponse<WithdrawalResponse>> GetWithdrawalRequests(int pageNumber, int pageSize,
            string fromDate,
            string toDate)
        {
            var transactionRepository = _unitOfWork.GetRepository<Transaction>();
            if (string.IsNullOrEmpty(fromDate) || string.IsNullOrEmpty(toDate))
                throw new ArgumentException("From date and to date must not be empty!");

            if (string.IsNullOrEmpty(fromDate) && string.IsNullOrEmpty(toDate))
            {
                var query = transactionRepository.GetAll()
                    .AsNoTracking()
                    .Where(t => t.Status == TransactionStatus.Pending &&
                        t.IsWithdrawal == true);
                var count = await query.CountAsync();
                var withdrawalRequests = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                if (!withdrawalRequests.Any())
                    throw new NoDataRetrievalException("No data of withdrawal requests!");

                var data = _mapper.Map<List<WithdrawalResponse>>(withdrawalRequests);
                return new PaginationResponse<WithdrawalResponse>(data, count, pageNumber, pageSize);
            }
            else
            {
                DateTime from = DateTime.Parse(fromDate ?? throw new ArgumentException(nameof(fromDate)));
                DateTime to = DateTime.Parse(toDate ?? throw new ArgumentException(nameof(toDate)));

                if (from >= to)
                    throw new ArgumentException("From date must be less than to date!");
                
                var query = transactionRepository.GetAll()
                    .AsNoTracking()
                    .Where(t => t.Status == TransactionStatus.Pending &&
                        t.IsWithdrawal == true &&
                        t.CreatedAt >= from && t.CreatedAt <= to);
                var count = await query.CountAsync();
                var withdrawalRequests = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                if (!withdrawalRequests.Any())
                    throw new NoDataRetrievalException("No data of withdrawal requests!");
                
                var data = _mapper.Map<List<WithdrawalResponse>>(withdrawalRequests);
                return new PaginationResponse<WithdrawalResponse>(data, count, pageNumber, pageSize);
            }
        }

        public async Task<PaginationResponse<WithdrawalResponse>> GetWithdrawalRequestsByUser(string userCode,
            int pageNumber,
            int pageSize)
        {
            var transactionRepository = _unitOfWork.GetRepository<Transaction>();
            var responses = new List<WithdrawalResponse>();
            
            var query = transactionRepository.GetAll()
                .AsNoTracking()
                .Where(t => t.UserCode == userCode && t.IsWithdrawal == true)
                .OrderByDescending(t => t.CreatedAt);
            var count = await query.CountAsync();

            var transactions = await query.ToListAsync();
            if (transactions.Count == 0)
                throw new NoDataRetrievalException("No data of withdrawal requests!");
            foreach (var item in transactions)
            {
                var response = new WithdrawalResponse
                {
                    Id = item.Id,
                    UserCode = item.UserCode,
                    WalletId = item.WalletId,
                    Amount = item.Amount,
                    CreatedAt = item.CreatedAt,
                    Reason = item.Reason,
                    CurrentBankingNo = item.CurrentBankingNo,
                };
                responses.Add(response);
            }
            return new PaginationResponse<WithdrawalResponse>(responses, count, 
                pageNumber, 
                pageSize);
        }

        public async Task<bool> UpdateWithdrawalStatus(List<long> tIds, string status)
        {
            var result = false;
            try
            {
                var transactionRepository = _unitOfWork.GetRepository<Transaction>();
                var walletRepository = _unitOfWork.GetRepository<Wallet>();
                var batchPaymentRepository = _unitOfWork.GetRepository<BatchPayment>();

                if (!Enum.TryParse(status, true, out TransactionStatus tranStatus))
                    throw new ArgumentException("Invalid transaction's status! Accept two value: Approved or Rejected");
                foreach (var item in tIds)
                {
                    var transaction = await transactionRepository.GetAll()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == item)
                        ?? throw new KeyNotFoundException("Transaction does not exist!");

                    var wallet = await walletRepository.GetAll()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(w => w.UserCode == transaction.UserCode)
                        ?? throw new KeyNotFoundException("Wallet does not exist!");

                    if (tranStatus == TransactionStatus.Approved)
                    {
                        transaction.Status = TransactionStatus.Approved;
                        transaction.ApprovedAt = DateTime.Now;

                        transactionRepository.Update(transaction);

                        var batchData = await batchPaymentRepository.GetAll()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(bp => bp.TransactionId == item)
                            ?? throw new KeyNotFoundException("No data of batch payment!");
                        batchData.Date = transaction.ApprovedAt;

                        batchPaymentRepository.Update(batchData);

                        // New balance for wallet
                        var balance = wallet.Balance - transaction.Amount;
                        wallet.Balance = balance;
                        walletRepository.Update(wallet);

                        result = await _unitOfWork.SaveAsync() > 0;
                    }
                    else if (tranStatus == TransactionStatus.Rejected)
                    {
                        var batchData = await batchPaymentRepository.GetAll()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(bp => bp.TransactionId == item)
                            ?? throw new KeyNotFoundException("No data of batch payment!");

                        batchPaymentRepository.Delete(batchData);
                        transaction.Status = TransactionStatus.Rejected;
                        transactionRepository.Update(transaction);

                        result = await _unitOfWork.SaveAsync() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, ex.Message, ex.StackTrace);
                await _unitOfWork.RollbackAsync();
                throw;
            }
            return result;
        }
    }
}
