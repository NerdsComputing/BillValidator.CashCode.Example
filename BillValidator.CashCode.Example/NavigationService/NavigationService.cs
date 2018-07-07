using System.Threading.Tasks;
using System.Windows;
using BillValidator.CashCode.Example.IoC;
using BillValidator.CashCode.Example.Pages.Base;
using BillValidator.CashCode.Example.Pages.MainWindow;
using MahApps.Metro.Controls.Dialogs;

namespace BillValidator.CashCode.Example.NavigationService
{
    public class NavigationService : INavigationService
    {
        public void NavigateToViewModel(BaseViewModel viewModel)
        {
            var mainViewModel = Container.Instance.Resolve<IMainWindowViewModel>();
            mainViewModel.ContainerViewModel.Dispose();
            mainViewModel.ContainerViewModel = viewModel;
        }

        public void PopUpViewModel(BaseViewModel viewModel)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Container.Instance.Resolve<IMainWindowViewModel>().ShowPopUpContentContainer(viewModel);
            });
        }

        public async Task<MessageDialogResult> PopUpMessage(string title, string message, MessageDialogStyle messageDialogStyle = MessageDialogStyle.Affirmative)
        {
            return await Application.Current.Dispatcher.Invoke(async () =>
            {
                return await Container.Instance.Resolve<IMainWindowViewModel>().ShopPopUpMessage(title, message, messageDialogStyle);
            });
        }
    }
}
