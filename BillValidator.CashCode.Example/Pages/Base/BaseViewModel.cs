using System;
using System.Windows;
using BillValidator.CashCode.Example.Eventor;
using BillValidator.CashCode.Example.IoC;
using BillValidator.CashCode.Example.NavigationService;
using MahApps.Metro.Controls.Dialogs;

namespace BillValidator.CashCode.Example.Pages.Base
{
    public class BaseViewModel : NotifyPropertyChanged, IDisposable
    {
        object _currentContext;
        BaseMetroDialog _currentDialog;
        private bool _isLoading;
        private string _title;
        private readonly INavigationService _navigationService;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged("IsLoading");
            }
        }

        private string _loadingMessage;
        public string LoadingMessage
        {
            get => _loadingMessage;
            set { _loadingMessage = value; OnPropertyChanged("LoadingMessage"); }
        }

        public string Title
        {
            get => _title;
            set => _title = value;
        }

        public DelegateCommand CloseCommand { get; set; }

        public INavigationService NavigationService => _navigationService;

        public BaseViewModel()
        {
            _navigationService = Container.Instance.Resolve<INavigationService>();
        }

        // Command handlers
        private void CloseCommandHandler()
        {
            try
            {
                DialogCoordinator.Instance.HideMetroDialogAsync(_currentContext, _currentDialog);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "UI Warning");
            }
        }

        public void BindCloseCommand(object givenContext, BaseMetroDialog givenDialog)
        {
            CloseCommand = new DelegateCommand(CloseCommandHandler);

            _currentContext = givenContext;
            _currentDialog = givenDialog;
        }

        public virtual void Dispose()
        {

        }
    }
}
