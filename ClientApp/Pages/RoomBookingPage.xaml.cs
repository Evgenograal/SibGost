using ClientApp.Services;
using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;
using System.Diagnostics;

namespace ClientApp.Pages;

[QueryProperty(nameof(RoomId), "roomId")]
public partial class RoomBookingPage : ContentPage
{
    private readonly SibGostDbContext _context;
    private Room _room;
    private decimal _totalPrice;
    private int _roomId;

    public int RoomId
    {
        get => _roomId;
        set
        {
            _roomId = value;
            _ = LoadRoomDataAsync();
        }
    }

    public RoomBookingPage(SibGostDbContext context)
    {
        InitializeComponent();
        _context = context;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_roomId > 0 && _room == null)
        {
            await LoadRoomDataAsync();
        }
    }

    private async Task LoadRoomDataAsync()
    {
        try
        {
            _room = await _context.Rooms.Include(r => r.Category).ThenInclude(c => c.PricePolicies).FirstOrDefaultAsync(r => r.Id == _roomId);

            if (_room == null || _room.Category == null)
            {
                await DisplayAlert("Ошибка", "Номер не найден", "ОК");
                await Shell.Current.GoToAsync("..");
                return;
            }

            LabelRoomInfo.Text = $"Номер {_room.RoomNumber}, этаж {_room.Floor}";
            LabelCategory.Text = _room.Category.Name;
            LabelBasePrice.Text = _room.Category.DisplayPrice;

            DatePickerCheckIn.Date = DateTime.Today;
            DatePickerCheckOut.Date = DateTime.Today.AddDays(1);

            CalculateTotalPrice();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить данные: {ex.Message}", "ОК");
        }
    }


    private void DateSelected(object sender, DateChangedEventArgs e)
    {
        if (DatePickerCheckOut.Date <= DatePickerCheckIn.Date)
        {
            DatePickerCheckOut.Date = DatePickerCheckIn.Date.Value.AddDays(1);
        }

        CalculateTotalPrice();
    }

    private void CalculateTotalPrice()
    {
        if (_room?.Category == null) return;

        var checkIn = DatePickerCheckIn.Date.Value;
        var checkOut = DatePickerCheckOut.Date.Value;
        var nights = (checkOut - checkIn).Days;

        if (nights <= 0)
        {
            LabelNights.Text = "Ночей: 0";
            FramePriceDetails.IsVisible = false;
            ButtonConfirm.IsEnabled = false;
            return;
        }

        LabelNights.Text = $"Ночей: {nights}";
        FramePriceDetails.IsVisible = true;
        StackPriceBreakdown.Children.Clear();

        _totalPrice = 0;

        for (var date = checkIn; date < checkOut; date = date.AddDays(1))
        {
            var priceForDay = _room.Category.CalculatePriceForDate(date);
            _totalPrice += priceForDay;

            var policy = _room.Category.PricePolicies?.FirstOrDefault(p =>
                p.IsActive && p.StartDate.Date <= date && p.EndDate.Date >= date);

            var policyInfo = policy != null && policy.Multiplier != 1.0m
                ? $" ({policy.Multiplier}x)"
                : "";

            StackPriceBreakdown.Children.Add(new Label
            {
                Text = $"{date:dd.MM}: {priceForDay:N0} ₽{policyInfo}",
                FontSize = 13,
                TextColor = Colors.Gray
            });
        }

        LabelTotalPrice.Text = $"{_totalPrice:N0} ₽";
        ButtonConfirm.IsEnabled = true;
    }

    private async void Confirm(object sender, EventArgs e)
    {
        if (_room == null) return;

        var saveId = await SecureStorage.Default.GetAsync("user_session_id");

        if(string.IsNullOrEmpty(saveId) || !int.TryParse(saveId, out int userId))
        {
            await DisplayAlert("Ошибка", "Вы не авторизованы. Войдите в аккаунт.", "ОК");
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        var checkIn = DatePickerCheckIn.Date;
        var checkOut = DatePickerCheckOut.Date;

        bool isAvailable = await IsRoomAvailableAsync(_room.Id, checkIn.Value, checkOut.Value);

        if (!isAvailable) 
        {
            var conflict = await _context.Bookings.FirstOrDefaultAsync(b => b.RoomId == _room.Id &&
                b.Status != "Отменено" &&
                b.Status != "Завершено" &&
                checkIn < b.CheckOut && b.CheckIn < checkOut);

            string conflictMsg = conflict != null ? $"\nУже забронировано: {conflict.CheckIn:dd.MM} - {conflict.CheckOut:dd.MM}" : "";

            await DisplayAlert("Номер занят", $"К сожалению, номер {_room.RoomNumber} уже забронирован на выбранные даты.{conflictMsg}\n\nПожалуйста, выберите другие даты или другой номер.", "ОК");
            return;
        }

        var result = await DisplayAlert("Подтверждение",
        $"Забронировать номер {_room.RoomNumber}?\n" +
        $"Даты: {checkIn:dd.MM} - {checkOut:dd.MM}\n" +
        $"Сумма: {_totalPrice:N0} ₽",
        "✅ Да", "❌ Нет");

        if (!result) return;

        try 
        {
            var booking = new Booking
            {
                UserId = userId,
                RoomId = _room.Id,
                CheckIn = checkIn.Value,
                CheckOut = checkOut.Value,
                Status = "Подтверждено",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.Bookings.AddAsync(booking);

            _room.Status = "Занят";
            _context.Rooms.Update(_room);

            await _context.SaveChangesAsync();

            var history = new BookingHistory
            {
                BookingId = booking.Id,
                Action = "Создано",
                ChangeBy = $"User_{userId}",
                ChangeAt = DateTime.UtcNow,
                NewValue = $"Бронь: {checkIn:dd.MM} - {checkOut:dd.MM}"
            };

            await _context.BookingHistories.AddAsync(history);
            await _context.SaveChangesAsync();

            await DisplayAlert("Успех", "Бронирование оформлено!", "ОК");
            await Shell.Current.GoToAsync("//RoomsPage");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Ошибка сохранения: {ex.Message}");
            await DisplayAlert("Ошибка", $"Не удалось: {ex.Message}", "ОК");
        }
    }

    private async Task<bool> IsRoomAvailableAsync(int roomId, DateTime checkIn, DateTime checkOut)
    {
        var existingBookings = await _context.Bookings.Where(b => b.RoomId == roomId &&
            b.Status != "Отменено" &&
            b.Status != "Завершено").ToListAsync();

        foreach (var booking in existingBookings) 
        {
            if(checkIn.Date < booking.CheckOut.Date && booking.CheckIn.Date < checkOut.Date) 
            {
                return false;
            }
        }

        return true;
    }
}