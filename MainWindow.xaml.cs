using System.Windows;

namespace ServerBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TelegramBot _bot;
        private Pages.OurIPList _ourIPListPage;

        public MainWindow()
        {
            InitializeComponent();
            // Создаем экземпляр OurIPList один раз
            _ourIPListPage = new Pages.OurIPList();

            // Бот запускается только если токен задан в App.config.
            // Токен в коде не хранится, см. AppConfig.BotToken.
            string botToken = AppConfig.BotToken;
            if (!string.IsNullOrEmpty(botToken))
            {
                _bot = new TelegramBot(botToken, _ourIPListPage);
            }
        }

        private void OpenIPsPages(object sender, RoutedEventArgs e)
        {
            RootFrame.Navigate(_ourIPListPage);
        }

        private void OpenScaningPages(object sender, RoutedEventArgs e)
        {
            RootFrame.Navigate(new Pages.ShowAlliPList());
        }
    }
}
