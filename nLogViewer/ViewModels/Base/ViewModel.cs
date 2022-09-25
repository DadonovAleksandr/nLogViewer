using System.ComponentModel;
using System.Runtime.CompilerServices;
using NLog;

namespace nLogViewer.ViewModels.Base;

public class BaseViewModel : INotifyPropertyChanged
{
    protected static Logger logger = LogManager.GetCurrentClassLogger();
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
    }

    protected virtual bool Set<T>(ref T field, T value, [CallerMemberName] string PropertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(PropertyName);
        return true;
    }
}