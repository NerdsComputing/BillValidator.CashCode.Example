namespace BillValidator.CashCode.Driver.Internal
{
    internal interface IPackage
    {
        byte Command { get; set; }
        byte[] Data { get; set; }

        byte[] GetBytes();
        string GetBytesHex();
        int GetLength();
    }
}