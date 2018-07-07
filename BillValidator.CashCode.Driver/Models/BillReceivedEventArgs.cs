using System;

namespace BillValidator.CashCode.Driver.Models
{
    /// <summary>
    /// Class of arguments for the event of receipt of a note in the bill acceptor
    /// </summary>
    public class BillReceivedEventArgs : EventArgs
    {

        public BillRecievedStatus Status { get; }
        public Bill Bill { get; }
        public CashCodeBillValidatorException Exception { get; }

        public BillReceivedEventArgs(BillRecievedStatus status, Bill bill, CashCodeBillValidatorException exception)
        {
            Status = status;
            Bill = bill;
            Exception = exception;
        }
    }
}