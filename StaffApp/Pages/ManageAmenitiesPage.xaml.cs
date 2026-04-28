using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;

namespace StaffApp.Pages;

public partial class ManageAmenitiesPage : ContentPage
{
    private readonly SibGostDbContext _context;
    private Amenity? _editingAmenity;

    public ManageAmenitiesPage(SibGostDbContext context)
    {
        InitializeComponent();
        _context = context;
    }

    protected override bool OnBackButtonPressed()
    {
        Shell.Current.GoToAsync("//StaffDashboardPage");

        return true;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAmenitiesAsync();
    }

    private async Task LoadAmenitiesAsync()
    {
        try
        {
            var amenities = await _context.Amenities.ToListAsync();
            AmenitiesCollection.ItemsSource = amenities;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить удобства: {ex.Message}", "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var name = EntryName.Text?.Trim();
        var description = EditorDescription.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Ошибка", "Введите название удобства", "OK");
            return;
        }

        try
        {
            if (_editingAmenity != null)
            {
                _editingAmenity.Name = name;
                _editingAmenity.Description = description;
                _context.Amenities.Update(_editingAmenity);
            }
            else
            {
                await _context.Amenities.AddAsync(new Amenity { Name = name, Description = description });
            }

            await _context.SaveChangesAsync();

            EntryName.Text = "";
            EditorDescription.Text = "";
            _editingAmenity = null;

            await LoadAmenitiesAsync();
            await DisplayAlert("Успех", "Удобство сохранено", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось сохранить: {ex.Message}", "OK");
        }
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Amenity amenity)
        {
            EntryName.Text = amenity.Name;
            EditorDescription.Text = amenity.Description;
            _editingAmenity = amenity;
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Amenity amenity)
        {
            var confirm = await DisplayAlert("Подтверждение", $"Удалить удобство \"{amenity.Name}\"?", "Да", "Нет");
            if (confirm)
            {
                try
                {
                    _context.Amenities.Remove(amenity);
                    await _context.SaveChangesAsync();
                    await LoadAmenitiesAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось удалить: {ex.Message}", "OK");
                }
            }
        }
    }
}