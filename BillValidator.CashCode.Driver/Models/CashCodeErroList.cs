using System;
using System.Collections.Generic;
using System.Linq;

namespace BillValidator.CashCode.Driver.Models
{
    public sealed class CashCodeErroList
    {
        public CashCodeBillValidatorException GeneralError { get; set; }
        public List<CashCodeBillValidatorException> Errors { get; }

        public CashCodeErroList()
        {
            GeneralError = new CashCodeBillValidatorException(Int32.MaxValue, "Unmapped error occured");

            Errors = new List<CashCodeBillValidatorException>
            {
                new CashCodeBillValidatorException (100000, "Unknown error"),
                new CashCodeBillValidatorException (100010, "Error opening Com-port"),
                new CashCodeBillValidatorException (100020, "Com port is not open"),
                new CashCodeBillValidatorException (100030, "Error sending bill inclusion commands"),
                new CashCodeBillValidatorException (100040, "An error occurred when sending the bill acceptor command. The POWER UP command was not received from the bill acceptor."),
                new CashCodeBillValidatorException (100050, "An error occurred when sending the bill acceptor command. The ACK team was not received from the bill acceptor."),
                new CashCodeBillValidatorException (100060, "An error occurred when sending the bill acceptor command. The INITIALIZE command was not received from the bill acceptor."),
                new CashCodeBillValidatorException (100070, "Error checking the status of the bill acceptor. The plug is removed."),
                new CashCodeBillValidatorException (100080, "Bill validation status check. Stacker is full."),
                new CashCodeBillValidatorException (100090, "An error checking the status of the bill acceptor. In the validator, a bill was stuck."),
                new CashCodeBillValidatorException (100100, "Error checking the status of the bill acceptor. A bill was stuck in the stack."),
                new CashCodeBillValidatorException (100110, "Error checking the status of the bill acceptor. Fake note."),
                new CashCodeBillValidatorException (100120, "Error checking the status of the bill acceptor. The previous note has not yet hit the stack and is in the recognition mechanism."),
                new CashCodeBillValidatorException (100130, "Error in the bill acceptor operation. Failure when the mechanism of the stacker."),
                new CashCodeBillValidatorException (100140, "Error in the bill acceptor operation. Failure in the speed of transfer of the bill to the stacker."),
                new CashCodeBillValidatorException (100150, "Error in the bill acceptor operation. The transfer of the bill to the stacker failed."),
                new CashCodeBillValidatorException (100160, "Error in the bill acceptor operation. The mechanism for equalizing bills failed."),
                new CashCodeBillValidatorException (100170, "Error in the bill acceptor operation. The stacker failed."),
                new CashCodeBillValidatorException (100180, "Error in the bill acceptor operation. Optical sensor failure."),
                new CashCodeBillValidatorException (100190, "Error in the bill acceptor operation. Malfunction of the inductor channel."),
                new CashCodeBillValidatorException (100200, "Error in the bill acceptor operation. The stackability check channel failed."),
                // Bug recognition errors
                new CashCodeBillValidatorException (0x60, "Rejecting due to Insertion"),
                new CashCodeBillValidatorException (0x61, "Rejecting due to Magnetic"),
                new CashCodeBillValidatorException (0x62, "Rejecting due to Remained bill in head"),
                new CashCodeBillValidatorException (0x63, "Rejecting due to Multiplying"),
                new CashCodeBillValidatorException (0x64, "Rejecting due to Conveying"),
                new CashCodeBillValidatorException (0x65, "Rejecting due to Identification1"),
                new CashCodeBillValidatorException (0x66, "Rejecting due to Verification"),
                new CashCodeBillValidatorException (0x67, "Rejecting due to Optic"),
                new CashCodeBillValidatorException (0x68, "Rejecting due to Inhibit"),
                new CashCodeBillValidatorException (0x69, "Rejecting due to Capacity"),
                new CashCodeBillValidatorException (0x6A, "Rejecting due to Operation"),
                new CashCodeBillValidatorException (0x6C, "Rejecting due to Length")
            };
        }

        public CashCodeBillValidatorException GetErrorByCode(int code)
        {
            var error = Errors.FirstOrDefault(x => x.Code == code);

            return error ?? GeneralError;
        }
    }
}
