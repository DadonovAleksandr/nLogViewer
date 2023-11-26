using nLogViewer.Tester.Infrastructure.Commands;
using nLogViewer.Tester.Model.AppSettings.AppConfig;
using nLogViewer.Tester.Service.UserDialogService;
using nLogViewer.Tester.ViewModels.Base;
using ProjectVersionInfo;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace nLogViewer.Tester.ViewModels.MainWindowVm
{
    internal class MainWindowViewModel : BaseViewModel
    {
        private readonly IAppConfig _appConfig;
        private readonly IUserDialogService _userDialogService;
        /* ------------------------------------------------------------------------------------------------------------ */
        public MainWindowViewModel(IUserDialogService userDialogService)
        {
            _log.Debug($"Вызов конструктора {GetType().Name}");
            _appConfig = AppConfig.GetConfigFromDefaultPath();
            _userDialogService = userDialogService;

            var prjVersion = new ProjectVersion(Assembly.GetExecutingAssembly());
            Title = $"{AppConst.Get().AppDesciption} {prjVersion.Version}";

            #region Commands
            Exit = new RelayCommand(OnExitExecuted, CanExitExecute);
            GenTrace = new RelayCommand(OnGenTraceExecuted, CanGenTraceExecute);
            GenDebug = new RelayCommand(OnGenDebugExecuted, CanGenDebugExecute);
            GenInfo = new RelayCommand(OnGenInfoExecuted, CanGenInfoExecute);
            GenWarn = new RelayCommand(OnGenWarnExecuted, CanGenWarnExecute);
            GenError = new RelayCommand(OnGenErrorExecuted, CanGenErrorExecute);
            GenRandom10 = new RelayCommand(OnGenRandom10Executed, CanGenRandom10Execute);
            GenRandom100 = new RelayCommand(OnGenRandom100Executed, CanGenRandom100Execute);
            GenRandom1000 = new RelayCommand(OnGenRandom1000Executed, CanGenRandom1000Execute);
            #endregion

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

        #region GenTrace
        public ICommand GenTrace { get; }
        private void OnGenTraceExecuted(object p) => _log.Trace($"Сообщение {++LogCount}");
        private bool CanGenTraceExecute(object p) => true;
        #endregion

        #region GenDebug
        public ICommand GenDebug { get; }
        private void OnGenDebugExecuted(object p) => _log.Debug($"Сообщение {++LogCount}");
        private bool CanGenDebugExecute(object p) => true;
        #endregion

        #region GenInfo
        public ICommand GenInfo { get; }
        private void OnGenInfoExecuted(object p) => _log.Info($"Сообщение {++LogCount}");
        private bool CanGenInfoExecute(object p) => true;
        #endregion

        #region GenWarn
        public ICommand GenWarn { get; }
        private void OnGenWarnExecuted(object p) => _log.Warn($"Сообщение {++LogCount}");
        private bool CanGenWarnExecute(object p) => true;
        #endregion

        #region GenError
        public ICommand GenError { get; }
        private void OnGenErrorExecuted(object p) => _log.Error($"Сообщение {++LogCount}");
        private bool CanGenErrorExecute(object p) => true;
        #endregion

        #region GenRandom10
        public ICommand GenRandom10 { get; }
        private void OnGenRandom10Executed(object p)
        {
            Random random = new Random();
            for(int i = 0; i < 10; i++)
            {
                var message = $"Сообщение {++LogCount}";
                switch(random.Next(0,5))
                {
                    case 0: _log.Trace(message); break;
                    case 1: _log.Debug(message); break;
                    case 2: _log.Info(message); break;
                    case 3: _log.Warn(message); break;
                    case 4: _log.Error(message); break;
                    case 5: _log.Fatal(message); break;
                }
            }
        }
        private bool CanGenRandom10Execute(object p) => true;
        #endregion

        #region GenRandom100
        public ICommand GenRandom100 { get; }
        private void OnGenRandom100Executed(object p)
        {
            Random random = new Random();
            for (int i = 0; i < 100; i++)
            {
                var message = $"Сообщение {++LogCount}";
                switch (random.Next(0, 5))
                {
                    case 0: _log.Trace(message); break;
                    case 1: _log.Debug(message); break;
                    case 2: _log.Info(message); break;
                    case 3: _log.Warn(message); break;
                    case 4: _log.Error(message); break;
                    case 5: _log.Fatal(message); break;
                }
            }
        }
        private bool CanGenRandom100Execute(object p) => true;
        #endregion

        #region GenRandom1000
        public ICommand GenRandom1000 { get; }
        private void OnGenRandom1000Executed(object p)
        {
            Random random = new Random();
            for (int i = 0; i < 1000; i++)
            {
                var message = $"Сообщение {++LogCount}";
                switch (random.Next(0, 5))
                {
                    case 0: _log.Trace(message); break;
                    case 1: _log.Debug(message); break;
                    case 2: _log.Info(message); break;
                    case 3: _log.Warn(message); break;
                    case 4: _log.Error(message); break;
                    case 5: _log.Fatal(message); break;
                }
            }
        }
        private bool CanGenRandom1000Execute(object p) => true;
        #endregion

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
}