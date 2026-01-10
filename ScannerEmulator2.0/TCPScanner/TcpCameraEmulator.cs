using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Dto;
using ScannerEmulator2._0.Reactive;
using ScannerEmulator2._0.Services;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace ScannerEmulator2._0.TCPScanner
{
    public class TcpCameraEmulator : IModel
    {
        public ReactiveProperty<string> Name { get; set; } = new(string.Empty);
        public ReactiveProperty<int> Port { get; set; } = new(0);
        public ReactiveProperty<string> Ip { get; set; } = new(string.Empty);
        public ReactiveProperty<bool> IsConnected { get; set; } = new(false);
        public bool IsRunning { get; set; } = false;
        public bool IsReady { get; private set; } = true;

        public Action? InfoChanged { get; set; }

        private TcpListener? _listener;
        private TcpClient? _client;
        private readonly object _clientLock = new();

        private CancellationTokenSource? _cts;
        private LoggerService _logger;

        private readonly Channel<OutgoingPacket> _channel =
            Channel.CreateUnbounded<OutgoingPacket>();

        public TcpCameraEmulator(string ip, int port, LoggerService logger)
        {
            Ip.Value = ip;
            Port.Value = port;
            Name.Value = $"{ip}:{port}";
            _logger = logger;
        }

        public async Task StartAsync()
        {
            if (IsRunning)
                return;

            _cts = new CancellationTokenSource();

            _listener = new TcpListener(IPAddress.Parse(Ip.Value), Port.Value);
            _listener.Start();

            IsRunning = true;

            _ = UpdateConnectionState(_cts.Token);
            _ = AcceptClientLoop(_cts.Token);
            _ = WriterLoop(_cts.Token);

            await Task.CompletedTask;
        }

        public void Stop()
        {
            _cts?.Cancel();

            lock (_clientLock)
            {
                _client?.Close();
                _client = null;
            }

            _listener?.Stop();
            IsConnected.Value = false;
            IsRunning = false;
        }

        public Guid EnqueueTask(CameraSendTask task)
        {
            _ = task.RunAsync(_channel.Writer, _cts!.Token);

            return task.Id.Value;
        }

        private async Task AcceptClientLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync(token);

                    lock (_clientLock)
                    {
                        _client?.Close();
                        _client = client;
                    }
                }
                catch
                {
                    if (token.IsCancellationRequested)
                        return;
                }
            }
        }

        private async Task WriterLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                TcpClient? client;

                lock (_clientLock)
                    client = _client;

                if (client == null || !client.Connected)
                {
                    await Task.Delay(100, token);
                    continue;
                }


                try
                {
                    using var stream = client.GetStream();
                    using var writer = new StreamWriter(stream, new UTF8Encoding(false))
                    {
                        AutoFlush = true
                    };

                    await foreach (var packet in _channel.Reader.ReadAllAsync(token))
                    {
                        await writer.WriteAsync(packet.Payload);
                        var log = packet.log;
                        log.CameraName = Name.Value ?? string.Empty;
                        _logger.Log(log);
                    }
                }
                catch
                {
                    lock (_clientLock)
                    {
                        _client?.Close();
                        _client = null;
                    }
                }
            }
        }

        private async Task UpdateConnectionState(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                bool connected = false;

                lock (_clientLock)
                {
                    if (_client != null)
                    {
                        connected = IsSocketConnected(_client.Client);
                    }
                }

                if (IsConnected.Value != connected)
                {
                    IsConnected.Value = connected;
                }

                InfoChanged?.Invoke();
                await Task.Delay(1000, token);
            }
        }
        private bool IsSocketConnected(Socket socket)
        {
            if (socket == null) return false;

            try
            {
                if (socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0)
                    return false;
                else
                    return socket.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}
