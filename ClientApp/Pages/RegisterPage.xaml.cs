using ClientApp.Services;
using System.Text.RegularExpressions;

namespace ClientApp.Pages;

public partial class RegisterPage : ContentPage
{
    private readonly IAuthService _authService;

    public RegisterPage(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var login = EntryLogin.Text?.Trim();
        var email = EntryEmail.Text?.Trim();
        var firstName = EntryFirstName.Text?.Trim();
        var lastName = EntryLastName.Text?.Trim();
        var pass = EntryPassword.Text;
        var confirm = EntryConfirmPass.Text;

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(pass) || string.IsNullOrWhiteSpace(email))
        {
            ShowError("Заполните логин, email и пароль");
            return;
        }

        if (pass != confirm)
        {
            ShowError("Пароли не совпадают");
            return;
        }

        string? passwordError = ValidatePasswordStrict(pass);
        if (passwordError != null)
        {
            ShowError(passwordError);
            return;
        }

        if (await _authService.IsLoginTaken(login))
        {
            ShowError("Такой логин уже существует");
            return;
        }

        string? emailError = ValidateEmailBeforeSend(email);
        if (emailError != null)
        {
            ShowError(emailError);
            return;
        }

        var code = new Random().Next(100000, 999999).ToString();
        try
        {
            LblError.Text = "Отправка кода на почту...";
            LblError.IsVisible = true;

            bool sent = await EmailService.SendCode(email, code);

            if (!sent)
            {
                ShowError("Не удалось отправить письмо. Проверьте интернет.");
                return;
            }

            var inputCode = await Shell.Current.DisplayPromptAsync(
                "Подтверждение почты",
                "Введите 6-значный код, отправленный на вашу почту:",
                placeholder: "123456",
                keyboard: Keyboard.Numeric);

            if (inputCode == code)
            {
                var result = await _authService.RegisterAsync(login, pass, email, firstName, lastName);

                if (result.IsSuccess)
                {
                    await Shell.Current.DisplayAlert("Успех", "Аккаунт создан!", "ОК");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    ShowError("Ошибка создания аккаунта");
                }
            }
            else
            {
                ShowError("Неверный код. Регистрация отменена.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ Email Error: {ex}");
        }
    }

    private string? ValidatePasswordStrict(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "Пароль не может быть пустым.";

        if (password.Length < 12)
            return "Пароль слишком короткий. Минимум 12 символов.";

        if (password.Length > 256)
            return "Пароль слишком длинный. Максимум 256 символов.";

        bool hasLower = false, hasUpper = false, hasDigit = false, hasSpecial = false;
        const string allowedSpecials = "@$!%*?#&";

        for (int i = 0; i < password.Length; i++)
        {
            char c = password[i];
            bool isLower = c >= 'a' && c <= 'z';
            bool isUpper = c >= 'A' && c <= 'Z';
            bool isDigit = c >= '0' && c <= '9';
            bool isSpecial = allowedSpecials.Contains(c);

            if (!isLower && !isUpper && !isDigit && !isSpecial)
                return "Пароль содержит недопустимые символы. Разрешены только английские буквы (A-Z, a-z), цифры и спецсимволы (@$!%*?#&).";

            if (isLower) hasLower = true;
            else if (isUpper) hasUpper = true;
            else if (isDigit) hasDigit = true;
            else if (isSpecial) hasSpecial = true;
        }

        if (!hasLower)
            return "Добавьте хотя бы одну строчную английскую букву (a-z).";
        if (!hasUpper)
            return "Добавьте хотя бы одну заглавную английскую букву (A-Z).";
        if (!hasDigit)
            return "Добавьте хотя бы одну цифру (0-9).";
        if (!hasSpecial)
            return "Добавьте хотя бы один спецсимвол из набора: @$!%*?&";

        for (int i = 0; i < password.Length - 3; i++)
        {
            if (password[i] == password[i + 1] &&
                password[i] == password[i + 2] &&
                password[i] == password[i + 3])
            {
                return "Пароль не должен содержать 4 и более одинаковых символов подряд.";
            }
        }

        return null;
    }

    private string? ValidateEmailBeforeSend(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Email не может быть пустым";

        if (email.Length < 6 || email.Length > 254)
            return "Некорректная длина email (от 6 до 254 символов)";

        if (email.Any(c => c > 127))
            return "Email не должен содержать кириллицу или спецсимволы";

        var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(email, pattern))
            return "Некорректный формат email";

        var parts = email.Split('@');
        if (parts.Length != 2)
            return "Некорректный формат email";

        var domain = parts[1];
        if (domain.StartsWith(".") || domain.EndsWith("."))
            return "Некорректный домен в email";

        var tld = domain.Split('.').Last();
        if (tld.Length < 2)
            return "Некорректный домен верхнего уровня";

        return null;
    }

    private void ShowError(string msg)
    {
        LblError.Text = msg;
        LblError.IsVisible = true;
    }
}