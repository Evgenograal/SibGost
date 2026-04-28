using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;
using StaffApp.Services;

namespace StaffApp.Pages;

public partial class ManageRoomsPage : ContentPage
{
    private readonly SibGostDbContext _context;
    private readonly ImgBBService _imgBBService;
    private Room? _editingRoom;
    private byte[]? _selectedImageBytes;
    private string? _uploadedImageUrl;

    public ManageRoomsPage(SibGostDbContext context, ImgBBService imgBBService)
    {
        InitializeComponent();
        _context = context;
        _imgBBService = imgBBService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
        PickerStatus.SelectedItem = "Свободен";
    }

    private async Task LoadDataAsync()
    {
        try
        {
            var rooms = await _context.Rooms
                .Include(r => r.Category)
                .Include(r => r.RoomAmenities).ThenInclude(ra => ra.Amenity)
                .ToListAsync();

            RoomsCollection.ItemsSource = rooms;

            var categories = await _context.Categories.ToListAsync();
            PickerCategory.ItemsSource = categories;
            PickerCategory.ItemDisplayBinding = new Binding("Name");

            var amenities = await _context.Amenities.ToListAsync();
            AmenitiesCollection.ItemsSource = amenities;

            if (categories.Any()) PickerCategory.SelectedIndex = 0;
            if (PickerFloor.ItemsSource != null) PickerFloor.SelectedIndex = 0;
            PickerStatus.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }
    private void ToggleFormClicked(object sender, EventArgs e)
    {
        FormFrame.IsVisible = !FormFrame.IsVisible;
        BtnToggleForm.Text = FormFrame.IsVisible ? "❌ Скрыть форму" : "➕ Добавить номер";
    }

    private async void PickImage(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "public.image" } },
                { DevicePlatform.Android, new[] { "image/*" } },
                { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png" } }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions { FileTypes = customFileType });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                _selectedImageBytes = ms.ToArray();

                ImagePreview.Source = ImageSource.FromStream(() => new MemoryStream(_selectedImageBytes));
                ImageFrame.IsVisible = true;
                LblImageStatus.Text = "⏳ Загрузка...";

                _uploadedImageUrl = await _imgBBService.UploadAsync(_selectedImageBytes, result.FileName);
                LblImageStatus.Text = _uploadedImageUrl != null ? $"✅ Загружено" : "❌ Ошибка загрузки";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private async void Save(object sender, EventArgs e)
    {
        var roomNumber = EntryRoomNumber.Text?.Trim();
        if (string.IsNullOrWhiteSpace(roomNumber))
        {
            await DisplayAlert("Ошибка", "Введите номер комнаты", "OK"); return;
        }

        var selectedCategory = PickerCategory.SelectedItem as Category;
        if (selectedCategory == null) { await DisplayAlert("Ошибка", "Выберите категорию", "OK"); return; }
        if (!int.TryParse(PickerFloor.SelectedItem?.ToString(), out int floor)) floor = 1;

        try
        {
            if (_editingRoom != null)
            {
                _editingRoom.RoomNumber = roomNumber;
                _editingRoom.Floor = floor;
                _editingRoom.CategoryId = selectedCategory.Id;
                _editingRoom.Status = PickerStatus.SelectedItem?.ToString() ?? "Свободен";
                _editingRoom.IsActive = CheckIsActive.IsChecked;
                if (!string.IsNullOrEmpty(_uploadedImageUrl)) _editingRoom.ImgUrl = _uploadedImageUrl;

                var selectedAmenities = AmenitiesCollection.SelectedItems.Cast<Amenity>().ToList();
                _editingRoom.RoomAmenities.Clear();
                foreach (var a in selectedAmenities) _editingRoom.RoomAmenities.Add(new RoomAmenity { RoomId = _editingRoom.Id, AmenityId = a.Id });

                _context.Rooms.Update(_editingRoom);
            }
            else
            {
                var newRoom = new Room
                {
                    RoomNumber = roomNumber,
                    Floor = floor,
                    CategoryId = selectedCategory.Id,
                    Status = PickerStatus.SelectedItem?.ToString() ?? "Свободен",
                    IsActive = CheckIsActive.IsChecked,
                    ImgUrl = _uploadedImageUrl ?? "",
                    RoomAmenities = new List<RoomAmenity>()
                };

                foreach (var a in AmenitiesCollection.SelectedItems.Cast<Amenity>())
                    newRoom.RoomAmenities.Add(new RoomAmenity { AmenityId = a.Id });

                await _context.Rooms.AddAsync(newRoom);
            }

            await _context.SaveChangesAsync();
            ResetForm();
            await LoadDataAsync();
            await DisplayAlert("Успех", "Сохранено", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }

    private void Edit(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Room room)
        {
            _editingRoom = room;
            _uploadedImageUrl = room.ImgUrl;
            EntryRoomNumber.Text = room.RoomNumber;
            PickerFloor.SelectedItem = room.Floor.ToString();
            PickerCategory.SelectedItem = PickerCategory.ItemsSource.Cast<Category>().FirstOrDefault(c => c.Id == room.CategoryId);
            PickerStatus.SelectedItem = room.Status;
            CheckIsActive.IsChecked = room.IsActive;

            AmenitiesCollection.SelectedItems.Clear();
            foreach (var ra in room.RoomAmenities)
            {
                var amenity = AmenitiesCollection.ItemsSource.Cast<Amenity>().FirstOrDefault(a => a.Id == ra.AmenityId);
                if (amenity != null) AmenitiesCollection.SelectedItems.Add(amenity);
            }

            string statusToSelect = room.Status;
            if (room.Status == "Free") statusToSelect = "Свободен";
            else if (room.Status == "Occupied") statusToSelect = "Занят";
            else if (room.Status == "Maintenance") statusToSelect = "Ремонт";

            PickerStatus.SelectedItem = statusToSelect;
            CheckIsActive.IsChecked = room.IsActive;

            if (!string.IsNullOrEmpty(room.ImgUrl))
            {
                ImagePreview.Source = room.ImgUrl;
                ImageFrame.IsVisible = true;
                LblImageStatus.Text = "✅ Текущее фото";
            }

            FormFrame.IsVisible = true;
            BtnToggleForm.Text = "❌ Скрыть форму";
            BtnCancel.IsVisible = true;
        }
    }

    private async void Delete(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Room room)
        {
            if (await DisplayAlert("Удалить?", $"Комната {room.RoomNumber}?", "Да", "Нет"))
            {
                try { _context.Rooms.Remove(room); await _context.SaveChangesAsync(); await LoadDataAsync(); }
                catch (Exception ex) { await DisplayAlert("Ошибка", ex.Message, "OK"); }
            }
        }
    }

    private void Cancel(object sender, EventArgs e) => ResetForm();

    private void ResetForm()
    {
        EntryRoomNumber.Text = ""; PickerFloor.SelectedIndex = 0; PickerCategory.SelectedIndex = 0;
        PickerStatus.SelectedIndex = 0; CheckIsActive.IsChecked = true;
        AmenitiesCollection.SelectedItems.Clear();
        ImageFrame.IsVisible = false; ImagePreview.Source = null; LblImageStatus.Text = "";
        _editingRoom = null; _selectedImageBytes = null; _uploadedImageUrl = null;
        BtnCancel.IsVisible = false;

        FormFrame.IsVisible = false;
        BtnToggleForm.Text = "➕ Добавить номер";
    }
}