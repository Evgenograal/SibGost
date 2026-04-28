using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;
using StaffApp.Security;

namespace StaffApp.Pages;

public partial class ManageUsersPage : ContentPage
{
	private readonly SibGostDbContext _context;
	private User? _editingUser;
    public bool IsAdmin { get; set; }
    public ManageUsersPage(SibGostDbContext context)
	{
		InitializeComponent();
		_context = context;
        BindingContext = this;
    }

	protected override async void OnAppearing()
	{
		base.OnAppearing();
        var currentUserIdStr = await SecureStorage.Default.GetAsync("user_session_id");
        if (int.TryParse(currentUserIdStr, out int currentUserId))
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);
            IsAdmin = currentUser?.RoleId == 2;
        }
        await LoadRolesAsync();
		await LoadUsersAsync();
	}

	private async Task LoadRolesAsync()
	{
		var roles = await _context.Roles.ToListAsync();
		PickerRole.ItemsSource = roles;
		PickerRole.ItemDisplayBinding = new Binding("Name");
		if (roles.Any()) PickerRole.SelectedIndex = 0;
	}

	private async Task LoadUsersAsync()
	{
		try
		{
			var users = await _context.Users.Include(u => u.Role).ToListAsync();
			UsersCollection.ItemsSource = users;
		}
		catch (Exception ex)
		{
			await DisplayAlert("Ошибка", $"Не удалось загрузить пользователей: {ex.Message}", "OK");
		}
	}

    private async void Save(object sender, EventArgs e)
    {
        var login = EntryLogin.Text?.Trim();
        var password = EntryPassword.Text?.Trim();
        var firstName = EntryFirstName.Text?.Trim();
        var lastName = EntryLastName.Text?.Trim();
        var email = EntryEmail.Text?.Trim();
        var selectedRole = PickerRole.SelectedItem as AppRole;

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Ошибка", "Заполните обязательные поля (логин, имя, email)", "OK");
            return;
        }

        if (selectedRole == null)
        {
            await DisplayAlert("Ошибка", "Выберите роль", "OK");
            return;
        }

        if (_editingUser == null && string.IsNullOrWhiteSpace(password))
        {
            await DisplayAlert("Ошибка", "Введите пароль для нового пользователя", "OK");
            return;
        }

        try
        {
            if (_editingUser != null)
            {
                _editingUser.Login = login;
                _editingUser.FirstName = firstName;
                _editingUser.LastName = lastName;
                _editingUser.Email = email;
                _editingUser.RoleId = selectedRole.Id;

                if (!string.IsNullOrWhiteSpace(password))
                {
                    _editingUser.PasswordHash = PasswordHasher.Hash(password);
                }

                _context.Users.Update(_editingUser);
            }
            else
            {
                var newUser = new User
                {
                    Login = login,
                    PasswordHash = PasswordHasher.Hash(password),
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    RoleId = selectedRole.Id
                };

                await _context.Users.AddAsync(newUser);
            }

            await _context.SaveChangesAsync();
            ResetForm();
            await LoadUsersAsync();
            await DisplayAlert("Успех", "Пользователь сохранён", "OK");

            FormFrame.IsVisible = false;
            BtnToggleForm.Text = "➕ Добавить пользователя";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось сохранить: {ex.Message}", "OK");
        }
    }


    private void Edit(object sender, EventArgs e) 
	{
		if(sender is Button btn && btn.CommandParameter is User user) 
		{
			_editingUser = user;
            EntryLogin.Text = user.Login;
            EntryPassword.Text = "";
            EntryFirstName.Text = user.FirstName;
            EntryLastName.Text = user.LastName;
            EntryEmail.Text = user.Email;

			var roles = PickerRole.ItemsSource as List<AppRole>;
			if(roles != null)
			{
				var userRole = roles.FirstOrDefault(r => r.Id == user.RoleId);
				if(userRole != null) PickerRole.SelectedItem = userRole;
			}
	
			BtnCancel.IsVisible = false;
        }
	}

	private async void Delete(object sender, EventArgs e) 
	{
		if(sender is Button btn && btn.CommandParameter is User user) 
		{
			var currentUserId = await SecureStorage.Default.GetAsync("user_session_id");
            if (int.TryParse(currentUserId, out int currentId) && currentId == user.Id)
            {
                await DisplayAlert("Ошибка", "Нельзя удалить самого себя", "OK");
                return;
            }

            var confirm = await DisplayAlert("Подтверждение", $"Удалить пользователя \"{user.FirstName} {user.LastName}\"?\n\nВНИМАНИЕ: Все бронирования будут удалены!", "Да", "Нет");

            if (confirm)
            {
                try
                {
                    _context.Users.Remove(user);
                    await _context.SaveChangesAsync();
                    await LoadUsersAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось удалить: {ex.Message}", "OK");
                }
            }
        }
    }

	private void Cancel(object sender, EventArgs e) 
	{
		ResetForm();
        FormFrame.IsVisible = false;
        BtnToggleForm.Text = "➕ Добавить пользователя";
    }

	private void ResetForm() 
	{
		EntryLogin.Text = "";
        EntryPassword.Text = "";
        EntryFirstName.Text = "";
        EntryLastName.Text = "";
        EntryEmail.Text = "";
        PickerRole.SelectedIndex = 0;
        _editingUser = null;
        BtnCancel.IsVisible = false;
    }

    private void ToggleFormClick(object sender, EventArgs e) 
    {
        FormFrame.IsVisible = !FormFrame.IsVisible;
        BtnToggleForm.Text = FormFrame.IsVisible ? "❌ Скрыть форму" : "➕ Добавить пользователя";
    }
}