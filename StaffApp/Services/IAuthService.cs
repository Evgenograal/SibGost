using System;
using System.Collections.Generic;
using System.Text;
using SibGost.Core.Models;

namespace StaffApp.Services
{
    public interface IAuthService
    {
        Task<bool> IsLoginTaken(string login);
        Task<User?> Login(string login, string password);
        Task<AuthResult> RegisterAsync(string login, string password, string email, string firstName, string? lastName);
    }
}