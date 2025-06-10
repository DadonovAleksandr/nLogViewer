using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using NLog;

namespace nLogViewer.ViewModels.Base;

public class BaseViewModel : INotifyPropertyChanged
{
    protected static Logger _log = LogManager.GetCurrentClassLogger();
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null)
    {
        var handlers = PropertyChanged;
        if(handlers is null) return;

        var invokationList = handlers.GetInvocationList();
        var arg = new PropertyChangedEventArgs(PropertyName);
        foreach (var action in invokationList)
            if (action.Target is DispatcherObject dispatcherObject)
                dispatcherObject.Dispatcher.Invoke(action, this, arg);
            else
                action.DynamicInvoke(this, arg);
    }

    protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string PropertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(PropertyName);
        return true;
    }
    
    /// <summary>
    /// Проксирует свойство к другому объекту с уведомлением об изменении
    /// </summary>
    /// <typeparam name="T">Тип свойства</typeparam>
    /// <param name="getter">Функция получения значения</param>
    /// <param name="setter">Действие установки значения</param>
    /// <param name="value">Новое значение</param>
    /// <param name="propertyName">Имя свойства</param>
    protected void SetProxied<T>(System.Func<T> getter, System.Action<T> setter, T value, [CallerMemberName] string propertyName = null)
    {
        if (Equals(getter(), value)) return;
        setter(value);
        OnPropertyChanged(propertyName);
    }
}