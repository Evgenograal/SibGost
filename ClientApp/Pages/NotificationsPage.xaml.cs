using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;

namespace ClientApp.Pages;

public partial class NotificationsPage : ContentPage
{
	private readonly SibGostDbContext _context;
	public NotificationsPage(SibGostDbContext context)
	{
		InitializeComponent();
		_context = context;
	}

	protected override async void OnAppearing() 
	{
		base.OnAppearing();
		await LoadNotifAsync();
	}

	private async Task LoadNotifAsync() 
	{
		try
		{
			var savedId = await SecureStorage.Default.GetAsync("user_session_id");
			if (string.IsNullOrEmpty(savedId) || !int.TryParse(savedId, out int userId))
			{
				await Shell.Current.GoToAsync("//LoginPage");
				return;
			}

			var userBookingsId = await _context.Bookings.Where(b => b.UserId == userId).Select(b => b.Id).ToListAsync();

			var notifications = await _context.Notifications.Include(n => n.Booking).ThenInclude(b => b.Room).Where(n => userBookingsId.Contains(n.BookingId))
				.OrderByDescending(n => n.ScheduledAt).ToListAsync();

			NotificationsCollection.ItemsSource = notifications;
        }
		catch (Exception ex)
		{
            await DisplayAlert("Ошибка", "Не удалось загрузить уведомления", "ОК");
        }
    }
}