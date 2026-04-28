using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SibGost.Core.Data;
using SibGost.Core.Models;
using StaffApp.Security;
using StaffApp.Services;
using System.IO;

namespace StaffApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            string dbName = "SibGost.db";
            string dbPath;

            var imgBBKey = "0445e6ed0c62639fc12f199fb58d9b20";
            builder.Services.AddSingleton(sp => new ImgBBService(new HttpClient(), imgBBKey));

#if WINDOWS
    var basePath = AppContext.BaseDirectory;
    dbPath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "..", "DataBase", dbName));
#elif ANDROID
            dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), dbName);
#else
    dbPath = Path.Combine(FileSystem.AppDataDirectory, dbName);
#endif

            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            System.Diagnostics.Debug.WriteLine($"DB Path: {dbPath}");

            builder.Services.AddDbContext<SibGostDbContext>(options =>
            {
                options.UseSqlite($"Data Source={dbPath}");
            });

            builder.Services.AddScoped<IAuthService, AuthService>();
            var app = builder.Build();

            try
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SibGostDbContext>();

                dbContext.Database.Migrate();

                if (!dbContext.Roles.Any())
                {
                    dbContext.Roles.AddRange(
                        new SibGost.Core.Models.AppRole { Id = 1, Name = "Клиент" },
                        new SibGost.Core.Models.AppRole { Id = 2, Name = "Администратор" },
                        new SibGost.Core.Models.AppRole { Id = 3, Name = "Ресепшионист" }
                    );
                    dbContext.SaveChanges();
                }

                var defaultAdminPass = "Admin123@";
                var defaultRecepPass = "Recep123@";

                if (!dbContext.Users.Any(u => u.Login == "admin"))
                {
                    dbContext.Users.Add(new User
                    {
                        Login = "admin",
                        PasswordHash = PasswordHasher.Hash(defaultAdminPass),
                        Email = "admin@sibgost.ru",
                        FirstName = "Никита",
                        LastName = "Никифоров",
                        RoleId = 2
                    });
                }

                if (!dbContext.Users.Any(u => u.Login == "reception"))
                {
                    dbContext.Users.Add(new User
                    {
                        Login = "reception",
                        PasswordHash = PasswordHasher.Hash(defaultRecepPass),
                        Email = "recep@sibgost.ru",
                        FirstName = "Иван",
                        LastName = "Кондратьев",
                        RoleId = 3
                    });
                }

                dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Migration error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
            }

            return app;
        }
    }
}
