﻿using ANF.Core.Models.Requests;
using ANF.Core.Models.Responses;

namespace ANF.Core.Services
{
    public interface IAdvertiserService
    {
        Task<bool> AddProfile(long advertiserId, AdvertiserProfileRequest profile);

        Task<AdvertiserProfileResponse> GetAdvertiserProfile(long advertiserId);

        Task<bool> UpdateProfile(long advertiserId, AdvertiserProfileUpdatedRequest request);

        Task<List<AffiliateSourceResponse>> GetTrafficSourceOfPublisher(long publisherId);

        Task<List<PublisherInformationForAdvertiser>> GetPendingPublisherInOffer(string offerId);

        Task<List<AdvertiserCampaignStatsResponse>> GetTotalStatsOfAllCampaigns(DateTime from, DateTime to);

        Task<List<AdvertiserCampaignStatsResponse>> GetTotalStatsOfCampaign(long campaignId, DateTime from, DateTime to);
    }
}
