using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;

namespace ClientApp.Pages;

public partial class EditProfilePage : ContentPage
{
	private readonly SibGostDbContext _dbContext;
	private User _currentUser;
	public EditProfilePage(SibGostDbContext dbContext)
	{
		InitializeComponent();
		_dbContext = dbContext;
        _currentUser = new User();
    }

	protected override async void OnAppearing() 
	{
		base.OnAppearing();
		await LoadCurrentUserData();
    }

	private async Task LoadCurrentUserData() 
	{
		var savedId = await SecureStorage.Default.GetAsync("user_session_id");

		if (string.IsNullOrEmpty(savedId))
		{
			await Shell.Current.GoToAsync("//LoginPage");
			return;
		}

		int userId = int.Parse(savedId);
		_currentUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

		if (_currentUser != null) 
		{
			EntryFirstName.Text = _currentUser.FirstName;
			EntryLastName.Text = _currentUser.LastName;
			EntryEmail.Text = _currentUser.Email;
		}
	}

    private async void SaveChanges(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EntryFirstName.Text))
        {
            await Shell.Current.DisplayAlert("Ошибка", "Имя обязательно для заполнения", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(EntryEmail.Text))
        {
            await Shell.Current.DisplayAlert("Ошибка", "Email обязателен для заполнения", "OK");
            return;
        }

        try
        {
            _currentUser.FirstName = EntryFirstName.Text.Trim();
            _currentUser.LastName = EntryLastName.Text?.Trim();
            _currentUser.Email = EntryEmail.Text.Trim();

            _dbContext.Users.Update(_currentUser);
            await _dbContext.SaveChangesAsync();

            await Shell.Current.DisplayAlert("Успех", "Данные успешно сохранены!", "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось сохранить: {ex.Message}", "OK");
        }
    }

    private async void Cancel(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}