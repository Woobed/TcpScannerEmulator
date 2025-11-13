using System.Net.Sockets;

namespace ScannerEmulator2._0.Abstractions
{
    public interface ITcpCameraEmulator: IModel
    {
        string Name { get; set; }
        void SetFile(string path);
        Task StartAsync();
        Task HandleClientAsync(int delay);
        bool StartStreaming(int delay);

        void PauseStreaming();

        void ResumeStreaming();

        void StopStreaming();
        void Stop();
        bool IsReady { get; }

        Action InfoChanged { get; set; }

    }
}
