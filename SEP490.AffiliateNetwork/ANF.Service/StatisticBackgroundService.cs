using ANF.Core;
using ANF.Core.Models.Entities;
using ANF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ANF.Service
{
    public class StatisticBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<StatisticBackgroundService> logger) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<StatisticBackgroundService> _logger = logger;

        //protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        var now = DateTime.UtcNow;
        //        var runTime = new DateTime(now.Year, now.Month, now.Day, 23, 59, 0);
        //        if (now > runTime)
        //            runTime = runTime.AddDays(1);

        //        var delay = runTime - now;
        //        _logger.LogInformation($"Next stats generation scheduled at {runTime} UTC");

        //        await Task.Delay(delay, stoppingToken);

        //        try
        //        {
        //            using var scope = _scopeFactory.CreateScope();
        //            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        //            await GeneratePublisherOfferStats(unitOfWork);
        //            await GenerateAdvertiserOfferStats(unitOfWork);
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error generating advertiser stats.");
        //        }
        //    }
        //}
        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation($"Stats generation started at {DateTime.UtcNow:UTC}");

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    await GeneratePublisherOfferStats(unitOfWork);
                    await GenerateAdvertiserOfferStats(unitOfWork);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating advertiser stats.");
                }

                // Wait for 30 seconds before the next execution
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
        private async Task<bool> GenerateAdvertiserOfferStats(IUnitOfWork unitOfWork)
        {
            try
            {
                var advertiserOfferStatsRepo = unitOfWork.GetRepository<AdvertiserOfferStats>();
                var userRepo = unitOfWork.GetRepository<User>();
                var campaignRepo = unitOfWork.GetRepository<Campaign>();
                var OfferRepo = unitOfWork.GetRepository<Offer>();

                //Offer
                var offers = await OfferRepo.GetAll()
                                      .Where(o => ((o.Status == Core.Enums.OfferStatus.Approved
                                                   || o.Status == Core.Enums.OfferStatus.Started)))
                                      .ToListAsync();

                //analyse
                foreach (var o in offers)
                {
                    var advertiserOfferStats = await advertiserOfferStatsRepo.GetAll()
                                                                             .FirstOrDefaultAsync(s => s.OfferId == o.Id);

                    if (advertiserOfferStats != null)
                    {
                        var newAdvertiserOfferStats = await AnalyzeAdvertiserOfferStats(o, unitOfWork) ?? throw new ArgumentException("Error in analyze process");
                        advertiserOfferStats.Date = newAdvertiserOfferStats.Date;
                        advertiserOfferStats.OfferId = newAdvertiserOfferStats.OfferId;
                        advertiserOfferStats.PublisherCount = newAdvertiserOfferStats.PublisherCount;
                        advertiserOfferStats.ClickCount = newAdvertiserOfferStats.ClickCount;
                        advertiserOfferStats.ConversionCount = newAdvertiserOfferStats.ConversionCount;
                        advertiserOfferStats.ConversionRate = newAdvertiserOfferStats.ConversionRate;
                        advertiserOfferStats.Revenue = newAdvertiserOfferStats.Revenue;
                        advertiserOfferStatsRepo.Update(advertiserOfferStats);
                    }
                    else
                    {
                        var newAdvertiserOfferStats = await AnalyzeAdvertiserOfferStats(o, unitOfWork) ?? throw new ArgumentException("Error in analyze process");
                        advertiserOfferStatsRepo.Add(newAdvertiserOfferStats);
                    }
                }

                return await unitOfWork.SaveAsync() > 0;
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                throw new ArgumentException(ex.Message);
            }
        }

        private async Task<bool> GeneratePublisherOfferStats(IUnitOfWork unitOfWork)
        {
            try
            {
                var publisherOfferStatsRepo = unitOfWork.GetRepository<PublisherOfferStats>();
                var userRepo = unitOfWork.GetRepository<User>();
                var publisherOfferRepo = unitOfWork.GetRepository<PublisherOffer>();
                var OfferRepo = unitOfWork.GetRepository<Offer>();

                //get publishers List
                var publishers = await userRepo.GetAll()
                                          .Where(u => (u.Role == Core.Enums.UserRoles.Publisher)
                                                       && (u.Status == Core.Enums.UserStatus.Active))
                                          .ToListAsync();
                if (publishers == null || publishers.Count == 0) throw new KeyNotFoundException("Not found any active publisher");

                foreach (var publisher in publishers)
                {
                    var publisherCode = publisher.UserCode;
                    var publisherOffers = await publisherOfferRepo.GetAll()
                                                            .Where(po => ((po.PublisherCode == publisherCode)
                                                                          && (po.Status == Core.Enums.PublisherOfferStatus.Approved)))
                                                            .ToListAsync();
                    var offerIds = publisherOffers.Select(po => po.OfferId).ToList();
                    var offers = await OfferRepo.GetAll()
                                          .Where(o => (offerIds.Contains(o.Id)
                                                       && (o.Status == Core.Enums.OfferStatus.Approved
                                                           || o.Status == Core.Enums.OfferStatus.Started)))
                                          .ToListAsync();
                    if (offers == null) throw new KeyNotFoundException("Don't have either Approved or Started offer");

                    //analyse
                    foreach (var o in offers)
                    {
                        var publisherOfferStats = await publisherOfferStatsRepo.GetAll()
                                                                                 .FirstOrDefaultAsync(s => s.OfferId == o.Id);

                        if (publisherOfferStats != null)
                        {
                            var newPublisherOfferStats = await AnalyzePublisherOfferStats(o, publisherCode, unitOfWork)
                                                          ?? throw new ArgumentException("Error in analyze process");
                            publisherOfferStats.Date = newPublisherOfferStats.Date;
                            publisherOfferStats.OfferId = newPublisherOfferStats.OfferId;
                            publisherOfferStats.PublisherCode = newPublisherOfferStats.PublisherCode;
                            publisherOfferStats.ClickCount = newPublisherOfferStats.ClickCount;
                            publisherOfferStats.ConversionCount = newPublisherOfferStats.ConversionCount;
                            publisherOfferStats.ConversionRate = newPublisherOfferStats.ConversionRate;
                            publisherOfferStats.Revenue = newPublisherOfferStats.Revenue;
                            
                            publisherOfferStatsRepo.Update(publisherOfferStats);
                        }
                        else
                        {
                            var newPublisherOfferStats = await AnalyzePublisherOfferStats(o, publisherCode, unitOfWork)
                                                          ?? throw new ArgumentException("Error in analyze process");
                            publisherOfferStatsRepo.Add(newPublisherOfferStats);
                        }
                    }
                }

                return await unitOfWork.SaveAsync() > 0;
            }
            catch (Exception ex)
            {
                await unitOfWork.RollbackAsync();
                throw new ArgumentException(ex.Message);
            }
        }


        private async Task<PublisherOfferStats?> AnalyzePublisherOfferStats(Offer offer, string publisherCode, IUnitOfWork unitOfWork)
        {
            try
            {
                var trackingRepo = unitOfWork.GetRepository<TrackingEvent>();
                var validationRepo = unitOfWork.GetRepository<TrackingValidation>();

                var clickList = await trackingRepo.GetAll()
                                            .AsNoTracking()
                                            .Where(t => t.OfferId == offer.Id
                                                        && t.PublisherCode == publisherCode)
                                            .ToListAsync();
                int clicksCount = clickList.Count();

                var clickIds = clickList.Select(c => c.Id)
                                        .Where(id => id != null)
                                        .ToList();
                var validatedClickList = await validationRepo.GetAll()
                                                       .AsNoTracking()
                                                       .Where(v => v.ClickId != null
                                                                    && clickIds.Contains(v.ClickId)
                                                                    && v.ValidationStatus == Core.Enums.ValidationStatus.Success)
                                                       .ToListAsync();

                int validatedClicksCount = validatedClickList.Count();

                decimal revenue = (offer.PricingModel == "CPS")
                                  ? (validatedClickList.Sum(v => v.Amount ?? 0) * (decimal)(offer.CommissionRate ?? 0))
                                  : (validatedClicksCount * offer.Bid);

                PublisherOfferStats newPublisherOfferStats = new PublisherOfferStats()
                {
                    Date = DateTime.Now,
                    OfferId = offer.Id,
                    PublisherCode = publisherCode,
                    ClickCount = clicksCount,
                    ConversionCount = validatedClicksCount,
                    ConversionRate = clicksCount == 0
                                         ? 0
                                         : Math.Round((decimal)validatedClicksCount / clicksCount, 2),
                };

                return newPublisherOfferStats;
            }
            catch
            {
                return null;
            }
        }
        private async Task<AdvertiserOfferStats?> AnalyzeAdvertiserOfferStats(Offer offer, IUnitOfWork unitOfWork)
        {
            try
            {
                var trackingRepo = unitOfWork.GetRepository<TrackingEvent>();
                var validationRepo = unitOfWork.GetRepository<TrackingValidation>();
                var publisherOfferRepo = unitOfWork.GetRepository<PublisherOffer>();

                var clickList = await trackingRepo.GetAll()
                                            .AsNoTracking()
                                            .Where(t => t.OfferId == offer.Id)
                                            .ToListAsync();
                int clicksCount = clickList.Count();

                var clickIds = clickList.Select(c => c.Id)
                                        .Where(id => id != null)
                                        .ToList();
                var validatedClickList = await validationRepo.GetAll()
                                                       .AsNoTracking()
                                                       .Where(v => v.ClickId != null
                                                                    && clickIds.Contains(v.ClickId)
                                                                    && v.ValidationStatus == Core.Enums.ValidationStatus.Success)
                                                       .ToListAsync();
                int validatedClicksCount = validatedClickList.Count();

                var publisherOfferList = await publisherOfferRepo.GetAll()
                                                           .AsNoTracking()
                                                           .Where(po => po.OfferId == offer.Id
                                                                        && po.Status == Core.Enums.PublisherOfferStatus.Approved)
                                                           .ToListAsync();
                int publisherCount = publisherOfferList.Count();

                decimal revenue = (offer.PricingModel == "CPS")
                                  ? (validatedClickList.Sum(v => v.Amount ?? 0) * (decimal)(offer.CommissionRate ?? 0))
                                  : (validatedClicksCount * offer.Bid);

                AdvertiserOfferStats newAdvertiserOfferStats = new AdvertiserOfferStats()
                {
                    Date = DateTime.Now,
                    OfferId = offer.Id,
                    ClickCount = clicksCount,
                    ConversionCount = validatedClicksCount,
                    ConversionRate = clicksCount == 0
                                         ? 0
                                         : Math.Round((decimal)validatedClicksCount / clicksCount, 2),
                    PublisherCount = publisherCount,
                    Revenue = revenue
                };

                return newAdvertiserOfferStats;
            }
            catch
            {
                return null;
            }
        }
    }
}
