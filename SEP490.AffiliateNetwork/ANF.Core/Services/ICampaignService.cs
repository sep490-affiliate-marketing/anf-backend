﻿using ANF.Core.Models.Requests;
using ANF.Core.Models.Responses;

namespace ANF.Core.Services
{
    public interface ICampaignService
    {
        Task<PaginationResponse<CampaignResponse>> GetCampaigns(PaginationRequest request);
        Task<PaginationResponse<CampaignResponse>> GetCampaignsWithOffers(PaginationRequest request);
        Task<PaginationResponse<CampaignResponse>> GetCampaignsByAdvertisersWithOffers(PaginationRequest request, string id);
        Task<bool> CreateCampaign(CampaignCreateRequest request);
        Task<bool> UpdateCampaignInformation(long id, CampaignUpdateRequest request);
        Task<bool> UpdateCampaignStatus(long id, string campaignStatus, string? rejectReason);
        Task<bool> DeleteCampaign(long id);
    }
}
