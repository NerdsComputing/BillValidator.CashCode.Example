using System.Collections.Generic;
using BillValidator.CashCode.Driver.Models;

namespace BillValidator.CashCode.Driver.BillsDefinition
{
    public interface IBillsDefinition
    {
        Bill InvalidBill { get; }
        List<Bill> Bills { get; }
    }
}
