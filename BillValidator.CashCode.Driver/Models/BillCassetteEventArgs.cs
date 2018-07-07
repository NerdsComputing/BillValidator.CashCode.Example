using System;

namespace BillValidator.CashCode.Driver.Models
{
    public class BillCassetteEventArgs : EventArgs
    {
        public BillCassetteStatus Status { get; }

        public BillCassetteEventArgs(BillCassetteStatus status)
        {
            Status = status;
        }
    }
}