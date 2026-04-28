using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace SibGost.Core.Configuration
{
    public static class AppConfig 
    {
        private static IConfiguration? _config;
        public static IConfiguration Instance
        {
            get 
            {
                if (_config == null) 
                {
                    var basePath = AppContext.BaseDirectory;
                    if (!File.Exists(Path.Combine(basePath, "appsettings.json")))
                        basePath = Directory.GetCurrentDirectory();
                    
                    _config = new ConfigurationBuilder()
                        .SetBasePath(basePath)
                        .AddJsonFile("appsettings.json", optional: false)
                        .Build();
                }
                return _config;
            }
        }
        public static string GetConnectionString(string name) =>
            Instance.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' not found");

        public static string GetSetting(string key) =>
            Instance[key] ?? throw new InvalidOperationException($"Setting '{key}' not found");
    }
}
