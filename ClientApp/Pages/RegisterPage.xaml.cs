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

        string? loginError = ValidateLoginStrict(login);
        if (loginError != null)
        {
            ShowError(loginError);
            return;
        }

        if (string.IsNullOrWhiteSpace(pass) || string.IsNullOrWhiteSpace(confirm))
        {
            ShowError("Введите пароль и его подтверждение");
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

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Введите email");
            return;
        }

        string? emailError = ValidateEmailBeforeSend(email);
        if (emailError != null)
        {
            ShowError(emailError);
            return;
        }

        if (await _authService.IsLoginTaken(login))
        {
            ShowError("Такой логин уже существует");
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

    private string? ValidateLoginStrict(string login)
    {
        if (string.IsNullOrWhiteSpace(login))
            return "Логин не может быть пустым.";

        if (login.Contains(' '))
            return "Логин не должен содержать пробелы.";

        if (login.Length < 4)
            return "Логин слишком короткий. Минимум 4 символа.";

        if (login.Length > 20)
            return "Логин слишком длинный. Максимум 20 символов.";

        char first = login[0];
        bool isFirstLetter = (first >= 'a' && first <= 'z') || (first >= 'A' && first <= 'Z');
        if (!isFirstLetter)
            return "Логин должен начинаться с английской буквы (A-Z или a-z).";

        char last = login[login.Length - 1];
        bool isLastAlphanumeric = ((last >= 'a' && last <= 'z') || (last >= 'A' && last <= 'Z') || (last >= '0' && last <= '9'));
        if (!isLastAlphanumeric)
            return "Логин должен заканчиваться на английскую букву или цифру.";

        for (int i = 0; i < login.Length; i++)
        {
            char c = login[i];
            bool isAllowed = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                             (c >= '0' && c <= '9') || c == '_' || c == '-';

            if (!isAllowed)
                return "Логин содержит недопустимые символы. Разрешены только английские буквы, цифры, '_' и '-'.";

            if (i > 0)
            {
                char prev = login[i - 1];
                if ((c == '_' && prev == '_') || (c == '-' && prev == '-'))
                    return "Запрещены подряд идущие '_' или '-'.";
            }
        }

        return null;
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
        const string allowedSpecials = "@$!%*?&";

        for (int i = 0; i < password.Length; i++)
        {
            char c = password[i];
            bool isLower = c >= 'a' && c <= 'z';
            bool isUpper = c >= 'A' && c <= 'Z';
            bool isDigit = c >= '0' && c <= '9';
            bool isSpecial = allowedSpecials.Contains(c);

            if (!isLower && !isUpper && !isDigit && !isSpecial)
                return "Пароль содержит недопустимые символы. Разрешены только английские буквы (A-Z, a-z), цифры и спецсимволы (@$!%*?&).";

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