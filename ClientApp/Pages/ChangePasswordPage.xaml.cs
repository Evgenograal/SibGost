using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using Microsoft.Maui.Storage;
using System.Text.RegularExpressions;
using ClientApp.Security;

namespace ClientApp.Pages;

public partial class ChangePasswordPage : ContentPage
{
    private readonly SibGostDbContext _dbContext;

    public ChangePasswordPage(SibGostDbContext dbContext)
    {
        InitializeComponent();
        _dbContext = dbContext;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var currentPass = EntryCurrentPass.Text;
        var newPass = EntryNewPass.Text;
        var confirmPass = EntryConfirmPass.Text;

        LblError.IsVisible = false;

        if (string.IsNullOrWhiteSpace(currentPass) || string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(confirmPass))
        {
            ShowError("Заполните все поля");
            return;
        }

        if (newPass != confirmPass)
        {
            ShowError("Новые пароли не совпадают");
            return;
        }

        var savedId = await SecureStorage.Default.GetAsync("user_session_id");
        if (string.IsNullOrEmpty(savedId))
        {
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        int userId = int.Parse(savedId);
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            ShowError("Пользователь не найден в базе");
            return;
        }

        if (!PasswordHasher.Verify(currentPass, user.PasswordHash))
        {
            ShowError("Текущий пароль введён неверно");
            return;
        }

        var pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$";
        if (!Regex.IsMatch(newPass, pattern))
        {
            ShowError("Новый пароль должен содержать:\n• Минимум 12 символов\n• Заглавные и строчные буквы\n• Цифры и спецсимволы (@$!%*?&)");
            return;
        }

        try
        {
            user.PasswordHash = PasswordHasher.Hash(newPass);
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            await Shell.Current.DisplayAlert("Успех", "Пароль успешно изменён", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка сохранения: {ex.Message}");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ShowError(string msg)
    {
        LblError.Text = msg;
        LblError.IsVisible = true;
    }
}