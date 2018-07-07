namespace BillValidator.CashCode.Example
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private readonly NavigationService.NavigationService _navigationService;
        public App()
        {
            Dispatcher.UnhandledException += OnDispatcherUnhandledException;
            _navigationService = new NavigationService.NavigationService();
        }

        void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            _navigationService.PopUpMessage("Error", $"{e.Exception.Message}");
            e.Handled = true;
        }
    }
}
