using nLogViewer.Tester.Infrastructure.Commands;
using nLogViewer.Tester.Model.AppSettings.AppConfig;
using nLogViewer.Tester.Service.UserDialogService;
using nLogViewer.Tester.ViewModels.Base;
using ProjectVersionInfo;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using nLogViewer.Model;
using nLogViewer.Tester.Model.Events;
using nLogViewer.Tester.Model.ViewerRunner;

namespace nLogViewer.Tester.ViewModels.MainWindowVm;

internal class MainWindowViewModel : BaseViewModel
{
    private readonly IAppConfig _appConfig;
    private readonly IUserDialogService _userDialogService;
    private readonly EventGenerator _eventGenerator;
    /* ------------------------------------------------------------------------------------------------------------ */
    public MainWindowViewModel(IUserDialogService userDialogService)
    {
        _log.Debug($"Вызов конструктора {GetType().Name}");
        _appConfig = AppConfig.GetConfigFromDefaultPath();
        _userDialogService = userDialogService;
        _eventGenerator = new EventGenerator();

        var prjVersion = new ProjectVersion(Assembly.GetExecutingAssembly());
        Title = $"{AppConst.Get().AppDesciption} {prjVersion.Version}";

        #region Commands
        Exit = new RelayCommand(OnExitExecuted, CanExitExecute);
        GenEvent = new RelayCommand(OnGenEventExecuted, CanGenEventExecute);
        GenRandom = new RelayCommand(OnGenRandomExecuted, CanGenRandomExecute);
        #endregion
        
        new ViewerRunner().Start();
    }

    /// <summary>
    /// Действия выполняемые при закрытии основной формы
    /// </summary>
    public void OnExit()
    {
        //_projectConfigurationRepository?.Save();
    }
    /* ------------------------------------------------------------------------------------------------------------ */
    #region Commands

    public ICommand GenEvent { get; }

    private void OnGenEventExecuted(object p)
    {
        if (p is LogEntryType type)
        {
            _eventGenerator.Generate(type);
            LogCount = _eventGenerator.LogCount;
        }
    }
    private bool CanGenEventExecute(object p) => true;
    
    public ICommand GenRandom { get; }
    private void OnGenRandomExecuted(object p)
    {
        if (int.TryParse(p.ToString(), out int count))
        {
            _eventGenerator.RandomGenerate(count);
            LogCount = _eventGenerator.LogCount;
        }
    }
    private bool CanGenRandomExecute(object p) => true;

    #region Exit
    public ICommand Exit { get; }
    private void OnExitExecuted(object p) => Application.Current.Shutdown();
    private bool CanExitExecute(object p) => true;
    #endregion

    #endregion

    /* ------------------------------------------------------------------------------------------------------------ */

    /// <summary>
    /// Заголовок окна
    /// </summary>
    public string Title { get => Get<string>(); set => Set(value); }

    /// <summary>
    /// Счетчик сообщений
    /// </summary>
    public int LogCount { get => Get<int>(); set => Set(value); }

}