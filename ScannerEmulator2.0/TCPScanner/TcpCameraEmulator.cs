using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScannerEmulator2._0.TCPScanner
{
    public class TcpCameraEmulator : ITcpCameraEmulator
    {
        public int Port { get; set; }
        public string Ip { get; set; }

        private readonly CancellationTokenSource _cts = new();
        private TcpListener? _listener;

        private TcpClient? _client;
        private readonly object _clientLock = new();
        private Task? _clientTask;

        private bool _isStreaming = false;
        private string _filePath = string.Empty;

        public string Name { get; set; }
        public bool IsRunning { get; private set; }
        public bool IsReady { get; private set; } = false;

        public string FileName { get; set; } = string.Empty;

        private TaskSettings? _lastSettings;

        public Action InfoChanged { get; set; }
        public Action<int, int> SendNotification { get; set; }

        private int _sentLines = 0;
        private int _finalLines = 0;
        private int _totalLines = 0;

        private FileStream? _fileStream;
        private StreamReader? _reader;

        public TcpCameraEmulator(string ip, int port)
        {
            Ip = ip;
            Port = port;
            Name = $"{ip}:{port}";
        }

        // ---------------- FILE SET --------------------

        public void SetFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл не найден", path);

            _filePath = path;
            FileName = Path.GetFileName(path);
            IsReady = true;

            Console.WriteLine($"{Name}: файл назначен: {path}");
            InfoChanged?.Invoke();
        }

        // ---------------- SERVER START --------------------

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
                    {
                        _client = client;
                    }

                    if (_clientTask == null || _clientTask.IsCompleted)
                        _clientTask = Task.Run(() => HandleClientAsync(_cts.Token));
                }
                catch
                {
                    if (token.IsCancellationRequested) return;
                }
            }
        }

        // ---------------- MAIN STREAM LOOP --------------------

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

                await PrepareFileAsync(); // всегда открываем файл заново при запуске обработчика

                while (!token.IsCancellationRequested)
                {
                    while (!_isStreaming && !token.IsCancellationRequested)
                    {
                        await Task.Delay(100, token);
                    }

                    if (token.IsCancellationRequested)
                        break;

                    var settings = _lastSettings;
                    if (settings == null)
                    {
                        _isStreaming = false;
                        continue;
                    }

                    _finalLines = settings.GroupCount == 1
                        ? _totalLines
                        : (int)Math.Ceiling((double)_totalLines / settings.GroupCount);

                    while (_isStreaming && !token.IsCancellationRequested)
                    {
                        if (settings.GroupCount == 1)
                        {
                            var line = await _reader!.ReadLineAsync();

                            if (line == null)
                            {
                                await HandleEOFAsync();
                                break;
                            }

                            await writer.WriteAsync(line + settings.DataSeparator);

                            _sentLines++;
                            SendNotification?.Invoke(_sentLines, _finalLines);

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
                                break;
                            }

                            string payload =
                                settings.DataHeader +
                                string.Join(settings.DataSeparator, group) +
                                settings.DataSeparator +
                                settings.DataTerminator;

                            await writer.WriteAsync(payload);

                            _sentLines++;
                            SendNotification?.Invoke(_sentLines, _finalLines);

                            await Task.Delay(settings.Delay, token);
                        }
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

        // ------------ EOF HANDLING (A1 — reset to begin) ----------------

        private async Task HandleEOFAsync()
        {
            Console.WriteLine($"{Name}: достигнут конец файла → остановка и перемотка");

            _isStreaming = false;

            await PrepareFileAsync(); // <-- ПЕРЕМАТЫВАЕМ НА НАЧАЛО

            _sentLines = 0;
            SendNotification?.Invoke(_sentLines, _finalLines);
        }

        // ---------------- FILE PREP --------------------

        private async Task PrepareFileAsync()
        {
            _reader?.Dispose();
            _fileStream?.Dispose();

            _fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _reader = new StreamReader(_fileStream, Encoding.UTF8);

            _totalLines = CountLines(_filePath);

            await Task.CompletedTask;
        }

        // ---------------- CONTROL COMMANDS --------------------

        public async Task<bool> StartStreaming(TaskSettings settings)
        {
            if (!IsReady)
                return false;

            _sentLines = 0;
            _lastSettings = settings;
            _isStreaming = true;

            // Если обработчик завершился — создаём новый
            bool hasClient;
            lock (_clientLock)
                hasClient = _client != null;

            if (hasClient && (_clientTask == null || _clientTask.IsCompleted))
                _clientTask = Task.Run(() => HandleClientAsync(_cts.Token));

            return await Task.FromResult(true);
        }

        public void PauseStreaming() => _isStreaming = false;

        public void ResumeStreaming()
        {
            _isStreaming = true;

            lock (_clientLock)
            {
                if (_client != null && (_clientTask == null || _clientTask.IsCompleted))
                    _clientTask = Task.Run(() => HandleClientAsync(_cts.Token));
            }
        }

        public void StopStreaming()
        {
            _isStreaming = false;
            _sentLines = 0;
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

        // ---------------- UTILS --------------------

        public static int CountLines(string filePath)
        {
            int count = 0;
            using var reader = new StreamReader(filePath);
            while (reader.ReadLine() != null) count++;
            return count;
        }
    }
}
