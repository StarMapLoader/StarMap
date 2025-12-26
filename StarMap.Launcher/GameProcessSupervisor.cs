using StarMap.Types.Pipes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace StarMapLoader
{
    internal class GameProcessSupervisor
    {
        private readonly string _gamePath;
        private readonly LoaderFacade _facade;
        private readonly PipeServer _pipeServer;

        private Process? _game;


        public GameProcessSupervisor(string gamePath, LoaderFacade facade, PipeServer pipeServer)
        {
            _gamePath = gamePath;
            _facade = facade;
            _pipeServer = pipeServer;
        }

        public async Task<Task<bool>> TryStartGameAsync(CancellationToken cancellationToken)
        {
            var processExitedTsc = new TaskCompletionSource<bool>();

            var psi = new ProcessStartInfo
            {
                FileName = _gamePath,
                Arguments = $"{_pipeServer.PipeName}",
                UseShellExecute = true, // Use the operating system shell to start the process
                CreateNoWindow = false, // Create a new window for the process
                RedirectStandardOutput = false,
                RedirectStandardError = false,
            };

            var pipeConntection = _pipeServer.StartListening(cancellationToken);

            if (Debugger.IsAttached)
            {
                _game = FindProcess();
            }

            if (_game is null)
            {
                _game = new Process
                {
                    StartInfo = psi,
                    EnableRaisingEvents = true
                };
                _game.Start();
            }

            await pipeConntection;

            if (cancellationToken.IsCancellationRequested)
                return Task.FromResult(false);

            var processStartedTcs = new TaskCompletionSource();

            void OnPrcessStarted(object? sender, EventArgs args)
            {
                _facade.OnProcessStarted -= OnPrcessStarted;
                processStartedTcs.TrySetResult();
            }

            _facade.OnProcessStarted += OnPrcessStarted;
            await processStartedTcs.Task;

            _game.EnableRaisingEvents = true;
            _game.Exited += (s, e) =>
            {
                processExitedTsc.TrySetResult(true);
                _pipeServer.Stop();
            };

            return processExitedTsc.Task;
        }

        private static Process? FindProcess()
        {
            string processName = "StarMap";
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.FirstOrDefault();
        }
    }
}
