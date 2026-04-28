using SibGost.Core.Data;
using SibGost.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace StaffApp.Pages;

public partial class StaffDashboardPage : ContentPage
{
    private readonly SibGostDbContext _dbContext;
    private User _currentUser;
    public bool IsAdmin { get; set; }
    private Timer _activityTimer;
    private DateTime _lastActivityTime;
    private const int INACTIVITY_THRESHOLD_MINUTES = 2;

    public StaffDashboardPage(SibGostDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
        this.SizeChanged += OnPageActivity;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboardDataAsync();
        StartActivityMonitoring();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopActivityMonitoring();
    }

    private void StartActivityMonitoring()
    {
        _lastActivityTime = DateTime.Now;

        _activityTimer = new Timer(CheckActivity, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    private void StopActivityMonitoring()
    {
        _activityTimer?.Dispose();
    }

    private void CheckActivity(object state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var elapsed = DateTime.Now - _lastActivityTime;
            var isInactive = elapsed.TotalMinutes >= INACTIVITY_THRESHOLD_MINUTES;

            if (isInactive)
            {
                ActivityIndicator.BackgroundColor = Colors.Red;
                LblActivityStatus.Text = "● Неактивен";
                LblActivityStatus.TextColor = Colors.Red;
            }
            else
            {
                ActivityIndicator.BackgroundColor = Color.FromArgb("#10B981");
                var minutesLeft = INACTIVITY_THRESHOLD_MINUTES - (int)elapsed.TotalMinutes;
                LblActivityStatus.Text = $"● Активен";
                LblActivityStatus.TextColor = Color.FromArgb("#10B981");
            }
        });
    }

    private void OnPageActivity(object sender, EventArgs e)
    {
        _lastActivityTime = DateTime.Now;
    }

    protected override bool OnBackButtonPressed()
    {
        _lastActivityTime = DateTime.Now;
        return base.OnBackButtonPressed();
    }

    private async Task LoadDashboardDataAsync()
    {
        try
        {
            var savedId = await SecureStorage.Default.GetAsync("user_session_id");
            if (string.IsNullOrEmpty(savedId))
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            int userId = int.Parse(savedId);
            _currentUser = await _dbContext.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (_currentUser != null)
            {
                LblUserName.Text = $"{_currentUser.Role.Name}: {_currentUser.FirstName} {_currentUser.LastName}";

                bool isAdmin = _currentUser.RoleId == 2;
                FrameAmenities.IsVisible = isAdmin;
                FrameCategories.IsVisible = isAdmin;
            }

            var totalRooms = await _dbContext.Rooms.CountAsync();
            LblTotalRooms.Text = totalRooms.ToString();

            var activeBookings = await _dbContext.Bookings
                .CountAsync(b => b.Status == "Подтверждено" || b.Status == "Ожидание" || b.Status == "Заселён");
            LblActiveBookings.Text = activeBookings.ToString();

            var today = DateTime.Today;
            var todayCheckIns = await _dbContext.Bookings
                .CountAsync(b => b.CheckIn.Date == today);
            LblTodayCheckIns.Text = todayCheckIns.ToString();

            var todayCheckOuts = await _dbContext.Bookings
                .CountAsync(b => b.CheckOut.Date == today);
            LblTodayCheckOuts.Text = todayCheckOuts.ToString();

            _lastActivityTime = DateTime.Now;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки: {ex.Message}");
        }
    }

    private async void OnManageRoomsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ManageRoomsPage");
    }

    private async void OnManageBookingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ManageBookingsPage");
    }

    private async void OnManageUsersClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ManageUsersPage");
    }

    private async void OnManageAmenitiesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ManageAmenitiesPage");
    }

    private async void OnManageCategoriesClicked(object sender, EventArgs e) 
    {
        await Shell.Current.GoToAsync("ManageCategoriesPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Выход", "Вы действительно хотите выйти?", "Да", "Нет");
        if (confirm)
        {
            SecureStorage.Default.Remove("user_session_id");
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}