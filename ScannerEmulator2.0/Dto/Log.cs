namespace ScannerEmulator2._0.Dto
{
    public class Log
    {
        public string CameraName { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }

        public Log(string message, string filename)
        {
            Message = message;
            FileName = filename;
        }
    }
}
