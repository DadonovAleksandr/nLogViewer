using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace nLogViewer.Tester.Views
{
    /// <summary>
    /// Логика взаимодействия для ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        #region Status

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(string),
                typeof(ProgressWindow),
                new PropertyMetadata(default(string))
                );


        [Description("Статусное сообщение")]
        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        #endregion

        #region ProgressValue

        public static readonly DependencyProperty ProgressValueProperty =
            DependencyProperty.Register(
                nameof(ProgressValue),
                typeof(double),
                typeof(ProgressWindow),
                new PropertyMetadata(double.NaN, OnProgressChanged)
                );

        private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var progressValue = (double)e.NewValue;
            var progressView = ((ProgressWindow)d).ProgressView;

            progressView.Value = progressValue;
            progressView.IsIndeterminate = double.IsNaN(progressValue);
        }

        [Description("Значение прогресса")]
        public double ProgressValue
        {
            get => (double)GetValue(ProgressValueProperty);
            set => SetValue(ProgressValueProperty, value);
        }

        #endregion


        #region Progress

        private IProgress<double> _progressInformer;
        public IProgress<double> ProgressInformer => _progressInformer ?? new Progress<double>(p => ProgressValue = p);

        #endregion

        #region Status

        private IProgress<string> _statusInformer;
        public IProgress<string> StatusInformer => _statusInformer ?? new Progress<string>(s => Status = s);

        #endregion

        private IProgress<(double Percent, string Message)> _progressStatusInformer;
        public IProgress<(double Percent, string Message)> ProgressStatusInformer => _progressStatusInformer
            ??= new Progress<(double Percent, string Message)>(p =>
            {
                ProgressValue = p.Percent;
                Status = p.Message;
            });

        private CancellationTokenSource _cancellation;
        public CancellationToken Cancellation
        {
            get
            {
                if (_cancellation != null) return _cancellation.Token;
                _cancellation = new CancellationTokenSource();
                CancelButton.IsEnabled = true;
                return _cancellation.Token;
            }

        }

        public ProgressWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => _cancellation?.Cancel();

    }
}