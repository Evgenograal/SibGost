using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SibGost.Core.Data;
using System.IO;
using ClientApp.Services;

namespace ClientApp
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
                        new SibGost.Core.Models.AppRole { Id = 1, Name = "Клиент"},
                        new SibGost.Core.Models.AppRole { Id = 2, Name = "Администратор"},
                        new SibGost.Core.Models.AppRole { Id = 3, Name = "Ресепшионист"}
                    );
                    dbContext.SaveChanges();
                }
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
