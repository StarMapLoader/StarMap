using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using StarMap.Types;
using StarMap.Types.Pipes;
using StarMap.Types.Proto.IPC;

namespace StarMapLoader
{
    internal class LoaderFacade : IDisposable
    {
        private readonly PipeServer _pipeServer;
        private readonly LoaderConfig _config;
        private readonly ModRepository _repository;

        public event EventHandler? OnProcessStarted;

        public LoaderFacade(PipeServer pipeServer, LoaderConfig config, ModRepository repository)
        {
            _pipeServer = pipeServer;
            _config = config;
            _repository = repository;

            _pipeServer.OnMessage += PipeServer_OnRequest;
        }

        private void PipeServer_OnRequest(object? sender, Any pipeMessage)
        {
            if (!pipeMessage.Is(PipeMessage.Descriptor)) return;

            var unpackedPipeMessage = pipeMessage.Unpack<PipeMessage>(); 
            var message = unpackedPipeMessage.Payload;

            IMessage? response = null;

            if (message.Is(IPCConnectRequest.Descriptor))
            {
                response = new IPCConnectResponse()
                {
                    GameLocation = _config.GameLocation,
                };

                OnProcessStarted?.Invoke(this, EventArgs.Empty);
            }
            if (message.Is(IPCGetCurrentManagedModsRequest.Descriptor))
            {
                var modsRepsonse = new IPCGetCurrentManagedModsResponse();
                modsRepsonse.Mods.AddRange(_repository.LoadedModInformation);
                response = modsRepsonse;
            }

            if (message.Is(IPCGetModsRequest.Descriptor))
            {
                var availableModsResponse = new IPCGetModsResponse();
                var availableMods = _repository.GetPossibleMods().GetAwaiter().GetResult().Select(repositoryMod => new IPCMod() 
                { 
                    Id = repositoryMod.Id,
                    Name = repositoryMod.Name,
                    Author = repositoryMod.Author,
                });

                availableModsResponse.Mods.AddRange(availableMods);
                response = availableModsResponse;
            }

            if (message.Is(IPCGetModDetailsRequest.Descriptor))
            {
                var modDetailsRequest = message.Unpack<IPCGetModDetailsRequest>();

                var modDetails = _repository.GetModInformation(modDetailsRequest.Id).GetAwaiter().GetResult();
                if (modDetails is null)
                {
                    response = new IPCGetModDetailsResponse();
                }
                else
                {
                    var mod = new IPCModDetails()
                    {
                        Mod = new IPCMod()
                        {
                            Id = modDetails.Mod.Id,
                            Name = modDetails.Mod.Name,
                            Author = modDetails.Mod.Author,
                        },
                        Description = modDetails.Description,
                    };
                    mod.Versions.AddRange(modDetails.Versions.Select((version) => new IPCModVersion() 
                    { 
                        Id = version.Id,
                        Version = version.Version,
                        DownloadLocation = version.DownloadLocation,
                    }));

                    response = new IPCGetModDetailsResponse()
                    {
                        Mod = mod
                    };
                }
            }

            if (message.Is(IPCSetManagedMods.Descriptor))
            {
                var setModUpdates = message.Unpack<IPCSetManagedMods>();

                _repository.SetModUpdates([.. setModUpdates.Updates]);

                response = new Any();
            }

            if (response is not null)
            {
                _pipeServer.SendResponseAsync(Any.Pack(new PipeMessage()
                {
                    RequestId = unpackedPipeMessage.RequestId,
                    Payload = Any.Pack(response)
                })).GetAwaiter().GetResult();
            }
        }

        public void Dispose()
        {
            _pipeServer.OnMessage -= PipeServer_OnRequest;
        }
    }
}
