using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PasswordKeeper.App.Services;
using PasswordKeeper.Core.Models;

namespace PasswordKeeper.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IVaultService _vault;
    private readonly IClipboardService _clipboard;
    private readonly IIdleTimerService _idle;
    private readonly IDialogService _dialog;

    public ObservableCollection<CategoryItemViewModel> CategoryItems { get; } = new();
    public ObservableCollection<VaultEntry> FilteredEntries { get; } = new();

    public EntryDetailViewModel EntryDetail { get; }
    public PasswordGeneratorViewModel Generator { get; }

    [ObservableProperty] private VaultEntry? selectedEntry;
    [ObservableProperty] private CategoryItemViewModel? selectedCategoryItem;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private int totalEntryCount;

    public event EventHandler? LockRequested;

    public MainViewModel(
        IVaultService vault,
        IClipboardService clipboard,
        IIdleTimerService idle,
        IDialogService dialog,
        EntryDetailViewModel entryDetail,
        PasswordGeneratorViewModel generator)
    {
        _vault = vault;
        _clipboard = clipboard;
        _idle = idle;
        _dialog = dialog;
        EntryDetail = entryDetail;
        Generator = generator;

        EntryDetail.Saved += OnEntrySaved;
        EntryDetail.Deleted += OnEntryDeleted;
        _idle.IdleThresholdExceeded += (_, _) => RequestLock();

        ReloadFromVault();
        _idle.Start();
    }

    private void OnEntrySaved(object? sender, VaultEntry e) => _ = SaveAndRefreshAsync();

    private async Task SaveAndRefreshAsync()
    {
        try
        {
            await _vault.SaveAsync();
            RecomputeCounts();
            RebuildFilteredEntries();
            StatusMessage = "Saved.";
        }
        catch (Exception ex)
        {
            _dialog.ShowError("Save failed", ex.Message);
        }
    }

    private void OnEntryDeleted(object? sender, VaultEntry e)
    {
        var vault = _vault.CurrentVault;
        if (vault is null) return;
        if (!_dialog.Confirm("Delete entry", $"Delete \"{e.Title}\"?")) return;

        vault.Entries.RemoveAll(x => x.Id == e.Id);
        SelectedEntry = null;
        EntryDetail.LoadEntry(null, vault.Categories);
        _ = SaveAndRefreshAsync();
    }

    [RelayCommand]
    private void AddEntry()
    {
        var vault = _vault.CurrentVault;
        if (vault is null) return;
        var entry = new VaultEntry
        {
            Title = "New entry",
            CategoryId = (SelectedCategoryItem is { IsAll: false }) ? SelectedCategoryItem.Id : null
        };
        vault.Entries.Add(entry);
        RecomputeCounts();
        RebuildFilteredEntries();
        SelectedEntry = entry;
    }

    [RelayCommand]
    private void AddCategory()
    {
        var vault = _vault.CurrentVault;
        if (vault is null) return;
        var cat = new Category { Name = "New category" };
        vault.Categories.Add(cat);
        var item = new CategoryItemViewModel(cat) { IsEditing = true };
        CategoryItems.Add(item);
        SelectedCategoryItem = item;
        _ = SaveAndRefreshAsync();
    }

    [RelayCommand]
    private void RenameSelectedCategory()
    {
        if (SelectedCategoryItem is null || SelectedCategoryItem.IsAll) return;
        SelectedCategoryItem.IsEditing = true;
    }

    [RelayCommand]
    private void DeleteCategory()
    {
        var vault = _vault.CurrentVault;
        if (vault is null || SelectedCategoryItem is null || SelectedCategoryItem.IsAll) return;
        if (!_dialog.Confirm("Delete category", $"Delete category \"{SelectedCategoryItem.Name}\"? Entries in it will become uncategorized.")) return;

        var catId = SelectedCategoryItem.Id;
        foreach (var entry in vault.Entries.Where(e => e.CategoryId == catId))
            entry.CategoryId = null;
        vault.Categories.RemoveAll(c => c.Id == catId);
        CategoryItems.Remove(SelectedCategoryItem);
        SelectedCategoryItem = CategoryItems.FirstOrDefault(c => c.IsAll);
        _ = SaveAndRefreshAsync();
    }

    [RelayCommand]
    private void Lock() => RequestLock();

    [RelayCommand]
    private void PersistCategoryRename(CategoryItemViewModel? item)
    {
        if (item is not null) item.IsEditing = false;
        _ = SaveAndRefreshAsync();
    }

    [RelayCommand]
    private void UseGeneratedPassword()
    {
        if (!string.IsNullOrEmpty(Generator.GeneratedPassword))
            EntryDetail.ApplyGeneratedPassword(Generator.GeneratedPassword);
    }

    private void RequestLock()
    {
        _idle.Stop();
        _clipboard.ClearIfOurs();
        LockRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ReloadFromVault()
    {
        CategoryItems.Clear();
        var vault = _vault.CurrentVault;
        if (vault is null) return;

        // Synthetic "All entries" row first
        var allRow = new CategoryItemViewModel(null);
        CategoryItems.Add(allRow);
        foreach (var c in vault.Categories) CategoryItems.Add(new CategoryItemViewModel(c));

        RecomputeCounts();
        SelectedCategoryItem = allRow;
        RebuildFilteredEntries();
        EntryDetail.LoadEntry(null, vault.Categories);
    }

    private void RecomputeCounts()
    {
        var vault = _vault.CurrentVault;
        if (vault is null) { TotalEntryCount = 0; return; }
        TotalEntryCount = vault.Entries.Count;
        foreach (var item in CategoryItems)
        {
            item.Count = item.IsAll
                ? vault.Entries.Count
                : vault.Entries.Count(e => e.CategoryId == item.Id);
        }
    }

    private void RebuildFilteredEntries()
    {
        FilteredEntries.Clear();
        var vault = _vault.CurrentVault;
        if (vault is null) return;

        IEnumerable<VaultEntry> q = vault.Entries;
        if (SelectedCategoryItem is not null && !SelectedCategoryItem.IsAll)
            q = q.Where(e => e.CategoryId == SelectedCategoryItem.Id);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string s = SearchText.Trim();
            q = q.Where(e =>
                Contains(e.Title, s) || Contains(e.Username, s) ||
                Contains(e.Url, s)   || Contains(e.Notes, s));
        }

        foreach (var entry in q.OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase))
            FilteredEntries.Add(entry);
    }

    private static bool Contains(string haystack, string needle)
        => haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    partial void OnSelectedCategoryItemChanged(CategoryItemViewModel? value)
        => RebuildFilteredEntries();

    partial void OnSearchTextChanged(string value) => RebuildFilteredEntries();

    partial void OnSelectedEntryChanged(VaultEntry? value)
    {
        var vault = _vault.CurrentVault;
        EntryDetail.LoadEntry(value, vault?.Categories ?? new List<Category>());
    }
}
