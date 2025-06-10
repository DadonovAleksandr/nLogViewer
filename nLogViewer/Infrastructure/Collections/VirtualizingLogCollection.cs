using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using NLog;
using nLogViewer.Infrastructure.Configuration;
using nLogViewer.Model;
using nLogViewer.ViewModels.LogViewerVM;

namespace nLogViewer.Infrastructure.Collections;

/// <summary>
/// Виртуализированная коллекция для отображения больших объемов логов
/// </summary>
internal class VirtualizingLogCollection : IList, INotifyCollectionChanged, INotifyPropertyChanged
{
    private static readonly Logger _log = LogManager.GetCurrentClassLogger();
    
    // Кэш для хранения загруженных элементов
    private readonly Dictionary<int, LogEntryView> _cache = new();
    
    // Размер страницы для загрузки данных
    private readonly int _pageSize;
    
    // Максимальное количество страниц в кэше
    private readonly int _maxCachedPages;
    
    // Конфигурация памяти
    private readonly MemoryConfiguration _memoryConfig;
    
    // Источник данных
    private readonly Func<int, int, IEnumerable<ILogEntry>> _fetchDataCallback;
    
    // Общее количество элементов
    private int _count;
    
    // Загруженные страницы
    private readonly HashSet<int> _loadedPages = new();

    public event NotifyCollectionChangedEventHandler CollectionChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    public VirtualizingLogCollection(Func<int, int, IEnumerable<ILogEntry>> fetchDataCallback, int totalCount, MemoryConfiguration memoryConfig = null)
    {
        _fetchDataCallback = fetchDataCallback ?? throw new ArgumentNullException(nameof(fetchDataCallback));
        _count = totalCount;
        _memoryConfig = memoryConfig;
        _pageSize = memoryConfig?.VirtualizationPageSize ?? 100;
        _maxCachedPages = memoryConfig?.MaxCachedPages ?? 10;
        _log.Debug($"Создана виртуализированная коллекция для {_count} элементов с размером страницы {_pageSize}");
    }

    public object? this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            // Проверяем кэш
            if (_cache.TryGetValue(index, out var cachedItem))
                return cachedItem;

            // Определяем страницу
            int pageIndex = index / _pageSize;
            
            // Загружаем страницу, если она еще не загружена
            if (!_loadedPages.Contains(pageIndex))
            {
                LoadPage(pageIndex);
            }

            // Возвращаем элемент из кэша
            return _cache.TryGetValue(index, out var item) ? item : null;
        }
        set => throw new NotSupportedException("Коллекция только для чтения");
    }

    private void LoadPage(int pageIndex)
    {
        _log.Trace($"Загрузка страницы {pageIndex}");
        
        int startIndex = pageIndex * _pageSize;
        int count = Math.Min(_pageSize, _count - startIndex);
        
        if (count <= 0) return;

        try
        {
            // Загружаем данные
            var items = _fetchDataCallback(startIndex, count).ToList();
            
            // Добавляем в кэш
            for (int i = 0; i < items.Count; i++)
            {
                _cache[startIndex + i] = new LogEntryView(items[i]);
            }
            
            _loadedPages.Add(pageIndex);
            
            // Очищаем старые страницы из кэша при превышении лимита
            if (_loadedPages.Count > _maxCachedPages)
            {
                CleanupCache();
            }
            
            _log.Trace($"Загружено {items.Count} элементов для страницы {pageIndex}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Ошибка загрузки страницы {pageIndex}");
        }
    }

    private void CleanupCache()
    {
        // Удаляем самые старые страницы
        var pagesToRemove = _loadedPages
            .OrderBy(p => p)
            .Take(_loadedPages.Count - _maxCachedPages)
            .ToList();

        foreach (var pageIndex in pagesToRemove)
        {
            _log.Trace($"Удаление страницы {pageIndex} из кэша");
            
            int startIndex = pageIndex * _pageSize;
            int endIndex = Math.Min(startIndex + _pageSize, _count);
            
            for (int i = startIndex; i < endIndex; i++)
            {
                _cache.Remove(i);
            }
            
            _loadedPages.Remove(pageIndex);
        }
    }

    public void UpdateTotalCount(int newCount)
    {
        if (_count == newCount) return;
        
        _log.Debug($"Обновление общего количества элементов: {_count} -> {newCount}");
        
        int oldCount = _count;
        _count = newCount;
        
        // Уведомляем об изменении свойств
        OnPropertyChanged(nameof(Count));
        
        // Для ListCollectionView безопаснее использовать Reset
        // так как он не поддерживает операции с диапазонами
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void InvalidateCache()
    {
        _log.Debug("Очистка всего кэша");
        _cache.Clear();
        _loadedPages.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    #region IList Implementation

    public int Count => _count;
    public bool IsReadOnly => true;
    public bool IsFixedSize => false;
    public bool IsSynchronized => false;
    public object SyncRoot => this;

    public int Add(object value) => throw new NotSupportedException();
    public void Clear() => throw new NotSupportedException();
    public bool Contains(object value) => false;
    public int IndexOf(object value) => -1;
    public void Insert(int index, object value) => throw new NotSupportedException();
    public void Remove(object value) => throw new NotSupportedException();
    public void RemoveAt(int index) => throw new NotSupportedException();
    public void CopyTo(Array array, int index) => throw new NotSupportedException();

    public IEnumerator GetEnumerator()
    {
        // Возвращаем только загруженные элементы для производительности
        for (int i = 0; i < _count; i++)
        {
            yield return this[i];
        }
    }

    #endregion

    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}