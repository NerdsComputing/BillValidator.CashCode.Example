using System;
using System.Collections.Generic;
using System.Text;

namespace BillValidator.CashCode.Driver.Internal
{
    public enum ResponseType { Ack, Nak }

    internal sealed class Package : IPackage
    {
        #region Private fields

        private const int Polynomial = 0x08408;         // Required for CRC calculation
        private const byte Sync = 0x02;              // Synchronization bit (fixed)
        private const byte Adr = 0x03;    // Peripheral address of the frame. For the bill of exchange from the documentation is equal to 0x03

        private byte _command;
        private byte[] _data;

        #endregion

        #region Constructor

        public Package()
        {}

        public Package(byte command, byte[] data)
        {
            _command = command;
            Data = data;
        }

        #endregion

        #region Properties

        public byte Command
        {
            get => _command;
            set => _command = value;
        }

        public byte[] Data
        {
            get => _data;
            set 
            {
                if (value.Length + 5 > 250)
                    return;

                _data = new byte[value.Length];
                _data = value;
            }
        }

        #endregion

        #region Methods

        // Returns an array of bytes of the package
        public byte[] GetBytes()
        {
            // Buffer package (without 2 bytes CRC). The first four bytes are SYNC, ADR, LNG, CMD
            var buff = new List<byte>
            {   
                Sync, // Byte 1: Synchronization flag
                Adr // Byte 2: device address
            };

            // Byte 3: Packet length
            // Calculate the length of the packet
            var result = GetLength();

            // If the packet length along with SYNC, ADR, LNG, CRC, CMD bytes is greater than 250
            // then we make a byte of length equal to 0, and the actual length of the message will be in DATA
            buff.Add(result > 250 ? (byte)0 : Convert.ToByte(result));

            // Byte 4: Command
            buff.Add(_command);

            if (_data != null)
            {
                buff.AddRange(_data);
            }

            // The last byte is CRC
            byte[] CRC = BitConverter.GetBytes(GetCrc16(buff.ToArray(), buff.Count));

            byte[] package = new byte[buff.Count + CRC.Length];
            buff.ToArray().CopyTo(package, 0);
            CRC.CopyTo(package, buff.Count);

            return package;
        }

        // Returns the string of the hexadecimal representation of the bytes of the packet
        public string GetBytesHex()
        {
            byte[] package = GetBytes();

            var hexString = new StringBuilder(package.Length);
            foreach (byte item in package)
            {
                hexString.Append(item.ToString("X2"));
            }

            return $"0x{hexString}";
        }

        // Packet length
        public int GetLength()
        {
            return (_data?.Length ?? 0) + 6;
        }

        // Calculation of the checksum
        private static int GetCrc16(byte[] bufData, int sizeData)
        {
            var crc = 0;

            for (var i = 0; i < sizeData; i++)
            {
                var tmpCrc = crc ^ bufData[i];

                for (byte j = 0; j < 8; j++)
                {
                    if ((tmpCrc & 0x0001) != 0) { tmpCrc >>= 1; tmpCrc ^= Polynomial; }
                    else { tmpCrc >>= 1; }
                }

                crc = tmpCrc;
            }

            return crc;
        }

        public static bool CheckCrc(byte[] buff)
        {
            var result = true;
            var oldCrc = new[] { buff[buff.Length - 2], buff[buff.Length - 1]};

            // The last two bytes in the length are removed, since this is the original CRC
            var newCrc = BitConverter.GetBytes(GetCrc16(buff, buff.Length - 2));

            for (var i = 0; i < 2; i++)
            {
                if (oldCrc[i] != newCrc[i])
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        public static byte[] CreateResponse(ResponseType type)
        {
            // Buffer package(without 2 bytes CRC). The first four bytes are SYNC, ADR, LNG, CMD
            var buff = new List<byte>
            {               
                Sync, // Byte 1: Synchronization flag  
                Adr, // Byte 2: device address
                0x06 // Byte 3: packet length, always 6
            };

            // Byte 4: Data
            switch (type)
            {
                case ResponseType.Ack:
                    buff.Add(0x00);
                    break;
                case ResponseType.Nak:
                    buff.Add(0xFF);
                    break;
            }

            // The last byte is CRC
            var crc = BitConverter.GetBytes(GetCrc16(buff.ToArray(), buff.Count));

            var package = new byte[buff.Count + crc.Length];
            buff.ToArray().CopyTo(package, 0);
            crc.CopyTo(package, buff.Count);

            return package;
        }

        #endregion
    }
}
