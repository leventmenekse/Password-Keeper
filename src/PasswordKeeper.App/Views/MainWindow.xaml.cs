using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PasswordKeeper.App.ViewModels;

namespace PasswordKeeper.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    private void OnRenameCategoryClick(object sender, RoutedEventArgs e)
    {
        var item = _vm.SelectedCategoryItem;
        if (item is null) return;
        item.IsEditing = true;

        // Force the style trigger + layout to apply before we try to focus.
        CategoriesListBox.UpdateLayout();

        Dispatcher.BeginInvoke(new Action(() =>
        {
            var container = CategoriesListBox.ItemContainerGenerator.ContainerFromItem(item) as ListBoxItem;
            if (container is null) return;
            var tb = FindVisualChild<TextBox>(container);
            if (tb is null) return;
            Keyboard.Focus(tb);
            tb.SelectAll();
        }), DispatcherPriority.Input);
    }

    private void OnCategoryNameLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is CategoryItemViewModel item && item.IsEditing)
            _vm.PersistCategoryRenameCommand.Execute(item);
    }

    private void OnCategoryNameKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb) return;
        if (e.Key == Key.Enter)
        {
            var expr = tb.GetBindingExpression(TextBox.TextProperty);
            expr?.UpdateSource();
            if (tb.DataContext is CategoryItemViewModel item)
            {
                item.IsEditing = false;
                _vm.PersistCategoryRenameCommand.Execute(item);
            }
            Keyboard.ClearFocus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            var expr = tb.GetBindingExpression(TextBox.TextProperty);
            expr?.UpdateTarget();
            if (tb.DataContext is CategoryItemViewModel item) item.IsEditing = false;
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            var deeper = FindVisualChild<T>(child);
            if (deeper is not null) return deeper;
        }
        return null;
    }
}
