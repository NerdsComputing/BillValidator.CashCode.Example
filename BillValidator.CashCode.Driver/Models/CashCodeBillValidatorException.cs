using System;

namespace BillValidator.CashCode.Driver.Models
{
    public class CashCodeBillValidatorException : Exception
    {
        public int Code { get; }

        public CashCodeBillValidatorException(int code, string message) : base(message)
        {
            Code = code;
        }
    }
}
