using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Timers;
using BillValidator.CashCode.Driver.BillsDefinition;
using BillValidator.CashCode.Driver.Internal;
using BillValidator.CashCode.Driver.Models;
using Timer = System.Timers.Timer;

namespace BillValidator.CashCode.Driver
{
    public class CashCodeBillValidator : IDisposable, ICashCodeBillValidator
    {
        private IBillsDefinition _billsDefinition;

        #region Closed members

        private const int POLL_TIMEOUT = 200;    // Timeout for waiting for a response from the reader
        private int _handleBillStackingTimeout; // Timeout waiting to unlock

        private readonly byte[] ENABLE_BILL_TYPES_WITH_ESCROW = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

        private EventWaitHandle _synchCom;     // Variable synchronization of sending and reading data from the com port
        private List<byte> _receivedBytes;  // Received Bytes

        private CashCodeBillValidatorException _lastError;
        private bool _disposed;
        private string _comPortName;
        private bool _isConnected;
        private int _baudRate = 9600;
        private bool _isPowerUp;
        private bool _isListening;
        private bool _isEnableBills;
        private object _Locker;

        private SerialPort _comPort;
        private CashCodeErroList _errorList;

        private Timer _listener;  // Banner Receiver Timer

        bool _returnBill;

        BillCassetteStatus _cassettestatus = BillCassetteStatus.Inplace;
        #endregion

        #region Constructor

        #endregion

        #region Destructor

        ~CashCodeBillValidator() { Dispose(false); }

        // Implements IDisposable interface
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // We tell GC not to finalize the object after calling Dispose, since it is already released
        }

        // Dispose(bool disposing) is performed in two scenarios
        // If disposing = true, the Dispose method is called explicitly or implicitly from the user code
        // Managed and unmanaged resources can be released
        // If disposing = false, then the method can be called runtime from the finalizer
        // In this case only unmanaged resources can be released.
        private void Dispose(bool disposing)
        {
            // We'll check if the Dispose method has already been called
            if (_disposed)
                return;

            // If disposing = true, release all managed and unmanaged resources
            if (disposing)
            {
                // Here we free the managed resources
                try
                {
                    // Stop the timer, if it works
                    if (_isListening)
                    {
                        _listener.Enabled = _isListening = false;
                    }

                    _listener.Dispose();

                    // Dismiss the switch-off signal to the bill acceptor
                    if (_isConnected)
                    {
                        Disable();
                    }
                }
                catch
                {
                    // ignored
                }
            }


            // Put the appropriate methods to free unmanaged resources
            // If disposing = false, only the following code will be executed
            try
            {
                _comPort.Close();
            }
            catch
            {
                // ignored
            }

            _disposed = true;
        }

        #endregion

        #region Properties

        public bool IsConnected => _isConnected;

        #endregion

        #region Open Methods

        /// <summary>
        /// Begin listening to the receipt
        /// </summary>
        public void StartListening()
        {
            // If not connected
            if (!_isConnected)
            {
                _lastError = _errorList.GetErrorByCode(100020);
                throw _lastError;
            }

            // If there is no energy, then we turn on
            if (!_isPowerUp) { PowerUp(); }

            _isListening = true;
            _listener.Start();
        }

        /// <summary>
        /// Stop listeners bill acceptor
        /// </summary>
        public void StopListening()
        {
            _isListening = false;
            _listener.Stop();
            Disable();
        }

        /// <summary>
        /// Opening a COM port for working with a bill acceptor
        /// handleBillStackingTimeout - how long to wait for making the decisiion to accept the bill or not until rejecting the bill, decision made in BillStacking delegate handlers
        /// </summary>
        /// <returns></returns>
        public CashCodeBillValidatorException Connect(string portName, IBillsDefinition billsDefinition, int handleBillStackingTimeout = 10000)
        {
            try
            {
                if (IsConnected)
                {
                    Dispose(true);
                }

                _handleBillStackingTimeout = handleBillStackingTimeout;
                _billsDefinition = billsDefinition;

                _errorList = new CashCodeErroList();

                _disposed = false;
                _isEnableBills = false;
                _comPortName = "";
                _Locker = new object();
                _isConnected = _isPowerUp = _isListening = _returnBill = false;

                // From the specification:
                //      Baud Rate:	9600 bps/19200 bps (no negotiation, hardware selectable)
                //      Start bit:	1
                //      Data bit:	8 (bit 0 = LSB, bit 0 sent first)
                //      Parity:		Parity none 
                //      Stop bit:	1

                _comPort = new SerialPort
                {
                    PortName = _comPortName = portName,
                    BaudRate = _baudRate,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One
                };
                _comPort.DataReceived += _ComPort_DataReceived;

                _receivedBytes = new List<byte>();
                _synchCom = new EventWaitHandle(false, EventResetMode.AutoReset);

                _listener = new Timer
                {
                    Interval = POLL_TIMEOUT,
                    Enabled = false
                };
                _listener.Elapsed += _Listener_Elapsed;

                _comPort.Open();
                _isConnected = true;
            }
            catch
            {
                _isConnected = false;
                _lastError = _lastError = _errorList.GetErrorByCode(100010);
            }

            return _lastError;
        }

        // Enable billing
        public CashCodeBillValidatorException PowerUp()
        {
            // If the com port is not open
            if (!_isConnected)
            {
                _lastError = _errorList.GetErrorByCode(100020);
                throw _lastError;
            }

            // POWER UP
            var byteResult = SendCommand(BillValidatorCommands.POLL).ToList();

            // Let's check the result
            if (CheckPollOnError(byteResult.ToArray()))
            {
                SendCommand(BillValidatorCommands.NAK);
                throw _lastError;
            }

            // Otherwise, send a confirmation tone
            SendCommand(BillValidatorCommands.ACK);

            // RESET
            byteResult = SendCommand(BillValidatorCommands.RESET).ToList();

            // If you did not receive an ACK signal from the bill acceptor
            if (byteResult[3] != 0x00)
            {
                _lastError = _errorList.GetErrorByCode(100050);
                return _lastError;
            }

            // INITIALIZE
            // Then we again question the bill acceptor
            byteResult = SendCommand(BillValidatorCommands.POLL).ToList();

            if (CheckPollOnError(byteResult.ToArray()))
            {
                SendCommand(BillValidatorCommands.NAK);
                throw _lastError;
            }

            // Otherwise, send a confirmation tone
            SendCommand(BillValidatorCommands.ACK);

            // GET STATUS
            byteResult = SendCommand(BillValidatorCommands.GET_STATUS).ToList();

            // The GET STATUS command returns 6 bytes of the response. If all are equal to 0, then the status is ok and you can work further, otherwise the error
            if (byteResult[3] != 0x00 || byteResult[4] != 0x00 || byteResult[5] != 0x00 ||
                byteResult[6] != 0x00 || byteResult[7] != 0x00 || byteResult[8] != 0x00)
            {
                _lastError = _errorList.GetErrorByCode(100070);
                throw _lastError;
            }

            SendCommand(BillValidatorCommands.ACK);

            // SET_SECURITY (in the test case, it sends 3 bytes(0 0 0)
            byteResult = SendCommand(BillValidatorCommands.SET_SECURITY, new byte[] { 0x00, 0x00, 0x00 }).ToList();

            // If you did not receive an ACK signal from the bill acceptor
            if (byteResult[3] != 0x00)
            {
                _lastError = _errorList.GetErrorByCode(100050);
                return _lastError;
            }

            // IDENTIFICATION
            byteResult = SendCommand(BillValidatorCommands.IDENTIFICATION).ToList();
            SendCommand(BillValidatorCommands.ACK);


            // POLL
            // Then we again question the bill acceptor.Should receive the INITIALIZE command
            byteResult = SendCommand(BillValidatorCommands.POLL).ToList();

            // Let's check the result
            if (CheckPollOnError(byteResult.ToArray()))
            {
                SendCommand(BillValidatorCommands.NAK);
                throw _lastError;
            }

            // Otherwise, send a confirmation tone
            SendCommand(BillValidatorCommands.ACK);

            // POLL
            // Then we again question the bill acceptor.Should get the UNIT DISABLE command
            byteResult = SendCommand(BillValidatorCommands.POLL).ToList();

            // Let's check the result
            if (CheckPollOnError(byteResult.ToArray()))
            {
                SendCommand(BillValidatorCommands.NAK);
                throw _lastError;
            }

            // Otherwise, send a confirmation tone
            SendCommand(BillValidatorCommands.ACK);

            _isPowerUp = true;

            return _lastError;
        }

        // Enabling the mode of accepting bills
        public CashCodeBillValidatorException Enable()
        {
            // If the com port is not open
            if (!_isConnected)
            {
                _lastError = _errorList.GetErrorByCode(100020);
                throw _lastError;
            }

            try
            {
                if (!_isListening)
                {
                    throw new InvalidOperationException("Error in the method for enabling the receipt of notes.You must call the StartListening method.");
                }

                lock (_Locker)
                {
                    _isEnableBills = true;

                    //  remove the ENABLE BILL TYPES command(in the test case it sends 6 bytes(255 255 255 0 0 0) Hold function enabled(Escrow)
                    var byteResult = SendCommand(BillValidatorCommands.ENABLE_BILL_TYPES, ENABLE_BILL_TYPES_WITH_ESCROW).ToList();

                    // If you did not receive an ACK signal from the bill acceptor
                    if (byteResult[3] != 0x00)
                    {
                        _lastError = _errorList.GetErrorByCode(100050);
                        throw _lastError;
                    }

                    // Then we again question the bill acceptor
                    byteResult = SendCommand(BillValidatorCommands.POLL).ToList();

                    // Let's check the result
                    if (CheckPollOnError(byteResult.ToArray()))
                    {
                        SendCommand(BillValidatorCommands.NAK);
                        throw _lastError;
                    }

                    // Otherwise, send a confirmation tone
                    SendCommand(BillValidatorCommands.ACK);
                }
            }
            catch
            {
                _lastError = _errorList.GetErrorByCode(100030);
            }

            return _lastError;
        }

        // Turn off the reception mode of notes
        public CashCodeBillValidatorException Disable()
        {
            List<byte> byteResult;

            lock (_Locker)
            {
                // If the com port is not open
                if (!_isConnected)
                {
                    _lastError = _errorList.GetErrorByCode(100020);
                    throw _lastError;
                }

                _isEnableBills = false;

                // remove the ENABLE BILL TYPES command(in the test example, it sends 6 bytes(0 0 0 0 0 0)
                byteResult = SendCommand(BillValidatorCommands.ENABLE_BILL_TYPES, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }).ToList();
            }

            // If you did not receive an ACK signal from the bill acceptor
            if (byteResult[3] != 0x00)
            {
                _lastError = _errorList.GetErrorByCode(100050);
            }

            return _lastError;
        }

        #endregion

        #region Closed methods

        private bool CheckPollOnError(byte[] byteResult)
        {
            var isError = false;

            // If you did not receive from the bill acceptor a third byte equal to 30N(ILLEGAL COMMAND)
            if (byteResult[3] == 0x30)
            {
                _lastError = _errorList.GetErrorByCode(100040);
                isError = true;
            }

            // If not received from the bill acceptor the third byte equal to 41N (Drop Cassette Full)
            else if (byteResult[3] == 0x41)
            {
                _lastError = _errorList.GetErrorByCode(100080);
                isError = true;
            }

            // If not received from the bill acceptor the third byte equal to 42N (Drop Cassette out of position)
            else if (byteResult[3] == 0x42)
            {
                _lastError = _errorList.GetErrorByCode(100070);
                isError = true;
            }

            // If not received from the bill acceptor the third byte equal to 43H (Validator Jammed)
            else if (byteResult[3] == 0x43)
            {
                _lastError = _errorList.GetErrorByCode(100090);
                isError = true;
            }

            // If you did not receive a third byte equal to 44N from the bill acceptor (Drop Cassette Jammed)
            else if (byteResult[3] == 0x44)
            {
                _lastError = _errorList.GetErrorByCode(100100);
                isError = true;
            }

            // If not received from the bill acceptor the third byte equal to 45N (Cheated)
            else if (byteResult[3] == 0x45)
            {
                _lastError = _errorList.GetErrorByCode(100110);
                isError = true;
            }

            // If you did not get a third byte equal to 46N from the bill acceptor (Pause)
            else if (byteResult[3] == 0x46)
            {
                _lastError = _errorList.GetErrorByCode(100120);
                isError = true;
            }

            // If you did not get a third byte equal to 47N (Generic Failure codes) from the bill acceptor,
            else if (byteResult[3] == 0x47)
            {
                if (byteResult[4] == 0x50) { _lastError = _errorList.GetErrorByCode(100130); }        // Stack Motor Failure
                else if (byteResult[4] == 0x51) { _lastError = _errorList.GetErrorByCode(100140); }   // Transport Motor Speed Failure
                else if (byteResult[4] == 0x52) { _lastError = _errorList.GetErrorByCode(100150); }   // Transport Motor Failure
                else if (byteResult[4] == 0x53) { _lastError = _errorList.GetErrorByCode(100160); }   // Aligning Motor Failure
                else if (byteResult[4] == 0x54) { _lastError = _errorList.GetErrorByCode(100170); }   // Initial Cassette Status Failure
                else if (byteResult[4] == 0x55) { _lastError = _errorList.GetErrorByCode(100180); }   // Optic Canal Failure
                else if (byteResult[4] == 0x56) { _lastError = _errorList.GetErrorByCode(100190); }   // Magnetic Canal Failure
                else if (byteResult[4] == 0x5F) { _lastError = _errorList.GetErrorByCode(100200); }   // Capacitance Canal Failure
                isError = true;
            }

            return isError;
        }

        // Sending a command to the bill acceptor
        private byte[] SendCommand(BillValidatorCommands cmd, byte[] data = null)
        {
            if (cmd == BillValidatorCommands.ACK || cmd == BillValidatorCommands.NAK)
            {
                byte[] bytes = null;

                switch (cmd)
                {
                    case BillValidatorCommands.ACK:
                        bytes = Package.CreateResponse(ResponseType.Ack);
                        break;
                    case BillValidatorCommands.NAK:
                        bytes = Package.CreateResponse(ResponseType.Nak);
                        break;
                }

                if (bytes != null) { _comPort.Write(bytes, 0, bytes.Length); }

                return null;
            }
            var package = new Package { Command = (byte)cmd };

            if (data != null)
            {
                package.Data = data;
            }

            var cmdBytes = package.GetBytes();
            _comPort.Write(cmdBytes, 0, cmdBytes.Length);

            // Let's wait until we get data from the com port
            _synchCom.WaitOne(_handleBillStackingTimeout);
            _synchCom.Reset();

            var byteResult = _receivedBytes.ToArray();

            // If the CRC is ok, then check the fourth bit with the result
            // Must already get the data from the com-port, so we'll check the CRC
            if (byteResult.Length == 0 || !Package.CheckCrc(byteResult))
            {
                throw new ArgumentException("The mismatch of the checksum of the received message.The device may not be connected to the COM port.Check the connection settings.");
            }

            return byteResult;
        }

        // Currency code table
        private Bill CashCodeTable(byte code)
        {
            return _billsDefinition.Bills.First(x => x.BillAcceptorCode == code);
        }

        #endregion

        #region Development

        /// <summary>
        /// Event of receiving a bill
        /// </summary>
        public event BillReceivedHandler BillReceived;
        private void OnBillReceived(BillReceivedEventArgs e)
        {
            BillReceived?.Invoke(this, new BillReceivedEventArgs(e.Status, e.Bill, e.Exception));
        }

        public event BillCassetteHandler BillCassetteStatusEvent;
        private void OnBillCassetteStatus(BillCassetteEventArgs e)
        {
            BillCassetteStatusEvent?.Invoke(this, new BillCassetteEventArgs(e.Status));
        }


        /// <summary>
        /// Event of process of sending a note on the stack(Here it is possible to do a refund)
        /// </summary>
        public event BillStackingHandler BillStacking;
        private void OnBillStacking(BillStackedEventArgs e)
        {
            if (BillStacking == null)
                return;

            foreach (BillStackingHandler subscriber in BillStacking.GetInvocationList())
            {
                subscriber(this, e).GetAwaiter().GetResult();

                if (e.HasToRejectBill)
                {
                    _returnBill = true;
                    return;
                }
            }

            _returnBill = false;
        }

        #endregion

        #region Event Handlers

        // Receiving data from the com port
        private void _ComPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // Let's go into 100 ms, in order to give the program all the data from the com port
            Thread.Sleep(100);
            _receivedBytes.Clear();

            // We read bytes
            while (_comPort.BytesToRead > 0)
            {
                _receivedBytes.Add((byte)_comPort.ReadByte());
            }

            // Unlocking
            _synchCom.Set();
        }

        // The timer of wiretapping of the bill acceptor
        private void _Listener_Elapsed(object sender, ElapsedEventArgs e)
        {
            _listener.Stop();

            try
            {
                lock (_Locker)
                {
                    // Omit the POLL command
                    var byteResult = SendCommand(BillValidatorCommands.POLL).ToList();

                    // If the fourth bit is not Idling (unoccupied), then go further
                    if (byteResult[3] != 0x14)
                    {
                        // ACCEPTING
                        // If you receive a response 15H (Accepting)
                        if (byteResult[3] == 0x15)
                        {
                            // Confirm
                            SendCommand(BillValidatorCommands.ACK);
                        }

                        // ESCROW POSITION  
                        // If the fourth bit is 1Ch(Rejecting), then the bill acceptor did not recognize the bill
                        else if (byteResult[3] == 0x1C)
                        {
                            // Accepted some bill
                            SendCommand(BillValidatorCommands.ACK);

                            OnBillReceived(new BillReceivedEventArgs(BillRecievedStatus.Rejected, _billsDefinition.InvalidBill,
                            _errorList.GetErrorByCode(byteResult[4])));
                        }

                        // ESCROW POSITION
                        // Bill recognized
                        else if (byteResult[3] == 0x80)
                        {
                            // Acknowledging
                            SendCommand(BillValidatorCommands.ACK);

                            // The event that the bill is in the process of sending to the stack
                            OnBillStacking(new BillStackedEventArgs(CashCodeTable(byteResult[4])));

                            // If the program responds with a refund, then the return
                            if (_returnBill)
                            {
                                // RETURN
                                // If the program refuses to accept the bill, we will send RETURN
                                byteResult = SendCommand(BillValidatorCommands.RETURN).ToList();
                                _returnBill = false;
                            }
                            else
                            {
                                // STACK
                                // If you are equal, we will send the note to the stack(STACK)
                                byteResult = SendCommand(BillValidatorCommands.STACK).ToList();
                            }
                        }

                        // STACKING
                        // If the fourth bit is 17h, then the process of sending a note to the stack(STACKING)
                        else if (byteResult[3] == 0x17)
                        {
                            SendCommand(BillValidatorCommands.ACK);
                        }

                        // Bill stacked
                        // If the fourth bit is 81h, therefore, the bill hit the stack
                        else if (byteResult[3] == 0x81)
                        {
                            // Acknowledging
                            SendCommand(BillValidatorCommands.ACK);

                            OnBillReceived(new BillReceivedEventArgs(BillRecievedStatus.Accepted,
                                CashCodeTable(byteResult[4]), null));
                        }

                        // RETURNING
                        // If the fourth bit is 18h, then the process returns
                        else if (byteResult[3] == 0x18)
                        {
                            SendCommand(BillValidatorCommands.ACK);
                        }

                        // BILL RETURNING
                        // If the fourth bit is 82h, then the note is returned
                        else if (byteResult[3] == 0x82)
                        {
                            SendCommand(BillValidatorCommands.ACK);
                        }

                        // Drop Cassette out of position
                        // The bills are removed
                        else if (byteResult[3] == 0x42)
                        {
                            if (_cassettestatus != BillCassetteStatus.Removed)
                            {
                                // fire event
                                _cassettestatus = BillCassetteStatus.Removed;
                                OnBillCassetteStatus(new BillCassetteEventArgs(_cassettestatus));
                            }
                        }

                        // Initialize
                        // The cassette is inserted back into place
                        else if (byteResult[3] == 0x13)
                        {
                            if (_cassettestatus == BillCassetteStatus.Removed)
                            {
                                // fire event
                                _cassettestatus = BillCassetteStatus.Inplace;
                                OnBillCassetteStatus(new BillCassetteEventArgs(_cassettestatus));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // ignored
            }
            finally
            {
                // If the timer is off, then we start
                if (!_listener.Enabled && _isListening)
                    _listener.Start();
            }

        }

        #endregion
    }
}