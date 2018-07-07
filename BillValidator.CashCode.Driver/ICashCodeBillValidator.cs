using BillValidator.CashCode.Driver.BillsDefinition;
using BillValidator.CashCode.Driver.Models;

namespace BillValidator.CashCode.Driver
{
    public interface ICashCodeBillValidator
    {
        bool IsConnected { get; }

        event BillCassetteHandler BillCassetteStatusEvent;
        event BillReceivedHandler BillReceived;
        event BillStackingHandler BillStacking;

        CashCodeBillValidatorException Connect(string portName, IBillsDefinition billsDefinition, int handleBillStackingTimeout = 10000);
        CashCodeBillValidatorException Disable();
        void Dispose();
        CashCodeBillValidatorException Enable();
        CashCodeBillValidatorException PowerUp();
        void StartListening();
        void StopListening();
    }
}