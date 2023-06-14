using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using NLog;

namespace nLogViewer.ViewModels.Base;

public class BaseViewModel : INotifyPropertyChanged
{
    protected static Logger _logger = LogManager.GetCurrentClassLogger();
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
}