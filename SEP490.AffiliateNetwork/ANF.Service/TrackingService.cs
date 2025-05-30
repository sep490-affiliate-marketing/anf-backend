﻿using ANF.Core;
using ANF.Core.Commons;
using ANF.Core.Enums;
using ANF.Core.Models.Entities;
using ANF.Core.Services;
using ANF.Infrastructure;
using ANF.Infrastructure.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using MyCSharp.HttpUserAgentParser;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ANF.Service
{
    public class TrackingService : ITrackingService
    {
        private static readonly string _ipApiBaseUrl = "http://ip-api.com/json/";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConcurrentQueue<TrackingEvent> _trackingQueue;
        private readonly CancellationTokenSource _cts;
        private readonly Task _processingTask;
        private readonly ILogger<TrackingService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly INotificationService _notificationService;
        private readonly HttpClient _httpClient;
        private readonly IpApiSettings _ipApiSettings;
        private readonly string _env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
            
        public TrackingService(IUnitOfWork unitOfWork, IMemoryCache cache, IHttpClientFactory httpClientFactory,
            IServiceScopeFactory scopeFactory, ILogger<TrackingService> logger,
            IOptions<IpApiSettings> options,
            INotificationService notificationService,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
            _trackingQueue = new ConcurrentQueue<TrackingEvent>();
            _cts = new CancellationTokenSource();
            _scopeFactory = scopeFactory;
            _processingTask = Task.Run(() => ProcessQueueAsync(_cts.Token));
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _ipApiSettings = options.Value;
            _notificationService = notificationService;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(_ipApiBaseUrl);
        }

        public async Task<string> StoreParams(long offerId, string publisherCode, string? siteId, HttpRequest httpRequest)
        {
            try
            {
                var campaignRepository = _unitOfWork.GetRepository<Campaign>();
                var offerRepository = _unitOfWork.GetRepository<Offer>();
                var publiserOfferRepository = _unitOfWork.GetRepository<PublisherOffer>();
                var userRepository = _unitOfWork.GetRepository<User>();

                var baseUrl = "https://dev.l3on.id.vn";

                var offer = await offerRepository.GetAll()
                                    .AsNoTracking()
                                    .Include(e => e.Campaign)
                                    .FirstOrDefaultAsync(e => e.Id == offerId);

                if (offer is null)
                {
                    _logger.LogInformation("Offer with ID {OfferId} not found.", offerId);
                    return baseUrl;
                }

                var campaign = offer.Campaign;

                var campaignUrl = campaign.ProductUrl;

                var userExist = await userRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(e => e.UserCode.ToString() == publisherCode);
                if (userExist is null)
                {
                    _logger.LogInformation("Publisher with ID {PublisherCode} not found.", publisherCode);
                    return campaignUrl;
                }

                string referer = httpRequest.Headers["Referer"].ToString();

                string userAgent = httpRequest.Headers["User-Agent"].ToString();
                var remoteIpAddress = httpRequest.HttpContext.Connection.RemoteIpAddress;
                if (remoteIpAddress == null)
                {
                    _logger.LogInformation("Cannot determine the client's IP address.");
                    return campaignUrl;
                }

                // Get remote IP address
                var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress.ToString();

                // For local testing
                if (ipAddress == "0.0.0.1") ipAddress = "your-ip";


                var uaInfor = HttpUserAgentParser.Parse(userAgent);
                if (offerId < 1)
                {
                    _logger.LogInformation("Invalid offer ID {OfferId}.", offerId);
                    return campaignUrl;
                }
                
                bool isBot = false;
                if (uaInfor.IsRobot())
                {
                    _logger.LogInformation("Bot request detected for offer ID {OfferId}.", offerId);
                    isBot = true;
                }

                // Check whether the publisher is running the offer
                var isExisted = await publiserOfferRepository.GetAll()
                    .AsNoTracking()
                    .AnyAsync(e => e.PublisherCode == publisherCode && e.OfferId == offerId);
                if (!isExisted)
                {
                    _logger.LogInformation("This offer is not run by this publisher!");
                    return campaignUrl;
                }

                //Offer kết thúc nhưng vẫn lưu tracking được
                if (DateTime.Now < offer.StartDate)
                {
                    _logger.LogInformation("Offer is not available");
                    return campaignUrl;
                }

                if (campaign.Status != CampaignStatus.Started && campaign.Status != CampaignStatus.Ended)
                {
                    _logger.LogInformation("Campaign is not available");
                    return campaignUrl;
                }

                var ipInfo = await GetIpInfoAsync(ipAddress!);

                string clickId = StringHelper.GenerateUniqueCode();

                var trackingEvent = new TrackingEvent
                {
                    Id = clickId,
                    PublisherCode = publisherCode,
                    OfferId = offerId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    SiteId = siteId,
                    Country = ipInfo.Country,
                    Carrier = ipInfo.Carrier,
                    ClickTime = DateTime.Now,
                    Referer = referer,
                    Proxy = ipInfo.Proxy.ToString(),
                };

                if (isBot || ipInfo == null || ipInfo.Proxy == true)
                {
                    trackingEvent.Status = TrackingEventStatus.Fraud;
                } 
                else if(campaign.Status != CampaignStatus.Started)
                {
                    trackingEvent.Status = TrackingEventStatus.Invalid;
                }
                else
                {
                    trackingEvent.Status = TrackingEventStatus.Pending;
                }

                _trackingQueue.Enqueue(trackingEvent);

                var trackingData = new Dictionary<string, string>
                {
                    { "click_id",trackingEvent.Id },
                    { "publisher_code", publisherCode },
                    { "offer_id", trackingEvent.OfferId.ToString() },
                    { "ip_address", trackingEvent.IpAddress!},
                    { "user_agent", trackingEvent.SiteId! },
                    { "site_id", trackingEvent.UserAgent },
                    { "country", trackingEvent.Country},
                    { "click_time", trackingEvent.ClickTime.ToString()},
                    { "referer", trackingEvent.Referer},
                }.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value);

                string redirectUrl = BuildRedirectUrl(campaign.ProductUrl, campaign.TrackingParams!, trackingData);
                return redirectUrl;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task StoreTrackingData(TrackingEvent trackingEvent)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var trackingEventRepository = unitOfWork.GetRepository<TrackingEvent>();
                var fraudRepository = unitOfWork.GetRepository<FraudDetection>();
                try
                {
                    trackingEventRepository.Add(trackingEvent);

                    if(trackingEvent.Status == TrackingEventStatus.Fraud)
                    {
                        FraudDetection fraud = new FraudDetection
                        {
                            ClickId = trackingEvent.Id,
                            Reason = "Bot devices",
                            DetectedTime = DateTime.Now
                        };
                        fraudRepository.Add(fraud);
                    }

                    await unitOfWork.SaveAsync();
                }
                catch (Exception)
                {
                    _logger.Log(LogLevel.Error, "Something went wrong with " + trackingEvent.Id);
                    await unitOfWork.RollbackAsync();
                    throw;
                }
            }
        }

        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_trackingQueue.TryDequeue(out var postbackData))
                    {
                        await StoreTrackingData(postbackData);
                    }
                    else
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (Exception)
                {
                    break;
                }
            }
        }

        private string BuildRedirectUrl(string productUrl,
                                    string trackingParamsJson,
                                    Dictionary<string, string> trackingData)
        {
            try
            {
                if (string.IsNullOrEmpty(trackingParamsJson))
                {
                    return productUrl;
                }

                var trackingParams = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(trackingParamsJson);
                if (trackingParams is null)
                {
                    return productUrl;
                }

                var paramsDict = new Dictionary<string, string>();
                foreach (var param in trackingParams)
                {
                    string paramName = param.ContainsKey("param_name") ? param["param_name"] : string.Empty;
                    string paramValueKey = param.ContainsKey("param_value") ? param["param_value"] : string.Empty;
                    if (!string.IsNullOrEmpty(paramName) && trackingData.ContainsKey(paramValueKey))
                    {
                        paramsDict[paramName] = trackingData[paramValueKey];
                    }
                }

                var queryString = string.Join("&", paramsDict.Select(
                                      p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
                return productUrl + (productUrl.Contains("?") ? "&" : "?") + queryString;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<IpInfor> GetIpInfoAsync(string ipAddress)
        {
            try
            {
                string apiKey = _ipApiSettings.ApiKey;
                string url = $"{ipAddress}?key={apiKey}&fields=status,message,country,isp,proxy";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IpInfor>(json) ?? throw new ArgumentException("Error!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching IP info from ipapi.co for {ipAddress}: {ex.Message}");
                return new IpInfor { Ip = ipAddress };
            }
        }
        public async Task ProcessTrackingEvent(TrackingConversionEvent trackingConversionEvent)
        {
            var offerRepository = _unitOfWork.GetRepository<Offer>();
            var walletRepository = _unitOfWork.GetRepository<Wallet>();
            var walletHistoryRepository = _unitOfWork.GetRepository<WalletHistory>();
            var campaignRepository = _unitOfWork.GetRepository<Campaign>();
            var transactionRepository = _unitOfWork.GetRepository<Transaction>();
            var userRepository = _unitOfWork.GetRepository<User>();
            var trackingValidationRepository = _unitOfWork.GetRepository<TrackingValidation>();
            var postBackRepository = _unitOfWork.GetRepository<PostbackData>();

            var trackingValidation = await trackingValidationRepository.GetAll()
                .FirstOrDefaultAsync(e => e.Id == trackingConversionEvent.Id);

            decimal money = 0;

            if (trackingValidation is null)
            {
                _logger.LogError($"=================== Tracking validate's id: {trackingConversionEvent.Id} does not exist ===================");
                return;
            }

            try
            {
                var offer = await offerRepository.GetAll()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == trackingConversionEvent.OfferId)
                    ?? throw new KeyNotFoundException($"Offer with id {trackingConversionEvent.OfferId} not found.");

                var campaign = await campaignRepository.GetAll()
                    .FirstOrDefaultAsync(e => e.Id == offer.CampaignId)
                    ?? throw new KeyNotFoundException($"Campaign with id {offer.CampaignId} not found.");

                var advertiserWallet = await walletRepository
                     .GetAll()
                     .FirstOrDefaultAsync(e => e.UserCode == campaign.AdvertiserCode)
                     ?? throw new KeyNotFoundException($"Advertiser wallet {campaign.AdvertiserCode} not found.");

                var publisherWallet = await walletRepository.GetAll()
                            .FirstOrDefaultAsync(e => e.UserCode == trackingConversionEvent.PublisherCode)
                            ?? throw new KeyNotFoundException($"Publisher wallet {trackingConversionEvent.PublisherCode} not found.");

                if (trackingConversionEvent.PricingModel == "CPS")
                {
                    var postback = await postBackRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(e => e.ClickId == trackingConversionEvent.ClickId);
                    if (postback is not null)
                    {
                        double postBackMoney = postback.Amount ?? 0;
                        money = (decimal) postBackMoney * ((decimal) (offer.CommissionRate ?? 0) / 100);
                    }
                } else
                {
                    money = offer.Bid;
                }
                _logger.LogInformation("====================================== Calculated money: {Money} ======================================", money);

                if (advertiserWallet.Balance < money)
                {
                    throw new InvalidOperationException("Insufficient funds in advertiser's wallet.");
                }

                //Validate campaign còn đủ tiền hay không
                if (campaign.Balance < money)
                {
                    throw new InvalidOperationException("Campaign is out of money.");
                }

                var advTransaction = new Transaction
                {
                    Id = IdHelper.GenerateTransactionId(),
                    UserCode = advertiserWallet.UserCode,
                    WalletId = advertiserWallet.Id,
                    Amount = money,
                    Reason = $"Transfer for campaign {campaign.Name} (Offer: {offer.Id})",
                    CreatedAt = DateTime.Now,
                    Status = TransactionStatus.Success,
                    IsWithdrawal = true,
                };
                transactionRepository.Add(advTransaction);

                var advertiserHistory = new WalletHistory
                {
                    TransactionId = advTransaction.Id,
                    CurrentBalance = advertiserWallet.Balance,
                    BalanceType = false
                };
                walletHistoryRepository.Add(advertiserHistory);
                _logger.LogInformation("=================== Updated advertiser wallet and recorded history ===================");

                var pubTransaction = new Transaction
                {
                    Id = IdHelper.GenerateTransactionId(),
                    UserCode = publisherWallet.UserCode,
                    WalletId = publisherWallet.Id,
                    Amount = money,
                    Reason = $"Transfer for campaign {campaign.Name} (Offer: {offer.Id})",
                    CreatedAt = DateTime.Now,
                    Status = TransactionStatus.Success,
                    IsWithdrawal = false,
                };
                transactionRepository.Add(pubTransaction);

                var pubHistory = new WalletHistory
                {
                    TransactionId = pubTransaction.Id,
                    CurrentBalance = publisherWallet.Balance,
                    BalanceType = false
                };
                walletHistoryRepository.Add(pubHistory);
                _logger.LogInformation("=================== Updated publisher wallet and recorded history ===================");

                //Update advertiser wallet
                advertiserWallet.Balance -= money;
                walletRepository.Update(advertiserWallet);

                //Update publisher wallet
                publisherWallet.Balance += money;
                walletRepository.Update(publisherWallet);

                //Update campaign budget
                campaign.Balance -= money;
                campaignRepository.Update(campaign);

                //Update tracking validation
                trackingValidation.ConversionStatus = ConversionStatus.Success;
                trackingValidationRepository.Update(trackingValidation);


                //Advertiser wallet
                await _notificationService.NotifyUserProfile(advertiserWallet.UserCode);

                //Publisher wallet
                await _notificationService.NotifyUserProfile(publisherWallet.UserCode);

                await _unitOfWork.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"=================== Error process tracking data for {trackingConversionEvent.Id}: {ex.Message} ===================");
                await _unitOfWork.RollbackAsync();
                //Update 2 cột status của tracking validation nếu có bug được throw ra
                trackingValidation.ConversionStatus = ConversionStatus.Failed;
                trackingValidation.ValidationStatus = ValidationStatus.Failed;
                trackingValidation.Amount = money;
                trackingValidationRepository.Update(trackingValidation);
                if(trackingConversionEvent.PricingModel == "CPS")
                {
                    var postback = await postBackRepository.GetAll().AsNoTracking().FirstOrDefaultAsync(e => e.ClickId == trackingConversionEvent.ClickId);
                    if (postback is not null)
                    {
                        postback.Status = PostbackStatus.Failed;
                        postBackRepository.Update(postback);
                    }
                }
                await _unitOfWork.SaveAsync();
                throw;
            }
        }
    }
}