using Microsoft.EntityFrameworkCore;
using SibGost.Core.Data;
using SibGost.Core.Models;

namespace StaffApp.Pages;

public partial class ManageCategoriesPage : ContentPage
{
	private readonly SibGostDbContext _context;
	private Category? _editingCategory;

    public ManageCategoriesPage(SibGostDbContext context)
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
		await LoadCategoriesAsync();
    }

	private async Task LoadCategoriesAsync() 
	{
		var categories = await _context.Categories.ToListAsync();
		CategoriesCollection.ItemsSource = categories;
	}

	private async void Save(object sender, EventArgs e)
	{
        var name = EntryName.Text?.Trim();
        var description = EditorDescription.Text?.Trim();

        if (!decimal.TryParse(EntryBasePrice.Text, out decimal basePrice))
        {
            await DisplayAlert("Ошибка", "Введите корректную цену", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Ошибка", "Введите название категории", "OK");
            return;
        }

        try
        {
            if (_editingCategory != null)
            {
                _editingCategory.Name = name;
                _editingCategory.Description = description;
                _editingCategory.BasePrice = basePrice;
                _context.Categories.Update(_editingCategory);
            }
            else
            {
                var newCategory = new Category
                {
                    Name = name,
                    Description = description,
                    BasePrice = basePrice
                };
                await _context.Categories.AddAsync(newCategory);
            }

            await _context.SaveChangesAsync();

            EntryName.Text = "";
            EditorDescription.Text = "";
            EntryBasePrice.Text = "";
            _editingCategory = null;

            await LoadCategoriesAsync();
            await DisplayAlert("Успех", "Категория сохранена", "OK");
        }
        catch (Exception ex)
        {
            var errorMessage = $"Не удалось сохранить:\n{ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nДетали:\n{ex.InnerException.Message}";
            }

            System.Diagnostics.Debug.WriteLine($"❌ SAVE ERROR: {ex}");
            System.Diagnostics.Debug.WriteLine($"❌ INNER: {ex.InnerException?.Message}");
            System.Diagnostics.Debug.WriteLine($"❌ STACK: {ex.StackTrace}");

            await DisplayAlert("Ошибка", errorMessage, "OK");
        }
    }

	private void Edit(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Category cat)
        {
            EntryName.Text = cat.Name;
            EditorDescription.Text = cat.Description;
            EntryBasePrice.Text = cat.BasePrice.ToString();
            _editingCategory = cat;
        }
    }


    private async void Delete(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is Category cat)
        {
            var confirm = await DisplayAlert("Удалить?", $"Удалить \"{cat.Name}\"?", "Да", "Нет");
            if (confirm)
            {
                try
                {
                    _context.Categories.Remove(cat);
                    await _context.SaveChangesAsync();
                    await LoadCategoriesAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Ошибка", $"Не удалось удалить. Возможно, категория используется в номерах.\n{ex.Message}", "OK");
                }
            }
        }
    }

	private async void PoliciesPrice(object sender, EventArgs e) 
	{
		if(sender is Button btn && btn.CommandParameter is Category cat) 
		{
            await Shell.Current.GoToAsync($"ManagePricePoliciesPage?categoryId={cat.Id}&categoryName={Uri.EscapeDataString(cat.Name)}");
        }
    }
}