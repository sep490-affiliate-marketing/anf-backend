﻿using ANF.Core.Commons;
using ANF.Core.Models.Requests;
using ANF.Core.Models.Responses;
using ANF.Core.Services;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ANF.Application.Controllers.v1
{
    public class PublishersController(IPublisherService publisherService) : BaseApiController
    {
        private readonly IPublisherService _publisherService = publisherService;

        /// <summary>
        /// Get publisher's traffic sources
        /// </summary>
        /// <param name="id">Publisher's id</param>
        /// <returns>A list of available affiliate sources</returns>
        [HttpGet("publishers/{id}/traffic-sources")]
        [MapToApiVersion(1)]
        [Authorize(Roles = "Publisher, Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAffiliateSourceOfPublisher(long id)
        {
            var response = await _publisherService.GetAffiliateSourceOfPublisher(id);
            return Ok(new ApiResponse<List<AffiliateSourceResponse>>
            {
                IsSuccess = true,
                Message = "Success.",
                Value = response
            });
        }

        /// <summary>
        /// Get publisher's profile
        /// </summary>
        /// <param name="id">Publisher's id</param>
        /// <returns>Publisher's profile information with list bank accounts</returns>
        [HttpGet("publishers/{id}/profile")]
        [Authorize(Roles = "Publisher, Admin")]
        [MapToApiVersion(1)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPublisher(long id)
        {
            var publisher = await _publisherService.GetPublisherProfile(id);
            return Ok(new ApiResponse<PublisherProfileResponse>
            {
                IsSuccess = true,
                Message = "Success.",
                Value = publisher
            });
        }

        /// <summary>
        /// Add publisher's profile
        /// </summary>
        /// <param name="id">Publisher's id</param>
        /// <param name="value">Requested data</param>
        /// <returns></returns>
        [HttpPost("publisher/{id}/profile")]
        [MapToApiVersion(1)]
        [Authorize(Roles = "Publisher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddProfile(long id, [FromForm] PublisherProfileCreatedRequest value)
        {
            var validationResult = HandleValidationErrors();
            if (validationResult is not null)
            {
                return validationResult;
            }
            var result = await _publisherService.AddProfile(id, value);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Success."
            });
        }

        /// <summary>
        /// Update publisher
        /// </summary>
        /// <param name="id">Publisher's id</param>
        /// <param name="value">Requested data for updating</param>
        /// <returns></returns>
        [HttpPut("publisher/{id}/profile")]
        [MapToApiVersion(1)]
        [Authorize(Roles = "Publisher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePublisher(long id, [FromForm] PublisherProfileUpdatedRequest value)
        {
            var validationResult = HandleValidationErrors();
            if (validationResult is not null)
            {
                return validationResult;
            }
            var result = await _publisherService.UpdateProfile(id, value);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Update successfully!"
            });
        }

        /// <summary>
        /// Add affiliate sources
        /// </summary>
        /// <param name="id">Publisher's id</param>
        /// <param name="requests">The list of affiliate sources</param>
        /// <returns></returns>
        [HttpPost("publisher/{id}/affiliate-sources")]
        [MapToApiVersion(1)]
        [Authorize(Roles = "Publisher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddSources(long id, [FromBody] List<AffiliateSourceCreateRequest> requests)
        {
            var validationResult = HandleValidationErrors();
            if (validationResult is not null)
            {
                return validationResult;
            }
            var result = await _publisherService.AddAffiliateSources(id, requests);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Success."
            });
        }

        /// <summary>
        /// Update affiliate source
        /// </summary>
        /// <param name="id">Affiliate source's id</param>
        /// <param name="request">Data</param>
        /// <returns></returns>
        [HttpPut("affiliate-source/{id}")]
        [MapToApiVersion(1)]
        [Authorize(Roles = "Publisher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateAffiliateSource(long id, AffiliateSourceUpdateRequest request)
        {
            var validationResult = HandleValidationErrors();
            if (validationResult is not null)
            {
                return validationResult;
            }
            var result = await _publisherService.UpdateAffiliateSource(id, request);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Success."
            });
        }

        /// <summary>
        /// Delete affiliate sources
        /// </summary>
        /// <param name="sourceIds">The list of source's id</param>
        /// <returns></returns>
        [HttpDelete("affiliate-sources")]
        [MapToApiVersion(1)]
        [Authorize(Roles = "Publisher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAffiliateSources(List<long> sourceIds)
        {
            var result = await _publisherService.DeleteAffiliateSources(sourceIds);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Success."
            });
        }

        //TODO: Review the endpoint again
        [HttpPatch("publisher/affiliate-sources")]
        [MapToApiVersion(1)]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateAffiliateSourceStatus(List<long> sIds)
        {
            var validationResult = HandleValidationErrors();
            if (validationResult is not null)
                return validationResult;
            var result = await _publisherService.UpdateAffiliateSourceState(sIds);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Success."
            });
        }
    }
}
