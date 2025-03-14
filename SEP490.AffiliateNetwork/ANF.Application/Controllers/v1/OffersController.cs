﻿using ANF.Core.Commons;
using ANF.Core.Models.Requests;
using ANF.Core.Models.Responses;
using ANF.Core.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace ANF.Application.Controllers.v1
{
    public class OffersController(IOfferService offerService) : BaseApiController
    {
        private readonly IOfferService _offerService = offerService;

        /// <summary>
        /// Get all offers
        /// </summary>
        /// <param name="request">Pagination request model</param>
        /// <returns></returns>
        [HttpGet("offers")]
        //[Authorize]
        [MapToApiVersion(1)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOffers([FromQuery] PaginationRequest request)
        {
            var offers = await _offerService.GetOffers(request);
            return Ok(new ApiResponse<PaginationResponse<OfferResponse>>
            {
                IsSuccess = true,
                Message = "Success.",
                Value = offers
            });
        }

        /// <summary>
        /// Get offer by id
        /// </summary>
        /// <param name="id">Offer id</param>
        /// <returns></returns>
        [HttpGet("offers/{id}")]
        //[Authorize]
        [MapToApiVersion(1)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOffer(long id)
        {
            var offer = await _offerService.GetOffer(id);
            return Ok(new ApiResponse<OfferResponse>
            {
                IsSuccess = true,
                Message = "Success.",
                Value = offer
            });
        }


        /// <summary>
        /// Create offers
        /// </summary>
        /// <param name="request">Offer data json</param>
        /// <returns></returns>
        [HttpPost("offers")]
        //[Authorize(Roles = "Advertiser")]
        [MapToApiVersion(1)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOffer([FromForm] OfferCreateRequest request)
        {

            var validationResult = HandleValidationErrors();
            if (validationResult is not null)
            {
                return validationResult;
            }
            var result = await _offerService.CreateOffer(request);
            if (!result) return BadRequest();

            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Create offer successfully"
            });
        }

        /// <summary>
        /// Update offer
        /// </summary>
        /// <param name="id">Offer id</param>
        /// <param name="request">Offer data</param>
        /// <returns></returns>
        [HttpPut("offers/{id}")]
        [MapToApiVersion(1)]
        //[Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateOffer(long id, OfferUpdateRequest request)
        {
            var validationResult = HandleValidationErrors();
            if (validationResult is not null)
            {
                return validationResult;
            }

            var result = await _offerService.UpdateOffer(id, request);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Update successfully"
            });
        }

        /// <summary>
        /// Delete offer
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("offers/{id}")]
        [MapToApiVersion(1)]
        //[Authorize(Roles = "Advertiser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteOffer(long id)
        {
            var result = await _offerService.DeleteOffer(id);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Delete success."
            });
        }
    }
}
