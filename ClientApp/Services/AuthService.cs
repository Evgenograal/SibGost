using ClientApp.Security;
using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ClientApp.Services
{
    public class AuthService : IAuthService
    {
        private readonly SibGostDbContext _context;

        public AuthService(SibGostDbContext context) 
        {
            _context = context;
        }

        public async Task<User?> Login(string login, string password) 
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Login == login);
            
            if (user == null) return null;

            if (PasswordHasher.Verify(password, user.PasswordHash))
            {
                await SecureStorage.Default.SetAsync("user_session_id", user.Id.ToString());
                return user;
            }

            return null;
        }

        public async Task<AuthResult> RegisterAsync(string login, string password, string email, string firstName, string? lastName)
        {
            if (await _context.Users.AnyAsync(u => u.Login == login))
                return AuthResult.Failed("Такой логин уже существует");

            if (await _context.Users.AnyAsync(u => u.Email == email))
                return AuthResult.Failed("Этот email уже зарегистрирован");

            var passwordCheck = ValidatePasswordHardcore(password);
            if (!passwordCheck.IsSuccess) return passwordCheck;

            var role = await _context.Roles.FindAsync(1);
            if (role == null)
                return AuthResult.Failed("Роль не найдена. Обратитесь к администратору.");

            var newUser = new User
            {
                Login = login,
                PasswordHash = PasswordHasher.Hash(password),
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                RoleId = 1,
                Role = role
            };

            await _context.Users.AddAsync(newUser);
            await _context.SaveChangesAsync();

            return AuthResult.Success();
        }

        public async Task<int?> GetCurrentUserIdAsync()
        {
            var savedId = await SecureStorage.Default.GetAsync("user_session_id");
            if (string.IsNullOrEmpty(savedId) || !int.TryParse(savedId, out var userId))
                return null;
            return userId;
        }

        private AuthResult ValidatePasswordHardcore(string password)
        {
            var blacklist = new[] { "password", "123456", "qwerty", "admin", "letmein", "welcome" };
            if (blacklist.Contains(password.ToLower()))
                return AuthResult.Failed("Использован слишком распространенный пароль");

            var pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$";

            if (!Regex.IsMatch(password, pattern))
            {
                return AuthResult.Failed("Пароль должен содержать минимум 12 символов, включая заглавные, строчные, цифры и спецсимволы (@$!%*?&)");
            }

            return AuthResult.Success();
        }
        public async Task<bool> IsLoginTaken(string login)
        {
            return await _context.Users.AnyAsync(u => u.Login == login);
        }
    }
}