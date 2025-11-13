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
        private readonly CancellationTokenSource _cts = new();
        private TcpListener? _listener;
        private bool _isStreaming;
        private string _filePath = String.Empty;
        public string Name { get; set; }
        public bool IsRunning { get; private set; }
        public bool IsReady { get; private set; } = false;

        public string FileName { get; set; } = String.Empty;

        public Action InfoChanged { get; set; }

        private TcpClient client;


        public TcpCameraEmulator(string ip, int port)
        {
            Name = $"{ip}:{port}";
            Ip = ip;
            Port = port;
        }

        public void SetFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл не найден", path);

            _filePath = path;
            FileName = Path.GetFileName(path);
            Console.WriteLine($"{Name}: файл назначен → {path}");
            IsReady = true;
            InfoChanged.Invoke();
        }

        // Запуск 
        public async Task StartAsync()
        {
            if (IsRunning) return;
            IsRunning = true;
            _listener = new TcpListener(IPAddress.Parse(Ip), Port);
            _listener.Start();

            Console.WriteLine($"{Name} запущена на {Ip}:{Port}");

            _ = AcceptClientsAsync(_cts.Token);
        }

        // Ожидание клиентов
        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                client = await _listener!.AcceptTcpClientAsync(token);
                Console.WriteLine($"{Name}: клиент подключен");
            }
        }

        // Обработка клиента 
        public async Task HandleClientAsync(TaskSettings settings)
        {
            if (client == null) return;
            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false))  /*{ AutoFlush = true }*/;

            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    if ((!_isStreaming) || string.IsNullOrEmpty(_filePath))
                    {
                        await Task.Delay(500, _cts.Token);
                        continue;
                    }

                    using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fileStream, Encoding.UTF8);

                    string? line;
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        if (!_isStreaming)
                        {
                            await WaitWhilePausedAsync(_cts.Token);
                        }

                        if (settings.GroupCount <= 0)
                        {
                            Console.WriteLine($"{Name} Указано некорректное количество объектов в группе");
                            return;
                        }

                        if (settings.GroupCount == 1)
                        {
                            string? outputLine = await reader.ReadLineAsync();
                            if (string.IsNullOrEmpty(outputLine))
                            {
                                Console.WriteLine($"{Name} Считана пустая строка");
                                continue;
                            }
                            await writer.WriteLineAsync(outputLine);
                            Console.WriteLine($"{Name} → {outputLine}");
                            await Task.Delay(settings.Delay, _cts.Token);
                        }
                        else if (settings.GroupCount > 1)
                        {
                            var lines = new List<string>();
                            for (int i = 0; i < settings.GroupCount && (line = await reader.ReadLineAsync()) != null; i++)
                            {
                                lines.Add(line);
                            }
                            if (lines.Count > 0)
                            {
                                string lineToSend = string.Join(settings.DataSeparator, lines) + settings.DataTerminator;

                                // Добавляем заголовок и терминатор если нужно
                                string outputLine = $"{settings.DataHeader}{lineToSend}{settings.DataTerminator}";

                                await writer.WriteLineAsync(outputLine);
                                Console.WriteLine($"{Name} → {outputLine}");
                                await Task.Delay(settings.Delay, _cts.Token);
                            }
                        }

                    }

                    Console.WriteLine($"{Name}: достигнут конец файла");
                    StopStreaming();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Name}: ошибка или отключение клиента ({ex.Message})");
            }
        }

        private async Task WaitWhilePausedAsync(CancellationToken token)
        {
            while (!_isStreaming && !token.IsCancellationRequested)
                await Task.Delay(100, token);
        }

        // Начало отправки
        public bool StartStreaming(TaskSettings settings)
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                Console.WriteLine($"{Name}: нет назначенного файла!");
                return false;
            }
            _ = HandleClientAsync(settings);
            _isStreaming = true;
            Console.WriteLine($"{Name}: трансляция запущена ({settings.Delay} мс)");
            return true;
        }

        // Пауза
        public void PauseStreaming()
        {
            _isStreaming = false;
            Console.WriteLine($"{Name}: трансляция поставлена на паузу");
        }

        // Возобновление после паузы
        public void ResumeStreaming()
        {
            _isStreaming = true;
            Console.WriteLine($"{Name}: трансляция возобновлена");
        }
        // Остановка после завершения файла
        public void StopStreaming()
        {
            _cts.Cancel();
            IsRunning = false;
            _isStreaming = false;
            Console.WriteLine($"{Name} остановлена ");
        }

        // Полная остановка
        public void Stop()
        {
            _cts.Cancel();
            _listener?.Stop();
            IsRunning = false;
            _isStreaming = false;
            Console.WriteLine($"{Name} остановлена и удалена");
        }
    }
}
