using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;

namespace BillValidator.CashCode.Example.Pages.MainWindow
{
    interface IMainWindowViewModel
    {
        Base.BaseViewModel ContainerViewModel { get; set; }
        Task<MessageDialogResult> ShopPopUpMessage(string title, string message, MessageDialogStyle messageDialogStyle = MessageDialogStyle.Affirmative);
        void ShowPopUpContentContainer(Base.BaseViewModel viewModel);
    }
}
