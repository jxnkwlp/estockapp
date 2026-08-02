using System.ComponentModel;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using EStockApp.Models;
using EStockApp.ViewModels;

namespace EStockApp.Behaviors;

public sealed class DataGridUxBehavior : Behavior<DataGrid>
{
    private MainWindowViewModel? _vm;
    private DataGridColumn? _contextColumn;

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is null)
            return;

        AssociatedObject.DataContextChanged += OnDataContextChanged;
        AssociatedObject.Sorting += OnSorting;
        AssociatedObject.CellPointerPressed += OnCellPointerPressed;
        AssociatedObject.ContextRequested += OnContextRequested;
        BindViewModel(AssociatedObject.DataContext as MainWindowViewModel);
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.DataContextChanged -= OnDataContextChanged;
            AssociatedObject.Sorting -= OnSorting;
            AssociatedObject.CellPointerPressed -= OnCellPointerPressed;
            AssociatedObject.ContextRequested -= OnContextRequested;
        }

        UnbindViewModel();
        base.OnDetaching();
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        BindViewModel(AssociatedObject?.DataContext as MainWindowViewModel);
    }

    private void BindViewModel(MainWindowViewModel? vm)
    {
        UnbindViewModel();
        _vm = vm;
        if (_vm != null)
            _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void UnbindViewModel()
    {
        if (_vm != null)
        {
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
            _vm = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.Items))
            Dispatcher.UIThread.Post(ReapplySort, DispatcherPriority.Loaded);
    }

    private void OnSorting(object? sender, DataGridColumnEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_vm is null || AssociatedObject?.CollectionView?.SortDescriptions is not { } sorts)
                return;

            if (sorts.Count == 0)
            {
                _vm.ClearSort();
                return;
            }

            _vm.SetSort(sorts[0].PropertyPath, sorts[0].Direction);
        }, DispatcherPriority.Loaded);
    }

    private void ReapplySort()
    {
        if (_vm is null || string.IsNullOrEmpty(_vm.SortMemberPath) || AssociatedObject?.CollectionView is null)
            return;

        var sorts = AssociatedObject.CollectionView.SortDescriptions;
        sorts.Clear();
        sorts.Add(DataGridSortDescription.FromPath(_vm.SortMemberPath, _vm.SortDirection));
    }

    private void OnCellPointerPressed(object? sender, DataGridCellPointerPressedEventArgs e)
    {
        if (!e.PointerPressedEventArgs.GetCurrentPoint(AssociatedObject).Properties.IsRightButtonPressed)
            return;

        _contextColumn = e.Column;
        if (AssociatedObject != null && e.Row.DataContext is not null)
            AssociatedObject.SelectedItem = e.Row.DataContext;
    }

    private void OnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (_vm is null || AssociatedObject is null)
            return;

        var source = e.Source as Control;
        var row = source?.FindAncestorOfType<DataGridRow>();
        var item = row?.DataContext as ProductItemModel ?? AssociatedObject.SelectedItem as ProductItemModel;
        if (item is null)
            return;

        AssociatedObject.SelectedItem = item;

        var columnTag = (_contextColumn ?? AssociatedObject.CurrentColumn)?.Tag as string;
        var actions = _vm.BuildRowContextActions(item, columnTag);
        if (actions.Count == 0)
            return;

        var menu = new ContextMenu();
        foreach (var action in actions)
        {
            if (action.IsSeparator)
            {
                menu.Items.Add(new Separator());
                continue;
            }

            menu.Items.Add(new MenuItem
            {
                Header = action.Header,
                Command = action.Command,
                CommandParameter = action.CommandParameter,
            });
        }

        AssociatedObject.ContextMenu = menu;
        if (e.TryGetPosition(AssociatedObject, out _))
        {
            menu.Open(AssociatedObject);
            e.Handled = true;
        }
    }
}
