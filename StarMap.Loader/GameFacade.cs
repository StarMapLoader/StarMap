using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using StarMap.Types.Mods;
using StarMap.Types.Pipes;
using StarMap.Types.Proto.IPC;

namespace StarMap
{
    internal class GameFacade : IGameFacade, IAsyncDisposable
    {
        private readonly PipeClient _pipe;

        private bool _connected = false;

        public GameFacade(PipeClient pipe)
        {
            _pipe = pipe;
        }

        public async Task<string> Connect()
        {
            await _pipe.ConnectAsync(default);
            _connected = true;

            var connectResponse = await RequestData(new IPCConnectRequest());

            if (!connectResponse.Is(IPCConnectResponse.Descriptor)) return "";

            return connectResponse.Unpack<IPCConnectResponse>().GameLocation;
        }

        public async ValueTask DisposeAsync()
        {
            //await RequestData(new IPCClosePipeMessage());
            _pipe.Dispose();
        }

        public async Task<Any> RequestData(IMessage request)
        {
            if (_pipe is null || !_connected)
                throw new InvalidOperationException("Pipe is not connected.");

            var messageReceived = new TaskCompletionSource<Any>(TaskCreationOptions.RunContinuationsAsynchronously);
            var requestId = Guid.NewGuid().ToString();

            void Handler(object? sender, Any message)
            {
                if (!message.Is(PipeMessage.Descriptor)) return;

                var responseWrapper = message.Unpack<PipeMessage>();
                if (responseWrapper.RequestId != requestId) return;

                messageReceived.SetResult(responseWrapper.Payload);
                _pipe.OnMessage -= Handler;
            }

            _pipe.OnMessage += Handler;
            await _pipe.SendMessageAsync(Any.Pack(new PipeMessage()
            {
                RequestId = requestId,
                Payload = Any.Pack(request)
            }));
            return await messageReceived.Task;
        }
    }
}
