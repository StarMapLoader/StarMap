using StarMap.Index.API;
using StarMap.Types.Pipes;
using StarMap.Types;
using System.Diagnostics;

namespace StarMapLoader
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MainInner().GetAwaiter().GetResult();
        }

        static async Task MainInner()
        {
            var config = new LoaderConfig();
            if (!config.TryLoadConfig()) return;

            using var remoteModRepository = new ModRepositoryClient(config.RepositoryLocation);
            var modRepository = new ModRepository(config.GameLocation, remoteModRepository);

            var shouldReload = true;

            var pipeName = Debugger.IsAttached ? "starmap_pipe" : $"starmap_pipe_{Guid.NewGuid()}";
            using var pipeServer = new PipeServer(pipeName);
            using var facade = new LoaderFacade(pipeServer, config, modRepository);

            while (shouldReload)
            {
                CancellationTokenSource stopGameCancelationTokenSource = new();

                var gameSupervisor = new GameProcessSupervisor(config.GameLocation, facade, pipeServer);

                await await gameSupervisor.TryStartGameAsync(stopGameCancelationTokenSource.Token);

                shouldReload = modRepository.HasChanges;
                if (shouldReload)
                {
                    modRepository.ApplyModUpdates();
                }
            }
        }
    }
}
