using System.Threading.Tasks;
using BillValidator.CashCode.Example.Pages.Base;
using MahApps.Metro.Controls.Dialogs;

namespace BillValidator.CashCode.Example.NavigationService
{
    public interface INavigationService 
    {
        void NavigateToViewModel(BaseViewModel viewModel);
        Task<MessageDialogResult> PopUpMessage(string title, string message, MessageDialogStyle messageDialogStyle = MessageDialogStyle.Affirmative);
        void PopUpViewModel(BaseViewModel viewModel);
    }
}
