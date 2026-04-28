using ClientApp.Services;
using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;

namespace ClientApp.Pages;

public partial class RoomsPage : ContentPage
{
    private readonly SibGostDbContext _context;

    public RoomsPage(SibGostDbContext context)
    {
        InitializeComponent();
        _context = context;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRoomsAsync();
    }

    private async Task LoadRoomsAsync()
    {
        try
        {
            var rooms = await _context.Rooms
                .Include(r => r.Category).ThenInclude(c => c.PricePolicies)
                .Where(r => r.IsActive)
                .ToListAsync();

            RoomsCollection.ItemsSource = rooms;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", "Не удалось загрузить список номеров", "ОК");
        }
    }

    private async void BookClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Room room)
        {
            await Shell.Current.GoToAsync($"RoomBookingPage?roomId={room.Id}");
        }
    }
}