using ClientApp.Services;
using Microsoft.Maui.Storage;

namespace ClientApp.Pages;

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
		var login = EntryLogin.Text;
		var password = EntryPassword.Text;

		var user = await _authService.Login(login, password);

        if (user != null)
        {
			await SecureStorage.Default.SetAsync("user_session_id", user.Id.ToString());
			await Shell.Current.GoToAsync("//RoomsPage");
        }
        else
        {
            ShowError("Неверный логин или пароль");
        }
    }
	private async void OnRegisterClicked(object sender, EventArgs e) 
	{
		await Shell.Current.GoToAsync("RegisterPage");
	}

	private void ShowError(string message) 
	{
		LblError.Text = message;
		LblError.IsVisible = true;
	}
}