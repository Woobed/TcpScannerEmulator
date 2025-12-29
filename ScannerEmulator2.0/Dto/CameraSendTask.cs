using ScannerEmulator2._0.Abstractions;
using ScannerEmulator2._0.Reactive;
using System.IO;
using System.Threading.Channels;

namespace ScannerEmulator2._0.Dto
{
    public class CameraSendTask : IModel
    {
        public ReactiveProperty<Guid> Id { get; set; } = new(Guid.NewGuid());

        private readonly string _filePath;
        private TaskSettings? _settings;
        private ChannelWriter<OutgoingPacket> _writer;
        private CancellationToken _token;

        public CameraSendTask(string filePath)
        {
            _filePath = filePath;
        }
        public void SetSettings(TaskSettings settings)
        {
            _settings = settings;
        }
        public async Task RunAsync(ChannelWriter<OutgoingPacket> writer,
            CancellationToken token)
        {
            _writer = writer;
            _token = token;
            using var reader = new StreamReader(File.OpenRead(_filePath));

            while (!_token.IsCancellationRequested)
            {
                if (_settings == null) return;
                if (_settings.GroupCount == 1)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null)
                        return;

                    await _writer.WriteAsync(
                        new OutgoingPacket(line + _settings.DataSeparator),
                        _token);
                }
                else
                {
                    var group = new List<string>();

                    for (int i = 0; i < _settings.GroupCount; i++)
                    {
                        var line = await reader.ReadLineAsync();
                        if (line == null)
                            break;

                        group.Add(line);
                    }

                    if (group.Count == 0)
                        return;

                    string payload =
                        _settings.DataHeader +
                        string.Join(_settings.DataSeparator, group) +
                        _settings.DataSeparator +
                        _settings.DataTerminator;

                    await _writer.WriteAsync(
                        new OutgoingPacket(payload),
                        _token);
                }

                await Task.Delay(_settings.Delay, _token);
            }
        }
    }
}