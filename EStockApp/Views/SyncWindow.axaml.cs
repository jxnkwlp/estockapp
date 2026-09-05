using Avalonia.Controls;
using Avalonia.Threading;
using EStockApp.ViewModels;
using System.Collections.Specialized;

namespace EStockApp;

public partial class SyncWindow : Window
{
    private SyncWindowViewModel? _viewModel;

    public SyncWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        UnbindViewModel();
        _viewModel = DataContext as SyncWindowViewModel;
        if (_viewModel != null)
            _viewModel.Records.CollectionChanged += OnRecordsChanged;
    }

    private void OnClosed(object? sender, System.EventArgs e)
    {
        UnbindViewModel();
    }

    private void UnbindViewModel()
    {
        if (_viewModel != null)
        {
            _viewModel.Records.CollectionChanged -= OnRecordsChanged;
            _viewModel = null;
        }
    }

    private void OnRecordsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems is not { Count: > 0 })
            return;

        var lastItem = e.NewItems[e.NewItems.Count - 1];

        // Defer until after the new row is realized/laid out; immediate ScrollIntoView often no-ops.
        Dispatcher.UIThread.Post(() =>
        {
            if (!ReferenceEquals(DataContext, _viewModel))
                return;

            SyncRecordsGrid.SelectedItem = lastItem;
            SyncRecordsGrid.ScrollIntoView(lastItem, null);
        }, DispatcherPriority.Loaded);
    }
}
