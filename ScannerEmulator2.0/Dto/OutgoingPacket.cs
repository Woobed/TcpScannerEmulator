namespace ScannerEmulator2._0.Dto
{
    public class OutgoingPacket
    {
        public string Payload { get; set; }
        public Log log { get; set; }
        public int Delay { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Hash { get; set; }
    }
}
