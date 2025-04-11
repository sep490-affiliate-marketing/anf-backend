using ANF.Core;
using ANF.Core.Models.Entities;
using ANF.Core.Models.Responses;
using ANF.Core.Services;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace ANF.Service
{
    public class StatisticService : IStatisticService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public StatisticService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<bool> GeneratePublisherOfferStatsByOfferId(long offerId, string publisherCode)
        {
            try
            {
                var publisherOfferStatsRepo = _unitOfWork.GetRepository<PublisherOfferStats>();
                var userRepo = _unitOfWork.GetRepository<User>();
                var publisherOfferRepo = _unitOfWork.GetRepository<PublisherOffer>();
                var OfferRepo = _unitOfWork.GetRepository<Offer>();

                //get offer
                var offer = await OfferRepo.GetAll()
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(o => o.Id == offerId 
                                                                && (o.Status == Core.Enums.OfferStatus.Started
                                                                    || o.Status == Core.Enums.OfferStatus.Approved));
                if (offer == null) throw new KeyNotFoundException("Not found offer of offer is neither approved nor started");

                //get publisher
                var publisher = await userRepo.GetAll()
                                              .AsNoTracking()
                                              .FirstOrDefaultAsync(p => (p.UserCode.Equals(publisherCode)
                                                                       && (p.Role == Core.Enums.UserRoles.Publisher)
                                                                       && (p.Status == Core.Enums.UserStatus.Active)));
                if (publisher == null) throw new KeyNotFoundException("Not found publisher or not active");

                //check if publisher is approved or not
                var publisherOffer = await publisherOfferRepo.GetAll()
                                                       .AsNoTracking()
                                                       .FirstOrDefaultAsync(po => ((po.OfferId == offerId)
                                                                                    && (po.PublisherCode.Equals(publisherCode))
                                                                                    && (po.Status == Core.Enums.PublisherOfferStatus.Approved)));
                if (publisherOffer == null) throw new KeyNotFoundException("Publisher is not approve in this offer");

                //analyse
                var publisherOfferStats = await publisherOfferStatsRepo.GetAll()
                                                                       .AsNoTracking()
                                                                       .FirstOrDefaultAsync(s => s.OfferId == offer.Id);

                if (publisherOfferStats != null)
                {
                    var newPublisherOfferStats = await AnalyzePublisherOfferStats(offer, publisherCode)
                                                 ?? throw new ArgumentException("Error in analyze process");
                    newPublisherOfferStats.Id = publisherOfferStats.Id;
                    publisherOfferStatsRepo.Update(newPublisherOfferStats);
                }
                else
                {
                    var newPublisherOfferStats = await AnalyzePublisherOfferStats(offer, publisherCode)
                                                 ?? throw new ArgumentException("Error in analyze process");
                    publisherOfferStatsRepo.Add(newPublisherOfferStats);
                }

                return await _unitOfWork.SaveAsync() > 0;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new ArgumentException(ex.Message);
            }
        }
        public async Task<bool> GeneratePublisherOfferStatsByPublisherCode(string publisherCode)
        {
            try
            {
                var publisherOfferStatsRepo = _unitOfWork.GetRepository<PublisherOfferStats>();
                var userRepo = _unitOfWork.GetRepository<User>();
                var publisherOfferRepo = _unitOfWork.GetRepository<PublisherOffer>();
                var OfferRepo = _unitOfWork.GetRepository<Offer>();

                //get publisher
                var publisher = await userRepo.GetAll()
                                              .AsNoTracking()
                                              .FirstOrDefaultAsync(p => (p.UserCode.Equals(publisherCode)
                                                                       && (p.Role == Core.Enums.UserRoles.Publisher)
                                                                       && (p.Status == Core.Enums.UserStatus.Active)));
                if (publisher == null) throw new KeyNotFoundException("Not found publisher or not active");

                //get publisherOffers list
                var publisherOffers = await publisherOfferRepo.GetAll()
                                                       .AsNoTracking()
                                                       .Where(po => (po.PublisherCode.Equals(publisherCode)
                                                                                   && (po.Status == Core.Enums.PublisherOfferStatus.Approved)))
                                                       .ToListAsync();
                if (publisherOffers == null) throw new KeyNotFoundException("Not found any approved offer of this publisher");

                //get offers list
                var offerIds = publisherOffers.Select(o => o.OfferId).ToList();
                var offers = await OfferRepo.GetAll()
                                            .AsNoTracking()
                                            .Where(o => offerIds.Contains(o.Id))
                                            .ToListAsync();
                //analyse
                foreach (var offer in offers)
                {
                    var publisherOfferStats = await publisherOfferStatsRepo.GetAll()
                                                                           .AsNoTracking()
                                                                           .FirstOrDefaultAsync(po => po.OfferId == offer.Id);
                    if (publisherOfferStats != null)
                    {
                        var newPublisherOfferStats =  await AnalyzePublisherOfferStats(offer, publisherCode)
                                                     ?? throw new ArgumentException("Error in analyze process");
                        newPublisherOfferStats.Id = publisherOfferStats.Id;
                        publisherOfferStatsRepo.Update(newPublisherOfferStats);
                    }
                    else
                    {
                        var newPublisherOfferStats = await AnalyzePublisherOfferStats(offer, publisherCode)
                                                     ?? throw new ArgumentException("Error in analyze process");
                        publisherOfferStatsRepo.Add(newPublisherOfferStats);
                    }
                }


                return await _unitOfWork.SaveAsync() > 0;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new ArgumentException(ex.Message);
            }
        }
        public async Task<List<PublisherOfferStatsResponse>> GetAllPublisherOfferStatsByPublisherCode(string publisherCode)
        {
            var publisherOfferStatsRepo = _unitOfWork.GetRepository<PublisherOfferStats>();

            var publisherOfferStatsList = await publisherOfferStatsRepo.GetAll()
                                                                 .AsNoTracking()
                                                                 .Where(po => po.PublisherCode == publisherCode)
                                                                 .ToListAsync();

            if (publisherOfferStatsList == null) throw new KeyNotFoundException("Not found");
            return _mapper.Map<List<PublisherOfferStatsResponse>>(publisherOfferStatsList);
        }
        public async Task<PublisherOfferStatsResponse> GetPublisherOfferStatsByOfferId(long offerId, string publisherCode)
        {
            var publisherOfferStatsRepo = _unitOfWork.GetRepository<PublisherOfferStats>();

            var publisherOfferStat = await publisherOfferStatsRepo.GetAll()
                                                            .AsNoTracking()
                                                            .FirstOrDefaultAsync(po => ((po.OfferId == offerId) && po.PublisherCode.Equals(publisherCode)));
            if (publisherOfferStat == null) throw new KeyNotFoundException("Not found");

            return _mapper.Map<PublisherOfferStatsResponse>(publisherOfferStat);

        }
        
        public async Task<bool> GenerateAdvertiserOfferStatsByAdvertiserCode(string advertiserCode)
        {
            try
            {
                var advertiserOfferStatsRepo = _unitOfWork.GetRepository<AdvertiserOfferStats>();
                var userRepo = _unitOfWork.GetRepository<User>();
                var campaignRepo = _unitOfWork.GetRepository<Campaign>();
                var OfferRepo = _unitOfWork.GetRepository<Offer>();

                //check valid advertiser
                var advertiser = await userRepo.GetAll()
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync(u => u.UserCode.Equals(advertiserCode) 
                                                                   && u.Role == Core.Enums.UserRoles.Advertiser
                                                                   && u.Status == Core.Enums.UserStatus.Active);
                if (advertiser == null) throw new KeyNotFoundException("not found Advertiser or not active");

                //get campaigns list
                var campaigns = await campaignRepo.GetAll()
                                            .AsNoTracking()
                                            .Where(c => (c.AdvertiserCode.Equals(advertiserCode)
                                                         && (c.Status == Core.Enums.CampaignStatus.Started
                                                            || c.Status == Core.Enums.CampaignStatus.Verified)))
                                            .ToListAsync();
                if (campaigns == null) throw new KeyNotFoundException("Advertiser with code = " + advertiserCode
                                                                      + " don't have either Started or Verified campaign");

                //get offers list
                var campaignIds = campaigns.Select(c => c.Id).ToList();
                var offers = await OfferRepo.GetAll()
                                      .AsNoTracking()
                                      .Where(o => (campaignIds.Contains(o.CampaignId)
                                                   && (o.Status == Core.Enums.OfferStatus.Approved
                                                       || o.Status == Core.Enums.OfferStatus.Started)))
                                      .ToListAsync();
                if (offers == null) throw new KeyNotFoundException("Advertiser with code = " + advertiserCode
                                                                      + " don't have either Approved or Started offer");

                //analyze
                foreach (var o in offers)
                {
                    var advertiserOfferStats = await advertiserOfferStatsRepo.GetAll()
                                                                             .AsNoTracking()
                                                                             .FirstOrDefaultAsync(s => s.OfferId == o.Id);

                    if (advertiserOfferStats != null)
                    {
                        var newAdvertiserOfferStats = await AnalyzeAdvertiserOfferStats(o) ?? throw new ArgumentException("Error in analyze process");
                        newAdvertiserOfferStats.Id = advertiserOfferStats.Id;
                        advertiserOfferStatsRepo.Update(newAdvertiserOfferStats);
                    }
                    else
                    {
                        var newAdvertiserOfferStats = await AnalyzeAdvertiserOfferStats(o) ?? throw new ArgumentException("Error in analyze process");
                        advertiserOfferStatsRepo.Add(newAdvertiserOfferStats);
                    }
                }
                return await _unitOfWork.SaveAsync() > 0;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new ArgumentException(ex.Message);
            }
        }
        public async Task<bool> GenerateAdvertiserOfferStatsByOfferId(long offerId)
        {
            try
            {
                var advertiserOfferStatsRepo = _unitOfWork.GetRepository<AdvertiserOfferStats>();
                var offerRepo = _unitOfWork.GetRepository<Offer>();

                //get offer details
                var offer = await offerRepo.GetAll()
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(o => o.Id == offerId
                                                               && (o.Status == Core.Enums.OfferStatus.Approved
                                                                   || o.Status == Core.Enums.OfferStatus.Started));
                if (offer == null) throw new KeyNotFoundException("not found offer or offer nether approved nor started");

                var advertiserOfferStats = await advertiserOfferStatsRepo.GetAll()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.OfferId == offerId);
                if (advertiserOfferStats != null)
                {
                    var newAdvertiserOfferStats = await AnalyzeAdvertiserOfferStats(offer) ?? throw new ArgumentException("Error in analyze process");
                    newAdvertiserOfferStats.Id = advertiserOfferStats.Id;
                    advertiserOfferStatsRepo.Update(newAdvertiserOfferStats);
                }
                else
                {
                    var newAdvertiserOfferStats = await AnalyzeAdvertiserOfferStats(offer) ?? throw new ArgumentException("Error in analyze process");
                    advertiserOfferStatsRepo.Add(newAdvertiserOfferStats);
                }

                return await _unitOfWork.SaveAsync() > 0;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                throw new ArgumentException(ex.Message);
            }
        }
        public async Task<AdvertiserOfferStatsResponse> GetAdvertiserOfferStatsByOfferId(long offerId)
        {
            var advertiserOfferStatsRepo = _unitOfWork.GetRepository<AdvertiserOfferStats>();
            var statistic = await advertiserOfferStatsRepo.GetAll()
                                                    .AsNoTracking()
                                                    .FirstOrDefaultAsync(s => s.OfferId == offerId);
            if (statistic == null) throw new KeyNotFoundException("Statistic of offer with id = '" + offerId + "' is not found");
            return _mapper.Map<AdvertiserOfferStatsResponse>(statistic);
        }
        public async Task<List<AdvertiserOfferStatsResponse>> GetAllAdvertiserOffersStatsByAdvertiserCode(string advertiserCode)
        {
            var advertiserOfferStatsRepo = _unitOfWork.GetRepository<AdvertiserOfferStats>();
            var userRepo = _unitOfWork.GetRepository<User>();
            var campaignRepo = _unitOfWork.GetRepository<Campaign>();
            var OfferRepo = _unitOfWork.GetRepository<Offer>();

            //check valid advertiser
            var advertiser = await userRepo.GetAll()
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(u => u.UserCode.Equals(advertiserCode));
            if (advertiser == null) throw new KeyNotFoundException("not found Advertiser with code = " + advertiserCode);

            //get campaigns list
            var campaigns = campaignRepo.GetAll()
                                        .AsNoTracking()
                                        .Where(c => (c.AdvertiserCode.Equals(advertiserCode)
                                                     && (c.Status == Core.Enums.CampaignStatus.Started
                                                        || c.Status == Core.Enums.CampaignStatus.Verified)))
                                        .ToList();
            if (campaigns == null) throw new KeyNotFoundException("Advertiser with code = " + advertiserCode
                                                                  + " don't have either Started or Verified campaign");

            //get offers list
            var campaignIds = campaigns.Select(c => c.Id).ToList();
            var offers = OfferRepo.GetAll()
                                  .AsNoTracking()
                                  .Where(o => (campaignIds.Contains(o.CampaignId)
                                               && (o.Status == Core.Enums.OfferStatus.Approved
                                                   || o.Status == Core.Enums.OfferStatus.Started)))
                                  .ToList();
            if (offers == null) throw new KeyNotFoundException("Advertiser with code = " + advertiserCode
                                                                  + " don't have either Approved or Started offer");
            var offerIds = offers.Select(o => o.Id).ToList();
            var statistics = advertiserOfferStatsRepo.GetAll()
                                                    .AsNoTracking()
                                                    .Where(s => offerIds.Contains(s.OfferId))
                                                    .ToList();
            if (statistics == null) throw new KeyNotFoundException("Statistics are not found");
            return _mapper.Map<List<AdvertiserOfferStatsResponse>>(statistics);
        }

        private async Task<PublisherOfferStats?> AnalyzePublisherOfferStats(Offer offer, string publisherCode)
        {
            try
            {
                var trackingRepo = _unitOfWork.GetRepository<TrackingEvent>();
                var validationRepo = _unitOfWork.GetRepository<TrackingValidation>();

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
        private async Task<AdvertiserOfferStats?> AnalyzeAdvertiserOfferStats(Offer offer)
        {
            try
            {
                var trackingRepo = _unitOfWork.GetRepository<TrackingEvent>();
                var validationRepo = _unitOfWork.GetRepository<TrackingValidation>();
                var publisherOfferRepo = _unitOfWork.GetRepository<PublisherOffer>();

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
