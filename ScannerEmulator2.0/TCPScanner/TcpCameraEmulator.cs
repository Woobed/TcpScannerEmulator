using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Dto;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ScannerEmulator2._0.TCPScanner
{
    public class TcpCameraEmulator : ITcpCameraEmulator
    {
        public int Port { get; set; }
        public string Ip { get; set; }
        public string Name { get; set; }
        public bool IsRunning { get; private set; }
        public bool IsReady { get; private set; } = false;
        public string FileName { get; set; } = string.Empty;

        private readonly CancellationTokenSource _cts = new();
        private TcpListener? _listener;
        private TcpClient? _client;
        private readonly object _clientLock = new();

        private bool _isStreaming = false;
        private string _filePath = string.Empty;

        private TaskSettings? _lastSettings;

        public Action InfoChanged { get; set; }
        public Action<int, int> SendNotification { get; set; }

        private int _sentLines = 0;
        private int _finalLines = 0;
        private int _totalLines = 0;

        private FileStream? _fileStream;
        private StreamReader? _reader;

        private int _clientHandlerRunning = 0;

        public TcpCameraEmulator(string ip, int port)
        {
            Ip = ip;
            Port = port;
            Name = $"{ip}:{port}";
        }
        public void SetFile(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Файл не найден", path);

            _filePath = path;
            FileName = Path.GetFileName(path);
            IsReady = true;

            Console.WriteLine($"{Name}: файл назначен: {path}");
            InfoChanged?.Invoke();
        }

        public async Task StartAsync()
        {
            if (IsRunning) return;

            _listener = new TcpListener(IPAddress.Parse(Ip), Port);
            _listener.Start();
            IsRunning = true;

            Console.WriteLine($"{Name}: сервер запущен");

            _ = AcceptClientsAsync(_cts.Token);
            await Task.CompletedTask;
        }

        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync(token);
                    Console.WriteLine($"{Name}: клиент подключён");
                    
                    lock (_clientLock)
                        _client = client;

                    TryStartClientHandler();
                }
                catch
                {
                    if (token.IsCancellationRequested) return;
                }
            }
        }

        private void TryStartClientHandler()
        {
            if (Interlocked.CompareExchange(ref _clientHandlerRunning, 1, 0) == 0)
            {
                _ = Task.Run(async () =>
                {
                    try { await HandleClientAsync(_cts.Token); }
                    finally { Interlocked.Exchange(ref _clientHandlerRunning, 0); }
                });
            }
        }

        private async Task HandleClientAsync(CancellationToken token)
        {
            TcpClient? localClient;
            lock (_clientLock)
                localClient = _client;

            if (localClient == null) return;

            try
            {
                using var stream = localClient.GetStream();
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

                await PrepareFileAsync();

                while (!token.IsCancellationRequested)
                {
                    if (!_isStreaming)
                    {
                        await Task.Delay(50, token);
                        continue;
                    }

                    var settings = _lastSettings;
                    if (settings == null)
                    {
                        _isStreaming = false;
                        continue;
                    }

                    _finalLines = settings.GroupCount == 1
                        ? _totalLines
                        : (int)Math.Ceiling((double)_totalLines / settings.GroupCount);


                    if (settings.GroupCount == 1)
                    {
                        var line = await _reader!.ReadLineAsync();
                        if (line == null)
                        {
                            await HandleEOFAsync();
                            continue;
                        }

                        await writer.WriteAsync(line + settings.DataSeparator);

                        int sent = Interlocked.Increment(ref _sentLines);
                        SendNotification?.Invoke(sent, _finalLines);

                        await Task.Delay(settings.Delay, token);
                    }
                    else
                    {
                        var group = new List<string>();
                        for (int i = 0; i < settings.GroupCount; i++)
                        {
                            var line = await _reader!.ReadLineAsync();
                            if (line == null) break;
                            group.Add(line);
                        }

                        if (group.Count == 0)
                        {
                            await HandleEOFAsync();
                            continue;
                        }

                        string payload = settings.DataHeader +
                                         string.Join(settings.DataSeparator, group) +
                                         settings.DataSeparator +
                                         settings.DataTerminator;

                        await writer.WriteAsync(payload);

                        int sent = Interlocked.Increment(ref _sentLines);
                        SendNotification?.Invoke(sent, _finalLines);

                        await Task.Delay(settings.Delay, token);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Name}: ошибка клиента: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"{Name}: обработчик завершён");
            }
        }

        private async Task HandleEOFAsync()
        {
            Console.WriteLine($"{Name}: достигнут конец файла → перемотка");

            _isStreaming = false;

            await PrepareFileAsync();

            Interlocked.Exchange(ref _sentLines, 0);
            SendNotification?.Invoke(0, _finalLines);
        }

        private async Task PrepareFileAsync()
        {
            _reader?.Dispose();
            _fileStream?.Dispose();

            _fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _reader = new StreamReader(_fileStream, Encoding.UTF8);

            _totalLines = CountLines(_filePath);
            await Task.CompletedTask;
        }

        public Task<bool> StartStreaming(TaskSettings settings)
        {
            if (!IsReady) return Task.FromResult(false);

            Interlocked.Exchange(ref _sentLines, 0);
            _lastSettings = settings;
            _isStreaming = true;

            return Task.FromResult(true);
        }

        public void PauseStreaming() => _isStreaming = false;

        public void ResumeStreaming() => _isStreaming = true;

        public void StopStreaming()
        {
            _isStreaming = false;
            Interlocked.Exchange(ref _sentLines, 0);
            SendNotification?.Invoke(0, _finalLines);
        }

        public void Stop()
        {
            _cts.Cancel();
            _listener?.Stop();

            lock (_clientLock)
            {
                _client?.Close();
                _client = null;
            }

            IsRunning = false;
        }

        public void DropTask()
        {
            IsReady = false;
            _filePath = string.Empty;
        }

        public static int CountLines(string filePath)
        {
            int count = 0;
            using var reader = new StreamReader(filePath);
            while (reader.ReadLine() != null) count++;
            return count;
        }
    }
}
