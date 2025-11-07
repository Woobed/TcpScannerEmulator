using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ScannerEmulator2._0.Abstractions
{
    public interface ITcpCameraEmulator
    {
        string Name { get; set; }
        void SetFile(string path);
        Task StartAsync();
        Task HandleClientAsync(TcpClient client, CancellationToken token, int delay);
        void StartStreaming(int delay);

        void PauseStreaming();

        void ResumeStreaming();

        void StopStreaming();
        void Stop();

    }
}
