using System.Windows;
using nLogViewer.ViewModels;

namespace nLogViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        
        public MainWindow()
        {
            InitializeComponent();
            Loaded += (sender, args) =>
            {
                if(!(DataContext is MainWindowViewModel mainVm)) return;
                mainVm.InitLogViewerControl(LogViewersControl);
            };
            
            Closing += (sender, args) =>
            {
                if(!(DataContext is MainWindowViewModel mainVm)) return;
                mainVm.OnExit();
            };
        }
        
        
    }
}