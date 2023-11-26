using System.Windows;
using System.Windows.Media;

namespace nLogViewer.Tester.Views
{
    public enum ErrorWindowType { Error, Warning }

    /// <summary>
    /// Логика взаимодействия для ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : Window
    {
        public ErrorWindow()
        {
            InitializeComponent();
        }

        public ErrorWindow(string msg, ErrorWindowType type = ErrorWindowType.Error)
        {
            InitializeComponent();

            if (string.IsNullOrEmpty(msg)) return;

            ExceptionDescription.Text = msg;

            (Title, ExceptionTitle.Text, OkBtn.Background, OkBtn.Foreground) = type switch
            {
                ErrorWindowType.Error => ("Error window",
                    "В результате выполнения программы было сгенерировано исключение:",
                    Brushes.Red, Brushes.White),
                ErrorWindowType.Warning => ("Warning window",
                    "В результате выполнения программы было сгенерировано предупреждение:",
                    Brushes.Yellow, Brushes.Black)
            };
        }
    }
}