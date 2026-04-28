using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;
using System.Globalization;

namespace StaffApp.Pages;

public partial class CreateBookingPage : ContentPage
{
	private readonly SibGostDbContext _context;
	private List<User> _allClients = new();
	private List<Room> _availableRooms = new();
    private List<PricePolicy> _allPricePolicies = new();

    public CreateBookingPage(SibGostDbContext context)
	{
		InitializeComponent();
		_context = context;

		PickerCheckIn.MinimumDate = DateTime.Today;
		PickerCheckIn.Date = DateTime.Today;
		PickerCheckOut.MinimumDate = DateTime.Today.AddDays(1);
		PickerCheckOut.Date = DateTime.Today.AddDays(1);
		PickerStstus.SelectedIndex = 1;

        PickerCheckIn.DateSelected += (s, e) => { CalculatePrice(); UpdateAvailableRooms(); };
        PickerCheckOut.DateSelected += (s, e) => { CalculatePrice(); UpdateAvailableRooms(); };
        PickerRoom.SelectedIndexChanged += (s, e) => CalculatePrice();
    }

    protected override async void OnAppearing() 
	{
		base.OnAppearing();
		await LoadClientsAsync();
		await LoadAvailableRoomsAsync();
		await LoadPricePoliciesAsync();
        CalculatePrice();
	}

    private async Task LoadPricePoliciesAsync() 
	{
		_allPricePolicies = await _context.PricePolicies.Where(p => p.IsActive).ToListAsync();
	}

    private async Task LoadClientsAsync() 
	{
		_allClients = await _context.Users.Where(u => u.RoleId == 1).OrderBy(u => u.Login).ToListAsync();

		PickerClient.ItemsSource = _allClients;
	}

	private async Task LoadAvailableRoomsAsync() 
	{
		var checkIn = PickerCheckIn.Date;
		var checkOut = PickerCheckOut.Date;

		var allRooms = await _context.Rooms.Include(r => r.Category).Where(r => r.IsActive).ToListAsync();

		var bookedRoomsId = await _context.Bookings.Where(b => b.CheckIn <  checkOut && b.CheckOut < checkIn).Select(b => b.RoomId).ToListAsync();
        
		_availableRooms = allRooms.Where(r => !bookedRoomsId.Contains(r.Id)).ToList();
		PickerRoom.ItemsSource = _availableRooms;

		if (!_availableRooms.Any()) 
		{
			LblRoomAvailability.Text = "Нет свободных комнат на выбранные даты";
			LblRoomAvailability.IsVisible = true;
        }
		else 
		{
			LblRoomAvailability.IsVisible = false;
		}
    }

	private async void ClientSearchChange(object sender, EventArgs e) 
	{
		var query = SearchClient.Text?.Trim() ?? "";
		var filtered = string.IsNullOrWhiteSpace(query) ? _allClients : _allClients.Where(c => c.Login.ToLower().Contains(query) || c.FirstName.ToLower().Contains(query) || c.LastName.ToLower().Contains(query)).ToList();
    }

	private async void FindClient(object sender, EventArgs e) 
	{
		var query = SearchClient.Text?.Trim().ToLower();
		if (string.IsNullOrWhiteSpace(query)) 
		{
			PickerClient.SelectedItem = null;
			return;
		}

		var found = _allClients.FirstOrDefault(c => c.FirstName.ToLower().Contains(query) || c.LastName.ToLower().Contains(query) || c.Login.ToLower().Contains(query));

		if (found != null) 
		{
			PickerClient.SelectedItem = found;
			SearchClient.Text = "";
		}
        else
        {
            await DisplayAlert("Не найдено", "Клиенты с такими данными не найдены", "ОК");
        }
    }

	private void DateChanged(object sender, DateChangedEventArgs e) => CalculatePrice();

	private void CalculatePrice() 
	{
		var room = PickerRoom.SelectedItem as Room;
		var checkIn = PickerCheckIn.Date;
		var checkOut = PickerCheckOut.Date;

        if (room?.Category == null || checkOut <= checkIn)
        {
            LblNights.Text = "";
            LblTotalPrice.Text = "0 ₽";
            return;
        }

        var nights = (checkOut.Value - checkIn.Value).Days;
        var basePrice = room.Category.BasePrice;
        decimal totalPrice = 0;

        for (var date = checkIn; date < checkOut; date = date.Value.AddDays(1))
        {
            var policy = _allPricePolicies
                .FirstOrDefault(p =>
                    p.CategoryId == room.CategoryId &&
                    date >= p.StartDate.Date &&
                    date <= p.EndDate.Date &&
                    p.IsActive);

            var multiplier = policy?.Multiplier ?? 1.0m;
            totalPrice += basePrice * multiplier;
        }

        LblNights.Text = $"🌙 {nights} ночей";
        LblTotalPrice.Text = $"{totalPrice:N0} ₽";

        var hasDiscounts = _allPricePolicies.Any(p =>
            p.CategoryId == room.CategoryId &&
            p.Multiplier != 1.0m &&
            checkIn <= p.EndDate &&
            checkOut >= p.StartDate);

        LblPriceNote.Text = hasDiscounts
            ? "* Учтены сезонные коэффициенты"
            : "* Окончательная цена рассчитывается при заселении";
    }

    private async void UpdateAvailableRooms()
    {
        await LoadAvailableRoomsAsync();

        var selectedRoom = PickerRoom.SelectedItem as Room;
        if (selectedRoom != null && !_availableRooms.Contains(selectedRoom))
        {
            PickerRoom.SelectedItem = null;
            CalculatePrice();
        }
    }

    private async void CreateBooking(object sender, EventArgs e) 
	{
		var client = PickerClient.SelectedItem as User;
		var room = PickerRoom.SelectedItem as Room;
		var checkIn = PickerCheckIn.Date;
		var checkOut = PickerCheckOut.Date;
		var status = PickerStstus.SelectedItem.ToString() ?? "Ожидание";

        if (client == null)
        {
            await DisplayAlert("Ошибка", "Выберите клиента", "ОК");
            return;
        }

        if (room == null)
        {
            await DisplayAlert("Ошибка", "Выберите комнату", "ОК");
            return;
        }

        if (checkOut <= checkIn)
        {
            await DisplayAlert("Ошибка", "Дата выезда должна быть позже заезда", "ОК");
            return;
        }

		try 
		{
			var newBooking = new Booking 
			{
                UserId = client.Id,
                RoomId = room.Id,
                CheckIn = checkIn.Value,
                CheckOut = checkOut.Value,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

			await _context.Bookings.AddAsync(newBooking);

            newBooking.History.Add(new BookingHistory
            {
                BookingId = newBooking.Id,
                Action = $"Бронирование создано: {status}",
                ChangeBy = "Сотрудник",
                ChangeAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            await DisplayAlert("Успех", $"Бронирование создано!", "ОК");
            await Shell.Current.GoToAsync("..");
        }
		catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось создать бронирование: {ex.Message}", "ОК");
        }
    }

	private async void Cancel(object sender, EventArgs e) 
	{
		await Shell.Current.GoToAsync("..");
	}
}