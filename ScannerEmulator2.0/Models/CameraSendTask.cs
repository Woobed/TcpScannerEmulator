using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Enums;
using ScannerEmulator2._0.Reactive;
using ScannerEmulator2._0.TCPScanner;
using System.IO;
using System.Threading.Channels;

namespace ScannerEmulator2._0.Dto
{
    public class CameraSendTask : IModel
    {
        public ReactiveProperty<Guid> Id { get; } = new(Guid.NewGuid());
        public ReactiveProperty<TaskState> State { get; } = new(TaskState.Created);
        public ReactiveProperty<string> FilePath { get; } = new(string.Empty);
        public ReactiveProperty<string> FileName { get; } = new(string.Empty);
        public ReactiveProperty<int> Progress { get; } = new(0);
        public ReactiveProperty<int> SentGroups { get; } = new(0);
        public ReactiveProperty<int> TotalGroups { get; } = new(0);
        public ReactiveProperty<string> ProgressText { get; } = new("0/0");

        public TcpCameraEmulator? Emulator { get; private set; }

        private TaskSettings? _settings;
        private CancellationTokenSource? _cts;
        private volatile bool _isPaused = false;
        private volatile bool _isRunning = false;

        public CameraSendTask(string filePath)
        {
            FilePath.Value = filePath;
            FileName.Value = Path.GetFileName(filePath);
        }

        public void SetSettings(TaskSettings settings)
        {
            _settings = settings;
            CalculateTotalGroups();
        }

        public void AssignToEmulator(TcpCameraEmulator emulator)
        {
            Emulator = emulator;
        }

        private void CalculateTotalGroups()
        {
            if (_settings == null || string.IsNullOrEmpty(FilePath.Value))
                return;

            try
            {
                int totalLines = File.ReadLines(FilePath.Value).Count();
                int totalGroups = (totalLines + _settings.GroupCount - 1) / _settings.GroupCount;
                TotalGroups.Value = totalGroups;
                UpdateProgressText();
            }
            catch
            {
                TotalGroups.Value = 0;
                UpdateProgressText();
            }
        }

        public void UpdateProgressText()
        {
            ProgressText.Value = $"{SentGroups.Value}/{TotalGroups.Value}";
            if (TotalGroups.Value > 0)
            {
                Progress.Value = Math.Min(100, SentGroups.Value * 100 / TotalGroups.Value);
            }
            else
            {
                Progress.Value = 0;
            }
        }

        public void StartExecution()
        {
            if (Emulator == null)
                throw new InvalidOperationException("Task is not assigned to any emulator");

            if (_isRunning && State.Value == TaskState.Paused)
            {
                Resume();
                return;
            }

            if (State.Value == TaskState.Created || State.Value == TaskState.Stopped)
            {
                _cts?.Cancel();
                _cts?.Dispose();

                State.Value = TaskState.Running;
                _isPaused = false;
                _isRunning = true;

                SentGroups.Value = 0;
                CalculateTotalGroups();

                Emulator.EnqueueTask(this);
            }
        }

        public async Task RunAsync(ChannelWriter<OutgoingPacket> writer, CancellationToken externalToken)
        {
            if (_settings == null)
                throw new InvalidOperationException("TaskSettings not set");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = _cts.Token;

            State.Value = TaskState.Running;
            _isPaused = false;
            _isRunning = true;

            int totalLines = File.ReadLines(FilePath.Value).Count();
            int sentLines = 0;
            SentGroups.Value = 0;
            CalculateTotalGroups();

            using var reader = new StreamReader(File.OpenRead(FilePath.Value));

            while (!reader.EndOfStream && !token.IsCancellationRequested)
            {
                while (_isPaused && !token.IsCancellationRequested)
                {
                    await Task.Delay(100, token);
                }

                if (token.IsCancellationRequested)
                    break;

                string payload;

                if (_settings.GroupCount == 1)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null)
                        break;

                    payload = line + _settings.DataSeparator;
                    //SentGroups.Value++;
                }
                else
                {
                    var group = new List<string>();
                    for (int i = 0; i < _settings.GroupCount; i++)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null) break;
                        group.Add(line);
                    }
                    if (group.Count == 0) break;


                    payload = _settings.DataHeader +
                              string.Join(_settings.DataSeparator, group) +
                              _settings.DataSeparator +
                              _settings.DataTerminator;

                    //SentGroups.Value++;
                    sentLines += group.Count;
                }

                var packet = new OutgoingPacket();
                packet.Payload = payload;
                packet.log = new(payload, FileName.Value ?? string.Empty);
                packet.Delay = _settings.Delay;
                packet.CreatedAt = DateTime.UtcNow;
                packet.Hash = this.GetHashCode();

                await writer.WriteAsync(packet, token);

                int delayRemaining = _settings.Delay;
                while (delayRemaining > 0 && !token.IsCancellationRequested)
                {
                    if (_isPaused)
                    {
                        await Task.Delay(100, token);
                        continue;
                    }

                    var stepDelay = Math.Min(10, delayRemaining);
                    await Task.Delay(stepDelay, token);
                    delayRemaining -= stepDelay;
                }
            }

            if (!_isPaused)
            {
                State.Value = token.IsCancellationRequested ? TaskState.Stopped : TaskState.Stopped;
                _isRunning = false;
            }
        }

        public void IncrementSentGroups(int hash)
        {
            if (this.GetHashCode() == hash)
            {
                SentGroups.Value++;
                UpdateProgressText();
            }
        }

        public void Pause()
        {
            if (State.Value == TaskState.Running && _isRunning)
            {
                State.Value = TaskState.Paused;
                _isPaused = true;
            }
        }

        public void Resume()
        {
            if (State.Value == TaskState.Paused && _isRunning)
            {
                State.Value = TaskState.Running;
                _isPaused = false;
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            State.Value = TaskState.Stopped;
            _isPaused = false;
            _isRunning = false;
        }
    }
}