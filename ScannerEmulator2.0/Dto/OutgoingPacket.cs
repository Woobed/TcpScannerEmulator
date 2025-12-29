namespace ScannerEmulator2._0.Dto
{
    public readonly struct OutgoingPacket
    {
        public string Payload { get; }

        public OutgoingPacket(string payload)
        {
            Payload = payload;
        }
    }
}
