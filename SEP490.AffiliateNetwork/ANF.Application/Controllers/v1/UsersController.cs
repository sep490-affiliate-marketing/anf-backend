﻿using Microsoft.AspNetCore.Mvc;
using ANF.Core.Services;
using ANF.Core.Commons;
using ANF.Core.Models.Requests;
using Asp.Versioning;
using ANF.Core.Models.Responses;
using Microsoft.AspNetCore.Authorization;

namespace ANF.Application.Controllers.v1
{
    public class UsersController(IUserService userService) : BaseApiController
    {
        private readonly IUserService _userService = userService;

        /// <summary>
        /// Authenticates a user with the provided email and password.
        /// </summary>
        /// <param name="value">The login request containing email and password.</param>
        /// <returns>An access token</returns>
        [HttpPost("users/login")]
        [MapToApiVersion(1)]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Login([FromBody] LoginRequest value)
        {
            var validationResult = HandleValidationErrors();
            if (validationResult is not null)
            {
                return validationResult;
            }
            var token = await _userService.Login(value.Email, value.Password);
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Login successfully.",
                Value = token
            });
        }

        /// <summary>
        /// Change user's account status 
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Admin")]
        [HttpPatch("users/{id}/status")]
        [MapToApiVersion(1)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeAccountStatus(long id, string status)
        {
            var result = await _userService.UpdateAccountStatus(id, status);
            if (result is not null)
            {
                return Ok(new ApiResponse<UserStatusResponse>
                {
                    IsSuccess = true,
                    Message = "Success.",
                    Value = result
                });
            }
            else return BadRequest();
            
        }

        /// <summary>
        /// Get all advertisers
        /// </summary>
        /// <param name="request">Pagination request model</param>
        /// <returns></returns>
        [HttpGet("users/advertisers")]
        [MapToApiVersion(1)]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAdvertisers([FromQuery] PaginationRequest request)
        {
            var advertisers = await _userService.GetAdvertisers(request);
            return Ok(advertisers);
        }


        [HttpGet("users/publishers")]
        [MapToApiVersion(1)]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPublishers([FromQuery] PaginationRequest request)
        {
            var publishers = await _userService.GetPublishers(request);
            return Ok(publishers);
        }

        /// <summary>
        /// Get information of the user
        /// </summary>
        /// <returns>User's information</returns>
        [HttpGet("users/me")]
        [MapToApiVersion(1)]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDetailedUser()
        {
            var user = await _userService.GetUserInformation();
            return Ok(new ApiResponse<DetailedUserResponse>
            {
                IsSuccess = true,
                Message = "Success.",
                Value = user
            });
        }

        /// <summary>
        /// Create new account
        /// </summary>
        /// <param name="value">Account data</param>
        /// <returns></returns>
        [HttpPost("users/account")]
        [AllowAnonymous]
        [MapToApiVersion(1)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegisterAccount(AccountCreateRequest value)
        {
            var validationResult = HandleValidationErrors();
            if (validationResult is not null)
            {
                return validationResult;
            }
            var result = await _userService.RegisterAccount(value);
            if (!result) return BadRequest();

            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Register account successfully. Please wait for admin to accept the registration."
            });
        }
        
        [HttpDelete("users/{id}")]
        [MapToApiVersion(1)]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(long id)
        {
            var result = await _userService.DeleteUser(id);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Success."
            });
        }

        /// <summary>
        /// Verify user's account
        /// </summary>
        /// <param name="id">User's id</param>
        /// <returns></returns>
        [HttpPatch("users/{id}/verify-account")]
        [MapToApiVersion(1)]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyUserAccount(long id)
        {
            var result = await _userService.ChangeEmailStatus(id);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Success."
            });
        }

        /// <summary>
        /// Reset passsword for user
        /// </summary>
        /// <param name="id">User's id</param>
        /// <param name="token">Reset token</param>
        /// <param name="request">Data to reset the password</param>
        /// <returns></returns>
        [HttpPatch("users/{id}/reset-token/{token}")]
        [MapToApiVersion(1)]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePassword(long id, string token, UpdatePasswordRequest request)
        {
            var result = await _userService.UpdatePassword(token, id, request);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Success."
            });
        }

        /// <summary>
        /// Send email to user to get the link to reset the password
        /// </summary>
        /// <param name="email">User's email</param>
        /// <returns></returns>
        [HttpPost("users/{email}")]
        [MapToApiVersion(1)]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SendEmailForForgetPassword(string email)
        {
            var result = await _userService.ChangePassword(email);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "An email for reset the password is sent to you. Please check the inbox and spam email to get the link!"
            });
        }

        [HttpPatch("users/{code}/wallet")]
        [MapToApiVersion(1)]
        [Authorize(Roles = "Advertiser, Publisher")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateWallet(Guid code)
        {
            var result = await _userService.ActivateWallet(code);
            if (!result) return BadRequest();
            return Ok(new ApiResponse<string>
            {
                IsSuccess = true,
                Message = "Wallet is actived successfully!"
            });
        }
    }
}
