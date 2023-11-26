using NLog;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace nLogViewer.Tester.ViewModels.Base
{
    internal class BaseViewModel : INotifyPropertyChanged
    {
        protected static Logger _log = LogManager.GetCurrentClassLogger();
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string PropertyName = null)
        {
            var handlers = PropertyChanged;
            if (handlers is null) return;

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

        private readonly Dictionary<string, object> _propertyValues = new();

        protected T? Get<T>([CallerMemberName] string PropertyName = null!) =>
            _propertyValues.TryGetValue(PropertyName, out var value) ? (T?)value! : default;

        protected bool Set<T>(T value, [CallerMemberName] string PropertyName = null)
        {
            if (_propertyValues.TryGetValue(PropertyName, out var oldValue) && Equals(oldValue, value))
                return false;

            _propertyValues[PropertyName] = value;
            OnPropertyChanged(PropertyName);
            return true;
        }
    }
}
