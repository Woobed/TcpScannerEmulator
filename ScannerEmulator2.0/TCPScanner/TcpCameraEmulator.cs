using ScannerEmulator2._0.Abstractions;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ScannerEmulator2._0.TCPScanner
{
    public class TcpCameraEmulator: ITcpCameraEmulator
    {
        private readonly IPAddress _ipAddress;
        private readonly int _port;
        private readonly CancellationTokenSource _cts = new();
        private TcpListener? _listener;
        private bool _isStreaming;
        private string? _filePath;
        public string Name { get; set; }
        public bool IsRunning { get; private set; }

        public TcpCameraEmulator(string ip, int port)
        {
            Name = $"{ip}_{port}";
            _ipAddress = IPAddress.Parse(ip);
            _port = port;
        }

        public void SetFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Файл не найден", path);

            _filePath = path;
            Console.WriteLine($"{Name}: файл назначен → {path}");
        }

        // Запуск 
        public async Task StartAsync()
        {
            if (IsRunning) return;
            IsRunning = true;

            _listener = new TcpListener(_ipAddress, _port);
            _listener.Start();

            Console.WriteLine($"{Name} запущена на {_ipAddress}:{_port}");

            _ = AcceptClientsAsync(_cts.Token);
        }

        // Ожидание клиентов
        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var client = await _listener!.AcceptTcpClientAsync(token);
                Console.WriteLine($"{Name}: клиент подключен");
            }
        }

        // Обработка клиента 
        public async Task HandleClientAsync(TcpClient client, CancellationToken token, int delay)
        {
            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!_isStreaming || string.IsNullOrEmpty(_filePath))
                    {
                        await Task.Delay(500, token);
                        continue;
                    }

                    using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fileStream, Encoding.UTF8);

                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null && !token.IsCancellationRequested)
                    {
                        if (!_isStreaming)
                        {
                            // Если поставили паузу — ждём, пока возобновят
                            await WaitWhilePausedAsync(token);
                        }

                        await writer.WriteLineAsync(line);
                        Console.WriteLine($"{Name} → {line}");
                        await Task.Delay(delay, token);
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
        public void StartStreaming(int delay)
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                Console.WriteLine($"{Name}: нет назначенного файла!");
                return;
            }

            _isStreaming = true;
            Console.WriteLine($"{Name}: трансляция запущена ({delay} мс)");
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
