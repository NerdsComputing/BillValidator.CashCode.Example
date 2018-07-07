using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using BillValidator.CashCode.Driver;
using BillValidator.CashCode.Driver.BillsDefinition;
using BillValidator.CashCode.Driver.Models;
using BillValidator.CashCode.Example.Eventor;
using BillValidator.CashCode.Example.Pages.Base;
using MahApps.Metro.Controls.Dialogs;

namespace BillValidator.CashCode.Example.Pages.MainExample
{
    public class MainExampleViewModel :  BaseViewModel, IMainExampleViewModel
    {
        private readonly ICashCodeBillValidator _cashCodeBillValidator;

        private ObservableCollection<string> _logList;
        public ObservableCollection<string> LogList
        {
            get => _logList;
            set { _logList = value; OnPropertyChanged(nameof(LogList)); }
        }

        private string _billValidatorPort;
        public string BillValidatorPort
        {
            get => _billValidatorPort;
            set { _billValidatorPort = value; OnPropertyChanged(nameof(BillValidatorPort)); }
        }

        private int _collectedMoneySum;
        public int CollectedMoneySum
        {
            get => _collectedMoneySum;
            set { _collectedMoneySum = value; OnPropertyChanged(nameof(CollectedMoneySum)); }
        }

        private bool _isAutoAcceptBill;
        public bool IsAutoAcceptBill
        {
            get => _isAutoAcceptBill;
            set { _isAutoAcceptBill = value; OnPropertyChanged(nameof(IsAutoAcceptBill)); }
        }


        public MainExampleViewModel(ICashCodeBillValidator cashCodeBillValidator)
        {
            LogList = new ObservableCollection<string>();
            LogOperation("Application started");
            BillValidatorPort = "COM9";
            IsAutoAcceptBill = true;

            _cashCodeBillValidator = cashCodeBillValidator;
            _cashCodeBillValidator.BillReceived += HandleBillReceived;
            _cashCodeBillValidator.BillStacking += HandleBillStacking;
            _cashCodeBillValidator.BillCassetteStatusEvent += HandleBillCassetteStatusEvent;
        }

        public DelegateCommand ConnectCommand => new DelegateCommand(() =>
        {
            LogOperation("Connecting...");
            _cashCodeBillValidator.Connect(BillValidatorPort, new RomanianBillsDefinition());

            if (_cashCodeBillValidator.IsConnected)
            {
                _cashCodeBillValidator.PowerUp();
                _cashCodeBillValidator.StartListening();
                _cashCodeBillValidator.Enable();
                LogOperation("Connected!");
            }
            else
            {
                LogOperation("Could not connect to bill validator, check Device Manager for COM port number");
            }
        });

        public DelegateCommand DisconnectCommand => new DelegateCommand(() =>
        {
            _cashCodeBillValidator.Dispose();
            LogOperation("Disconnected");
        });

        public DelegateCommand EnableBillValidatorCommand => new DelegateCommand(() =>
        {
            _cashCodeBillValidator.Enable();
            LogOperation("Accepting bills enabled");
        });

        public DelegateCommand DisableBillValidatorCommand => new DelegateCommand(() =>
        {
            _cashCodeBillValidator.Disable();
            LogOperation("Accepting bills disabled");
        });

        public DelegateCommand ResetCollectedMoneySumCommand => new DelegateCommand(() =>
        {
            CollectedMoneySum = 0;
        }); 

        private void HandleBillCassetteStatusEvent(object sender, BillCassetteEventArgs e)
        {
            LogOperation($"Bill casette status changed to {e.Status}");
        }

        private async Task HandleBillStacking(object sender, BillStackedEventArgs e)
        {
            if (IsAutoAcceptBill)
            {
                e.HasToRejectBill = false;
                LogOperation($"Auto-accepted bill {e.Bill.Description}");
            }
            else
            {
                LogOperation("Choose if accept bill or not");
                var answer = await NavigationService.PopUpMessage("Question", $"Do you accept {e.Bill.Description}?", MessageDialogStyle.AffirmativeAndNegative);
                e.HasToRejectBill = answer == MessageDialogResult.Negative;
            }
        }

        private void HandleBillReceived(object sender, BillReceivedEventArgs e)
        {
            if (e.Status == BillRecievedStatus.Rejected)
            {
                LogOperation($"Bill {e.Bill.Description} rejected: {e.Exception?.Message}");
            }
            else if (e.Status == BillRecievedStatus.Accepted)
            {
                CollectedMoneySum += e.Bill.MoneyValue;
                LogOperation($"Bill {e.Bill.Description} accepted");
            }
        }

        private void LogOperation(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogList.Add($"{DateTime.Now.ToString("HH:mm:ss.fff")}>   {message}");
                OnPropertyChanged(nameof(LogList));
            });
            
        }
    }
}
