using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using System.IO.Pipes;

namespace StarMap.Types.Pipes
{
    public class PipeClient
    {
        private readonly string _pipeName;
        private NamedPipeClientStream? _client;

        private Task? _readingTask;
        private CancellationTokenSource? _readingCts;

        public event EventHandler<Any>? OnMessage;

        public PipeClient(string pipeName)
        {
            _pipeName = pipeName;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            _client = new NamedPipeClientStream(
                serverName: ".",
                pipeName: _pipeName,
                direction: PipeDirection.InOut,
                options: PipeOptions.Asynchronous);


            Console.WriteLine($"StarMap - Connecting to pipe {_pipeName}...");
            await _client.ConnectAsync(cancellationToken);

            _readingCts = new CancellationTokenSource();
            _readingTask = Task.Run(() => MessageReaderThread(_readingCts.Token), cancellationToken);
        }

        private async Task MessageReaderThread(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_client is null)
                        break;

                    var message = await _client.ReadProtoAsync(cancellationToken);

                    _ = Task.Run(() => OnMessage?.Invoke(this, message), cancellationToken);
                }
                catch (OperationCanceledException e) { }
            }
        }

        public async Task SendMessageAsync(IMessage message, CancellationToken cancellationToken = default)
        {
            if (_client is null)
                return;

            await _client.WriteProtoAsync(message, cancellationToken);
        }

        public void Dispose()
        {
            _readingCts?.Cancel();
            _readingTask?.Wait(TimeSpan.FromSeconds(5));
            _client?.Dispose();
        }
    }
}
