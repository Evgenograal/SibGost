using System;
using System.Collections.Generic;
using System.Text;

namespace ClientApp.Services
{
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public static AuthResult Success() => new AuthResult { IsSuccess = true };
        public static AuthResult Failed(string error) => new AuthResult { IsSuccess = false, ErrorMessage = error };
    }
}
