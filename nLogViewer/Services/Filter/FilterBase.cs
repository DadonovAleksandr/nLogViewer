using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace nLogViewer.Services.Filter;

/// <summary>
/// Базовый класс для фильтров с автоматическим вызовом RefreshFilter при изменении свойств
/// </summary>
internal abstract class FilterBase
{
    public event RefreshFilter RefreshFilter;
    
    /// <summary>
    /// Словарь для хранения значений свойств
    /// </summary>
    private readonly Dictionary<string, object> _properties = new();
    
    /// <summary>
    /// Устанавливает значение свойства и вызывает RefreshFilter при изменении
    /// </summary>
    /// <typeparam name="T">Тип свойства</typeparam>
    /// <param name="value">Новое значение</param>
    /// <param name="propertyName">Имя свойства (автоматически подставляется)</param>
    /// <returns>true если значение изменилось</returns>
    protected bool SetProperty<T>(T value, [CallerMemberName] string propertyName = "")
    {
        if (_properties.TryGetValue(propertyName, out var currentValue) && 
            EqualityComparer<T>.Default.Equals((T)currentValue, value))
        {
            return false;
        }
        
        _properties[propertyName] = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    
    /// <summary>
    /// Получает значение свойства
    /// </summary>
    /// <typeparam name="T">Тип свойства</typeparam>
    /// <param name="defaultValue">Значение по умолчанию</param>
    /// <param name="propertyName">Имя свойства (автоматически подставляется)</param>
    /// <returns>Значение свойства</returns>
    protected T GetProperty<T>(T defaultValue = default, [CallerMemberName] string propertyName = "")
    {
        if (_properties.TryGetValue(propertyName, out var value))
        {
            return (T)value;
        }
        
        return defaultValue;
    }
    
    /// <summary>
    /// Вызывается при изменении свойства
    /// </summary>
    /// <param name="propertyName">Имя изменившегося свойства</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        RefreshFilter?.Invoke();
    }
}