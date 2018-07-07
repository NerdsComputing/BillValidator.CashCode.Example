using System.Threading.Tasks;

namespace BillValidator.CashCode.Driver.Models
{
    // Delegate of the event in the process of sending a note on the stack (Here you can do a refund)
    public delegate Task BillStackingHandler(object sender, BillStackedEventArgs e);
}