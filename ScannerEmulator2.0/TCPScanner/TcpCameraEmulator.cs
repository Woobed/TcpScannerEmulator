using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Dto;
using System;
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

        private readonly CancellationTokenSource _cts = new();
        private TcpListener? _listener;

        private TcpClient? _client;
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

        public TcpCameraEmulator(string ip, int port)
        {
            Name = $"{ip}:{port}";
            Ip = ip;
            Port = port;
        }


        // ----------- FILE ASSIGNMENT ------------------

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


        // ----------- SERVER START ---------------------

        public async Task StartAsync()
        {
            if (IsRunning) return;

            _listener = new TcpListener(IPAddress.Parse(Ip), Port);
            _listener.Start();

            IsRunning = true;

            Console.WriteLine($"{Name} запущена на {Ip}:{Port}");

            _ = AcceptClientsAsync(_cts.Token);
        }


        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var newClient = await _listener!.AcceptTcpClientAsync(token);
                    Console.WriteLine($"{Name}: клиент подключен");

                    _client = newClient;

                    // запуск обработчика только при подключении клиента
                    if (_clientTask == null || _clientTask.IsCompleted)
                    {
                        _clientTask = HandleClientAsync();
                    }
                }
                catch
                {
                    if (token.IsCancellationRequested) return;
                }
            }
        }


        // ----------- CLIENT HANDLING ------------------

        private async Task HandleClientAsync()
        {
            if (_client == null) return;

            try
            {
                using var stream = _client.GetStream();
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

                using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream);

                _totalLines = CountLines(_filePath);

                while (!_cts.Token.IsCancellationRequested)
                {
                    // ждём, пока включат стриминг
                    while (!_isStreaming && !_cts.Token.IsCancellationRequested)
                        await Task.Delay(100, _cts.Token);

                    if (_cts.Token.IsCancellationRequested)
                        return;

                    var settings = _lastSettings;
                    if (settings == null)
                        continue;

                    _finalLines = settings.GroupCount == 1
                        ? _totalLines
                        : (int)Math.Ceiling((double)_totalLines / settings.GroupCount);

                    string? line;

                    while (_isStreaming && !_cts.Token.IsCancellationRequested)
                    {
                        // одиночные строки
                        if (settings.GroupCount == 1)
                        {
                            string? outputLine = await reader.ReadLineAsync();

                            if (outputLine == null)
                            {
                                // конец файла
                                Console.WriteLine($"{Name}: достигнут конец файла — перезапуск");
                                fileStream.Seek(0, SeekOrigin.Begin);
                                reader.DiscardBufferedData();
                                _sentLines = 0;
                                break;
                            }

                            outputLine += settings.DataSeparator;

                            await writer.WriteAsync(outputLine);

                            _sentLines++;
                            SendNotification?.Invoke(_sentLines, _finalLines);

                            await Task.Delay(settings.Delay);
                        }
                        else // группировка
                        {
                            var group = new List<string>();

                            for (int i = 0; i < settings.GroupCount; i++)
                            {
                                line = await reader.ReadLineAsync();
                                if (line == null) break;
                                group.Add(line);
                            }

                            if (group.Count == 0)
                            {
                                Console.WriteLine($"{Name}: достигнут конец файла — перезапуск");
                                fileStream.Seek(0, SeekOrigin.Begin);
                                reader.DiscardBufferedData();
                                _sentLines = 0;
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

                            await Task.Delay(settings.Delay);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Name}: ошибка клиента: {ex.Message}");
            }
        }


        // ----------- CONTROL COMMANDS ------------------

        public async Task<bool> StartStreaming(TaskSettings settings)
        {
            if (string.IsNullOrWhiteSpace(_filePath))
            {
                Console.WriteLine($"{Name}: нет назначенного файла");
                return false;
            }

            _lastSettings = settings;
            _isStreaming = true;

            Console.WriteLine($"{Name}: трансляция включена");
            return true;
        }

        public void PauseStreaming()
        {
            _isStreaming = false;
            Console.WriteLine($"{Name}: пауза");
        }

        public void ResumeStreaming()
        {
            _isStreaming = true;
            Console.WriteLine($"{Name}: продолжение");
        }

        public void StopStreaming()
        {
            _isStreaming = false;
            _sentLines = 0;
            SendNotification?.Invoke(0, _finalLines);
            Console.WriteLine($"{Name}: трансляция остановлена");
        }

        public void Stop()
        {
            _cts.Cancel();
            _listener?.Stop();
            _client?.Close();

            IsRunning = false;
            _isStreaming = false;

            Console.WriteLine($"{Name}: сервер остановлен");
        }

        public void DropTask()
        {
            IsReady = false;
            _filePath = string.Empty;
        }


        // ----------- UTILS ------------------

        public static int CountLines(string filePath)
        {
            int count = 0;

            using var reader = new StreamReader(filePath);
            while (reader.ReadLine() != null)
                count++;

            return count;
        }
    }
}
