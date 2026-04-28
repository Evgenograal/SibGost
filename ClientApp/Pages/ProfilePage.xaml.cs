using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;
using Microsoft.Maui.Storage;

namespace ClientApp.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly SibGostDbContext _dbContext;
    private User? _currentUser;

    public ProfilePage(SibGostDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUserDataAsync();
    }

    private async Task LoadUserDataAsync()
    {
        var savedId = await SecureStorage.Default.GetAsync("user_session_id");

        if (string.IsNullOrEmpty(savedId))
        {
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        int currentUserId = int.Parse(savedId);

        _currentUser = await _dbContext.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == currentUserId);

        if (_currentUser != null)
        {
            LblLogin.Text = _currentUser.Login;
            LblFirstName.Text = _currentUser.FirstName;
            LblLastName.Text = _currentUser.LastName ?? "—";
            LblEmail.Text = _currentUser.Email;

            var bookingsCount = await _dbContext.Bookings
                .CountAsync(b => b.UserId == currentUserId);
            LblBookingsCount.Text = $"Бронирований: {bookingsCount}";
        }
    }

    private async void Edit(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("EditProfilePage");
    }

    private async void Logout(object sender, EventArgs e)
    {
        bool confirm = await Shell.Current.DisplayAlert("Выход", "Вы действительно хотите выйти?", "Да", "Нет");
        if (confirm)
        {
            SecureStorage.Default.Remove("user_session_id");
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
    private async void ChangePassword(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ChangePasswordPage");
    }
}