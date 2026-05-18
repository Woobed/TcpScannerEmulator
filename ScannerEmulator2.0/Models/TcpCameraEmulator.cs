using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Dto;
using ScannerEmulator2._0.Reactive;
using ScannerEmulator2._0.Services;
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

        private Action<int>? MessageSentEvent;
        public Action? InfoChanged { get; set; }

        private TcpListener? _listener;
        private TcpClient? _client;
        private readonly object _clientLock = new();

        private CancellationTokenSource? _cts;
        private readonly LoggerService _logger;

        private readonly Channel<OutgoingPacket> _channel =
            Channel.CreateBounded<OutgoingPacket>(
                new BoundedChannelOptions(10000)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false
                });

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
            if (_cts == null)
                return;

            // Сначала отменяем все фоновые задачи
            _cts.Cancel();

            // Закрываем канал, чтобы ReadAsync/WriteAsync завершились
            _channel.Writer.TryComplete();

            lock (_clientLock)
            {
                try
                {
                    _client?.Close();
                }
                catch
                {
                }

                _client = null;
            }

            try
            {
                _listener?.Stop();
            }
            catch
            {
            }

            IsConnected.Value = false;
            IsRunning = false;
        }

        public Guid EnqueueTask(CameraSendTask task)
        {
            MessageSentEvent += task.IncrementSentGroups;
            _ = RunTaskWhenConnected(task);
            return task.Id.Value;
        }

        private async Task RunTaskWhenConnected(CameraSendTask task)
        {
            if (_cts == null)
                return;

            var token = _cts.Token;

            try
            {
                // Ждём реального подключения сокета.
                // Важно: здесь НЕ используем IsConnected.Value,
                // так как оно обновляется раз в секунду.
                while (!token.IsCancellationRequested)
                {
                    bool connected;

                    lock (_clientLock)
                    {
                        connected =
                            _client != null &&
                            _client.Connected &&
                            IsSocketConnected(_client.Client);
                    }

                    if (connected)
                        break;

                    await Task.Delay(50, token);
                }

                if (token.IsCancellationRequested)
                    return;

                await task.RunAsync(_channel.Writer, token);
            }
            catch (OperationCanceledException)
            {
                // Нормальная ситуация при Stop()
            }
            catch
            {
                // При необходимости можно добавить логирование
            }
        }

        private async Task AcceptClientLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync(token);

                    // Минимизация задержек TCP
                    client.NoDelay = true;
                    client.SendBufferSize = 8192;

                    lock (_clientLock)
                    {
                        try
                        {
                            _client?.Close();
                        }
                        catch
                        {
                        }

                        _client = client;
                    }

                    // Обновляем состояние немедленно, не дожидаясь UpdateConnectionState
                    if (!IsConnected.Value)
                    {
                        IsConnected.Value = true;
                        InfoChanged?.Invoke();
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    if (token.IsCancellationRequested)
                        return;

                    await Task.Delay(100, token);
                }
            }
        }

        private async Task WriterLoop(CancellationToken token)
        {
            TcpClient? writerClient = null;
            NetworkStream? stream = null;
            DateTime nextSendTime = DateTime.UtcNow;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    TcpClient? client;

                    lock (_clientLock)
                    {
                        client = _client;
                    }

                    if (client == null)
                    {
                        if (stream != null)
                        {
                            stream.Dispose();
                            stream = null;
                            writerClient = null;
                        }

                        await Task.Delay(50, token);
                        continue;
                    }

                    if (!ReferenceEquals(writerClient, client))
                    {
                        stream?.Dispose();

                        stream = client.GetStream();
                        writerClient = client;

                        await Task.Delay(100, token);
                        nextSendTime = DateTime.UtcNow;
                    }

                    OutgoingPacket packet;
                    try
                    {
                        packet = await _channel.Reader.ReadAsync(token);
                    }
                    catch (ChannelClosedException)
                    {
                        break;
                    }
                    var now = DateTime.UtcNow;
                    if (nextSendTime > now)
                    {
                        await Task.Delay(nextSendTime - now, token);
                    }
                    token.ThrowIfCancellationRequested();
                    var bytes = Encoding.UTF8.GetBytes(packet.Payload);

                    try
                    {
                        await stream!.WriteAsync(bytes, 0, bytes.Length, token);
                        await stream.FlushAsync(token);
                    }
                    catch
                    {
                        if (!token.IsCancellationRequested)
                        {
                            try
                            {
                                await _channel.Writer.WriteAsync(packet, token);
                            }
                            catch
                            {
                            }
                        }
                        try
                        {
                            stream?.Dispose();
                        }
                        catch
                        {
                        }

                        stream = null;
                        writerClient = null;

                        lock (_clientLock)
                        {
                            try
                            {
                                _client?.Close();
                            }
                            catch
                            {
                            }

                            _client = null;
                        }

                        IsConnected.Value = false;
                        InfoChanged?.Invoke();

                        await Task.Delay(100, token);
                        continue;
                    }

                    MessageSentEvent?.Invoke(packet.Hash);

                    var log = packet.log;
                    log.CameraName = Name.Value ?? string.Empty;
                    _logger.Log(log);

                    var delay = Math.Max(packet.Delay, 1);
                    nextSendTime = DateTime.UtcNow.AddMilliseconds(delay);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                try
                {
                    stream?.Dispose();
                }
                catch
                {
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
                        connected =
                            _client.Connected &&
                            IsSocketConnected(_client.Client);
                    }
                }

                if (IsConnected.Value != connected)
                {
                    IsConnected.Value = connected;
                    InfoChanged?.Invoke();
                }

                await Task.Delay(1000, token);
            }
        }

        private bool IsSocketConnected(Socket socket)
        {
            try
            {
                if (socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0)
                    return false;

                return socket.Connected;
            }
            catch
            {
                return false;
            }
        }
    }
}