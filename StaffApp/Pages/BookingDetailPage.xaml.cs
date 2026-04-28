using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;

namespace StaffApp.Pages;

[QueryProperty(nameof(BookingId), "bookingId")]
public partial class BookingDetailPage : ContentPage
{
	private readonly SibGostDbContext _context;
	private Booking? _booking;
	private int _bookingId;

	public int BookingId { get => _bookingId; set => _bookingId= value;  }
    public BookingDetailPage(SibGostDbContext context)
	{
		InitializeComponent();
		_context = context;
	}

	protected override async void OnAppearing() 
	{
		base.OnAppearing();
		await LoadBookingDataAsync();
    }

    private async Task LoadBookingDataAsync() 
	{
		try
		{
			_booking = await _context.Bookings.Include(b => b.User).Include(b => b.Room).ThenInclude(r => r.Category).Include(b => b.History).Include(b => b.Notifications).FirstOrDefaultAsync(b => b.Id == _bookingId);
            
			if (_booking == null)
            {
                await DisplayAlert("Ошибка", "Бронирование не найдено", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            LblGuest.Text = $"{_booking.User.FirstName} {_booking.User.LastName}";
            LblRoom.Text = $"Комната: {_booking.Room.RoomNumber} ({_booking.Room.Category.Name})";
            LblDates.Text = $"Заезд: {_booking.CheckIn:dd.MM.yyyy} | Выезд: {_booking.CheckOut:dd.MM.yyyy}";
            LblStatus.Text = $"Текущий статус: {_booking.Status}";

            PickerNewStatus.SelectedItem = _booking.Status;

            HistoryCollection.ItemsSource = _booking.History.OrderByDescending(h => h.ChangeAt).ToList();
            NotificationsCollection.ItemsSource = _booking.Notifications.OrderByDescending(n => n.ScheduledAt).ToList();
        }
		catch (Exception ex) 
		{
			await DisplayAlert("Ошибка", ex.Message, "OK");
		}
	}

    private async void UpdateStatus(object sender, EventArgs e)
    {
        var newStatus = PickerNewStatus.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(newStatus) || newStatus == _booking?.Status) return;

        try
        {
            var oldStatus = _booking!.Status;
            _booking.Status = newStatus;
            _booking.UpdatedAt = DateTime.UtcNow;

            _booking.History.Add(new BookingHistory
            {
                BookingId = _booking.Id,
                Action = $"Статус изменён: {oldStatus} → {newStatus}",
                ChangeBy = await GetStaffNameAsync(),
                ChangeAt = DateTime.Now,
                OldValue = oldStatus,
                NewValue = newStatus
            });

            if (newStatus == "Подтверждено" || newStatus == "Заселён")
            {
                _booking.Notifications.Add(new Notification
                {
                    BookingId = _booking.Id,
                    Message = $"Ваше бронирование №{_booking.Id} переведено в статус: {newStatus}",
                    Type = "StatusUpdate",
                    ScheduledAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            await DisplayAlert("Успех", $"Статус обновлён на: {newStatus}", "OK");
            await LoadBookingDataAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось обновить статус: {ex.Message}", "OK");
        }
    }

    private async Task<string> GetStaffNameAsync()
    {
        var savedId = await SecureStorage.Default.GetAsync("user_session_id");
        if (int.TryParse(savedId, out int userId))
        {
            var staff = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return staff != null ? $"{staff.FirstName} {staff.LastName}" : "Сотрудник";
        }
        return "Сотрудник";
    }
}