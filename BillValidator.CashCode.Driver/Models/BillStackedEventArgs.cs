namespace BillValidator.CashCode.Driver.Models
{
    public class BillStackedEventArgs
    {
        public bool HasToRejectBill { get; set; }

        public Bill Bill { get; }

        public BillStackedEventArgs(Bill bill)
        {
            Bill = bill;
            HasToRejectBill = false;
        }
    }
}