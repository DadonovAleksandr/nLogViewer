using System.Windows.Input;
using nLogViewer.Infrastructure.Commands;
using nLogViewer.ViewModels.Base;

namespace nLogViewer.ViewModels;

public class LogViewerViewModel : BaseViewModel
{

    public LogViewerViewModel()
    {
        
        
        #region commands
        AutoscrollCommand = new LambdaCommand(OnEnableAutoscrollCommandExecuted, CanEnableAutoscrollCommandExecute);
        ClearCommand = new LambdaCommand(OnClearCommandExecuted, CanClearCommandExecute);
        PauseCommand = new LambdaCommand(OnPauseCommandExecuted, CanPauseCommandExecute);
        #endregion
    }
    
    
    
    #region Commands

    #region Включение автопрокрутки
    public ICommand AutoscrollCommand { get; }
    private void OnEnableAutoscrollCommandExecuted(object p)
    {
        EnabledAutoscroll = !EnabledAutoscroll;
    }
    private bool CanEnableAutoscrollCommandExecute(object p) => true;
    #endregion
    
    #region Очистка лога
    public ICommand ClearCommand { get; }
    private void OnClearCommandExecuted(object p)
    {
        ;
    }
    private bool CanClearCommandExecute(object p) => true;
    #endregion

    #region Пауза отображения лога
    public ICommand PauseCommand { get; }
    private void OnPauseCommandExecuted(object p)
    {
        EnabledPause = !EnabledPause;
    }
    private bool CanPauseCommandExecute(object p) => true;
    #endregion

    #endregion
    
    
    #region Toolbar
    
    private bool _autoscroll;
    /// <summary>
    /// Включение автопрокрутки
    /// </summary>
    public bool EnabledAutoscroll
    {
        get => _autoscroll;
        set => Set(ref _autoscroll, value);
    }
    
    private bool _pause;
    /// <summary>
    /// Включение паузы
    /// </summary>
    public bool EnabledPause
    {
        get => _pause;
        set => Set(ref _pause, value);
    }
    
    #endregion

}