using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ClientApp.Pages;

public partial class BookingsPage : ContentPage
{
	private readonly SibGostDbContext _context;
	public BookingsPage(SibGostDbContext context)
	{
		InitializeComponent();
		_context = context;
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
			var bookings = await _context.Bookings.Include(b => b.Room).ThenInclude(r => r.Category).OrderByDescending(b => b.CreatedAt).ToListAsync();

			BookingsCollection.ItemsSource = bookings;
		}
		catch (Exception ex)
		{
			await DisplayAlert("Ошибка", "Не удалось загрузить данные", "ОК");
		}
	}

	private async void Cancel(object sender, EventArgs e)
	{
		if (sender is Button btn && btn.CommandParameter is Booking booking)
		{
			var roomName = booking.Room?.RoomNumber ?? "Неизвестно";
			var confirm = await DisplayAlert("Отмена", $"Отменить бронь номера {roomName}?", "Да", "Нет");

			if (!confirm) return;

			try 
			{
				booking.Status = "Отменено";
                booking.UpdatedAt = DateTime.UtcNow;

                if (booking.Room != null)
                    booking.Room.Status = "Свободен";

                await _context.SaveChangesAsync();
                await LoadBookingsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка отмены: {ex.Message}");
            }
        }
    }
    private async void SearchClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//RoomsPage");
    }
}