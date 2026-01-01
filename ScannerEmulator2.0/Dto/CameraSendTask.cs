using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Enums;
using ScannerEmulator2._0.Reactive;
using System.IO;
using System.Threading.Channels;

namespace ScannerEmulator2._0.Dto
{
    public class CameraSendTask : IModel
    {
        public ReactiveProperty<Guid> Id { get; } = new(Guid.NewGuid());
        public ReactiveProperty<TaskState> State { get; } = new(TaskState.Created);
        public ReactiveProperty<string> FilePath { get; } = new(string.Empty);
        public ReactiveProperty<int> Progress { get; } = new(0);

        private TaskSettings? _settings;

        private readonly ManualResetEventSlim _pauseEvent = new(true);
        private CancellationTokenSource? _cts;

        public CameraSendTask(string filePath)
        {
            FilePath.Value = filePath;
        }

        public void SetSettings(TaskSettings settings)
        {
            _settings = settings;
        }

        public async Task RunAsync(ChannelWriter<OutgoingPacket> writer, CancellationToken externalToken)
        {
            if (_settings == null)
                throw new InvalidOperationException("TaskSettings not set");

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var token = _cts.Token;

            State.Value = TaskState.Running;

            int totalLines = File.ReadLines(FilePath.Value).Count();
            int sentLines = 0;

            using var reader = new StreamReader(File.OpenRead(FilePath.Value));

            while (!reader.EndOfStream && !token.IsCancellationRequested)
            {
                _pauseEvent.Wait(token);

                string payload;

                if (_settings.GroupCount == 1)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null)
                        break;

                    payload = line + _settings.DataSeparator;
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
                }

                await writer.WriteAsync(new OutgoingPacket(payload), token);

                sentLines += _settings.GroupCount;
                Progress.Value = Math.Min(100, sentLines * 100 / totalLines);

                await Task.Delay(_settings.Delay, token);
            }

            State.Value = TaskState.Stopped;
        }

        public void Start()
        {
            if (State.Value == TaskState.Created || State.Value == TaskState.Stopped)
            {
                State.Value = TaskState.Paused;
                _pauseEvent.Reset();
            }
        }
        public void Pause()
        {
            if (State.Value == TaskState.Running)
            {
                State.Value = TaskState.Paused;
                _pauseEvent.Reset();
            }
        }
        public void Resume()
        {
            if (State.Value == TaskState.Paused)
            {
                State.Value = TaskState.Running;
                _pauseEvent.Set();
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            State.Value = TaskState.Stopped;
        }
    }
}
