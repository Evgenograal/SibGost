using SibGost.Core.Models;
using SibGost.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace StaffApp.Pages;

[QueryProperty(nameof(CategoryId), "categoryId")]
[QueryProperty(nameof(CategoryName), "categoryName")]
public partial class ManagePricePoliciesPage : ContentPage
{
    private readonly SibGostDbContext _context;
    private PricePolicy? _editingPolicy;
    private int _categoryId;
    private string _categoryName = "";

    public int CategoryId { get => _categoryId; set => _categoryId = value; }
    public string CategoryName { get => _categoryName; set => _categoryName = Uri.UnescapeDataString(value); }

    public ManagePricePoliciesPage(SibGostDbContext context)
    {
        InitializeComponent();
        _context = context;
    }

    protected override bool OnBackButtonPressed()
    {
        Shell.Current.GoToAsync("ManageCategoriesPage");

        return true;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        LblCategoryTitle.Text = $"Категория: {_categoryName}";
        PickerStartDate.Date = DateTime.Today;
        PickerEndDate.Date = DateTime.Today.AddDays(7);
        await LoadPoliciesAsync();
    }

    private async Task LoadPoliciesAsync()
    {
        var policies = await _context.PricePolicies.Where(p => p.CategoryId == _categoryId).ToListAsync();
        PoliciesCollection.ItemsSource = policies;
    }

    private async void Save(object sender, EventArgs e)
    {
        if (!decimal.TryParse(EntryMultiplier.Text, out decimal multiplier))
        {
            await DisplayAlert("Ошибка", "Введите корректный множитель", "OK"); return;
        }

        var startDate = PickerStartDate.Date ?? DateTime.Today;
        var endDate = PickerEndDate.Date ?? DateTime.Today;
        if (endDate < startDate)
        {
            await DisplayAlert("Ошибка", "Дата окончания не может быть раньше начала", "OK"); return;
        }

        try
        {
            if (_editingPolicy != null)
            {
                _editingPolicy.StartDate = startDate;
                _editingPolicy.EndDate = endDate;
                _editingPolicy.Multiplier = multiplier;
                _editingPolicy.Description = EditorDescription.Text?.Trim();
                _editingPolicy.IsActive = CheckIsActive.IsChecked;
                _context.PricePolicies.Update(_editingPolicy);
            }
            else
            {
                await _context.PricePolicies.AddAsync(new PricePolicy
                {
                    CategoryId = _categoryId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Multiplier = multiplier,
                    Description = EditorDescription.Text?.Trim(),
                    IsActive = CheckIsActive.IsChecked
                });
            }

            await _context.SaveChangesAsync();
            ResetForm();
            await LoadPoliciesAsync();
            await DisplayAlert("Успех", "Политика сохранена", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось сохранить: {ex.Message}", "OK");
        }
    }

    private void Edit(object sender, EventArgs e) 
    {
        if(sender is Button btn && btn.CommandParameter is PricePolicy policy) 
        {
            PickerStartDate.Date = policy.StartDate;
            PickerEndDate.Date = policy.EndDate;
            EntryMultiplier.Text = policy.Multiplier.ToString();
            EditorDescription.Text = policy.Description;
            CheckIsActive.IsChecked = policy.IsActive;
            _editingPolicy = policy;
        }
    }

    private async void Delete(object sender, EventArgs e) 
    {
        if (sender is Button btn && btn.CommandParameter is PricePolicy policy)
        {
            if (await DisplayAlert("Удалить?", "Удалить эту политику цен?", "Да", "Нет"))
            {
                _context.PricePolicies.Remove(policy);
                await _context.SaveChangesAsync();
                await LoadPoliciesAsync();
            }
        }
    }

    private void ResetForm() 
    {
        EntryMultiplier.Text = "";
        EditorDescription.Text = "";
        CheckIsActive.IsChecked = true;
        PickerStartDate.Date = DateTime.Today;
        PickerEndDate.Date = DateTime.Today.AddDays(7);
        _editingPolicy = null;
    }
}