﻿using ANF.Core.Models.Requests;
using ANF.Core.Models.Responses;

namespace ANF.Core.Services
{
    public interface IUserService
    {
        Task<LoginResponse> Login(string email, string password);

        Task<bool> RegisterAccount(AccountCreateRequest request);

        Task<UserStatusResponse> UpdateAccountStatus(long userId, string status);
    }
}
