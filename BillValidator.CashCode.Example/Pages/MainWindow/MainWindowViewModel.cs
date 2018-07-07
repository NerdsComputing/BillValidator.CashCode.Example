using System.Threading.Tasks;
using System.Windows;
using BillValidator.CashCode.Example.Pages.Base;
using BillValidator.CashCode.Example.Pages.MainExample;
using MahApps.Metro.Controls.Dialogs;

namespace BillValidator.CashCode.Example.Pages.MainWindow
{
    public class MainWindowViewModel : BaseViewModel, IMainWindowViewModel
    {
        private BaseViewModel _containerViewModel;
        public BaseViewModel ContainerViewModel
        {
            get => _containerViewModel;
            set
            {
                _containerViewModel = value;
                OnPropertyChanged(nameof(ContainerViewModel));
            }
        }

        public MainWindowViewModel(IMainExampleViewModel mainExampleViewModel)
        {
            ContainerViewModel = (BaseViewModel)mainExampleViewModel;
        }

        public void ShowPopUpContentContainer(BaseViewModel viewModel)
        {
            var customDialog = new CustomDialog { Title = viewModel.Title };

            viewModel.BindCloseCommand(this, customDialog);

            customDialog.Content = viewModel;

            Application.Current.Dispatcher.Invoke(() => DialogCoordinator.Instance.ShowMetroDialogAsync(this, customDialog));

        }

        public async Task<MessageDialogResult> ShopPopUpMessage(string title, string message, MessageDialogStyle messageDialogStyle = MessageDialogStyle.Affirmative)
        {
            return await Application.Current.Dispatcher.Invoke(async() => await DialogCoordinator.Instance.ShowMessageAsync(this, title, message, messageDialogStyle));
        }
    }
}
