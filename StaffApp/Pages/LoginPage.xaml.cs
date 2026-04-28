using StaffApp.Services;
using Microsoft.Maui.Storage; // 🔹 Не забудь этот using!

namespace StaffApp.Pages;

public partial class LoginPage : ContentPage
{
    private readonly IAuthService _authService;

    public LoginPage(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            var login = EntryLogin.Text;
            var password = EntryPassword.Text;

            var user = await _authService.Login(login, password);

            if (user != null)
            {
                if (user.RoleId == 1)
                {
                    await DisplayAlert("Доступ запрещен", "Это приложение только для сотрудников.", "ОК");
                    return;
                }

                await SecureStorage.Default.SetAsync("user_session_id", user.Id.ToString());
                await Shell.Current.GoToAsync("//StaffDashboardPage");
            }
            else
            {
                ShowError("Неверный логин или пароль");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка входа", ex.Message, "OK");
            System.Diagnostics.Debug.WriteLine($"❌ Login error: {ex}");
        }
    }

    private void ShowError(string message)
    {
        LblError.Text = message;
        LblError.IsVisible = true;
    }
}