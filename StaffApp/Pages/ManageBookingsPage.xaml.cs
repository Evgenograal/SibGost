using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;

namespace StaffApp.Pages;

public partial class ManageBookingsPage : ContentPage
{
	private readonly SibGostDbContext _context;
	private List<Booking> _allBookings = new();

	public ManageBookingsPage(SibGostDbContext context)
	{
		InitializeComponent();
		_context = context;
		PickerStatusFilter.SelectedIndex = 0;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadBookingsAsync();
	}

	private async Task LoadBookingsAsync()
	{
		try
		{
			_allBookings = await _context.Bookings.Include(b => b.User).Include(b => b.Room).OrderByDescending(b => b.CreatedAt).ToListAsync();
			ApplyFilter();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Ошибка", $"Не удалось загрузить бронирования: {ex.Message}", "OK");
		}
	}
	private void ApplyFilter()
	{
		var selectedStatus = PickerStatusFilter.SelectedItem?.ToString() ?? "Все";

		var filtered = selectedStatus == "Все" ? _allBookings : _allBookings.Where(b => b.Status == selectedStatus).ToList();

		BookingsCollection.ItemsSource = filtered;
	}

	private void FilterChanged(object sender, EventArgs e) => ApplyFilter();

	private async void BookingSelected(object sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is Booking booking)
		{
			BookingsCollection.SelectedItem = null;

			await Shell.Current.GoToAsync($"BookingDetailPage?bookingId={booking.Id}");
		}
	}

	private async void CreateBooking(object sender, EventArgs e) 
	{
		await Shell.Current.GoToAsync("CreateBookingPage");
	}
}