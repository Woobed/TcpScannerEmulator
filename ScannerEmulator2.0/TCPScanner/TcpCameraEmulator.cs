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
        private bool _isStreaming;
        private string _filePath = String.Empty;
        public string Name { get; set; }
        public bool IsRunning { get; private set; }
        public bool IsReady { get; private set; } = false;

        public string FileName { get; set; } = String.Empty;

        public Action InfoChanged { get; set; }

        public Action<int, int> SendNotification { get; set; }

        private TcpClient client;
        private Task _currentStreamingTask;

        public int _sentLines { get; set; }
        public int _finalLines { get; set; }
        private int _totalLines;
        private bool _isProcessing = false;

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
            InfoChanged?.Invoke();
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

            try
            {
                using var stream = client.GetStream();
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

                // ИНИЦИАЛИЗАЦИЯ ПЕРЕМЕННЫХ ВНЕ ЦИКЛА - ОДИН РАЗ!
                _totalLines = CountLines(_filePath);
                _finalLines = settings.GroupCount > 1
                    ? (int)Math.Ceiling((double)_totalLines / settings.GroupCount)
                    : _totalLines;

                // ОТКРЫВАЕМ ФАЙЛ ОДИН РАЗ - ВНЕ ЦИКЛА!
                using var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream, Encoding.UTF8);

                while (!_cts.Token.IsCancellationRequested)
                {
                    // ЖДЕМ ПОКА ВКЛЮЧЕН СТРИМИНГ
                    while (!_isStreaming && !_cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(100, _cts.Token);
                    }

                    if (_cts.Token.IsCancellationRequested) break;

                    string? line;
                    // ОСНОВНОЙ ЦИКЛ ОТПРАВКИ ДАННЫХ
                    while (_isStreaming && !_cts.Token.IsCancellationRequested)
                    {
                        if (settings.GroupCount <= 0)
                        {
                            Console.WriteLine($"{Name} Указано некорректное количество объектов в группе");
                            return;
                        }

                        if (settings.GroupCount == 1)
                        {
                            string? outputLine = await reader.ReadLineAsync();
                            if (outputLine == null)
                            {
                                // Достигнут конец файла - перезапускаем с начала
                                Console.WriteLine($"{Name}: достигнут конец файла, перезапуск");
                                fileStream.Seek(0, SeekOrigin.Begin); // ПЕРЕМОТКА НА НАЧАЛО
                                reader.DiscardBufferedData();
                                _sentLines = 0; // СБРАСЫВАЕМ ТОЛЬКО ПРИ ПЕРЕЗАПУСКЕ ФАЙЛА
                                break;
                            }

                            if (string.IsNullOrEmpty(outputLine))
                            {
                                Console.WriteLine($"{Name} Считана пустая строка");
                                continue;
                            }

                            await writer.WriteLineAsync(outputLine);
                            _sentLines++;
                            SendNotification?.Invoke(_sentLines, _finalLines);
                            Console.WriteLine($"{Name} → {outputLine} ({_sentLines}/{_finalLines})");
                            await Task.Delay(settings.Delay, _cts.Token);
                        }
                        else if (settings.GroupCount > 1)
                        {
                            var lines = new List<string>();
                            for (int i = 0; i < settings.GroupCount && (line = await reader.ReadLineAsync()) != null; i++)
                            {
                                lines.Add(line);
                            }

                            if (lines.Count == 0)
                            {
                                // Достигнут конец файла - перезапускаем с начала
                                Console.WriteLine($"{Name}: достигнут конец файла, перезапуск");
                                fileStream.Seek(0, SeekOrigin.Begin); // ПЕРЕМОТКА НА НАЧАЛО
                                reader.DiscardBufferedData();
                                _sentLines = 0; // СБРАСЫВАЕМ ТОЛЬКО ПРИ ПЕРЕЗАПУСКЕ ФАЙЛА
                                break;
                            }

                            string lineToSend = string.Join(settings.DataSeparator, lines);
                            string outputLine = $"{settings.DataHeader}{lineToSend}{settings.DataTerminator}";

                            await writer.WriteLineAsync(outputLine);
                            _sentLines++;
                            SendNotification?.Invoke(_sentLines, _finalLines);

                            Console.WriteLine($"{Name} → {outputLine} ({_sentLines}/{_finalLines})");
                            await Task.Delay(settings.Delay, _cts.Token);
                        }
                    }

                    // КОРОТКАЯ ПАУЗА ПЕРЕД СЛЕДУЮЩЕЙ ИТЕРАЦИЕЙ
                    if (!_cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(100, _cts.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{Name}: ошибка или отключение клиента ({ex.Message})");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        // Начало отправки
        public async Task <bool> StartStreaming(TaskSettings settings)
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                Console.WriteLine($"{Name}: нет назначенного файла!");
                return false;
            }

            // ЗАЩИТА ОТ ПОВТОРНОГО ЗАПУСКА
            if (_isStreaming || _isProcessing)
            {
                Console.WriteLine($"{Name}: трансляция уже запущена");
                return false;
            }
            if (client == null) await AcceptClientsAsync(_cts.Token);
            _isProcessing = true;
            _isStreaming = true;

            // ЗАПУСКАЕМ ТОЛЬКО ЕСЛИ ЕЩЕ НЕ ЗАПУЩЕН
            if (_currentStreamingTask == null || _currentStreamingTask.IsCompleted)
            {
                _currentStreamingTask = HandleClientAsync(settings);
            }

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
            _isStreaming = true; // ПРАВИЛЬНО - ТОЛЬКО _isStreaming!
            Console.WriteLine($"{Name}: трансляция возобновлена");
        }

        // Остановка после завершения файла
        public void StopStreaming()
        {
            _isStreaming = false;
            _isProcessing = false;
            _sentLines = 0;
            SendNotification?.Invoke(_sentLines, _finalLines);
            Console.WriteLine($"{Name} остановлена");
        }

        // Полная остановка
        public void Stop()
        {
            _cts.Cancel();
            _listener?.Stop();
            IsRunning = false;
            _isStreaming = false;
            _isProcessing = false;
            Console.WriteLine($"{Name} остановлена и удалена");
        }

        public void DropTask()
        {
            IsReady = false;
            _filePath = string.Empty;
        }
        public static int CountLines(string filePath)
        {
            int count = 0;
            using (var reader = new StreamReader(filePath))
            {
                while (reader.ReadLine() != null)
                {
                    count++;
                }
            }
            return count;
        }
    }
}