using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using nLogViewer.Services;

namespace nLogViewer.Views;

public partial class LogViewerView : UserControl
{
    private readonly IVirtualizationService _virtualizationService;
    
    public LogViewerView()
    {
        InitializeComponent();
        _virtualizationService = App.Host.Services.GetService<IVirtualizationService>();
        
        Loaded += OnLoaded;
    }
    
    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // Применяем настройки виртуализации при загрузке
        ApplyVirtualizationSettings();
        
        // Подписываемся на изменения коллекции для автоматической настройки
        if (DataContext is ViewModels.LogViewerVM.LogViewerViewModel viewModel && 
            viewModel.FiltredLogEntries is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged += OnCollectionChanged;
        }
    }
    
    private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        ApplyVirtualizationSettings();
    }
    
    private void ApplyVirtualizationSettings()
    {
        if (_virtualizationService == null) return;
        
        var dataGrid = FindName("LogDataGrid") as DataGrid;
        if (dataGrid == null) return;
        
        var itemCount = 0;
        if (DataContext is ViewModels.LogViewerVM.LogViewerViewModel viewModel && 
            viewModel.FiltredLogEntries != null)
        {
            // ICollectionView не имеет Count, проверяем тип коллекции
            if (viewModel.FiltredLogEntries is ICollection collection)
            {
                itemCount = collection.Count;
            }
            else
            {
                // Используем LINQ для подсчета, если коллекция не ICollection
                itemCount = viewModel.FiltredLogEntries.Cast<object>().Count();
            }
        }
        
        _virtualizationService.ApplyVirtualizationSettings(dataGrid, itemCount);
    }
    
    public void Dispose()
    {
        if (DataContext is ViewModels.LogViewerVM.LogViewerViewModel viewModel)
        {
            viewModel.Dispose();
            
            if (viewModel.FiltredLogEntries is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged -= OnCollectionChanged;
            }
        }
    }
}