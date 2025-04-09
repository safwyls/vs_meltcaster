using System.IO;
using Vintagestory.API.Config;

namespace Meltcaster;

public class FileWatcher 
{
    private readonly FileSystemWatcher _watcher;
    private readonly MeltcasterModSystem _mod;

    public bool Queued { get; set; }

    public FileWatcher(MeltcasterModSystem mod) {
        _mod = mod;

        _watcher = new FileSystemWatcher(GamePaths.ModConfig) {
            Filter = $"{mod.ModId}.json",
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        _watcher.Changed += Changed;
        _watcher.Created += Changed;
        _watcher.Deleted += Changed;
        _watcher.Renamed += Changed;
        _watcher.Error += Error;
    }

    private void Changed(object sender, FileSystemEventArgs e)
    {
        _mod.Api.Event.EnqueueMainThreadTask(() => QueueReload(true), "queueReload");
    }

    private void Error(object sender, ErrorEventArgs e) {
        _mod.Api.Logger.Error(e.GetException().ToString());
        _mod.Api.Event.EnqueueMainThreadTask(() => QueueReload(), "queueReload");
    }

    /// <summary>
    /// My workaround for <a href='https://github.com/dotnet/runtime/issues/24079'>dotnet#24079</a>.
    /// </summary>
    private void QueueReload(bool changed = false) 
    {
        // check if already queued for reload
        if (Queued)
        {
            return;
        }

        // mark as queued
        Queued = true;

        // inform console/log
        if (changed)
        {
            _mod.Api.Logger.Event("Detected meltcaster config was changed, reloading.");
        }

        // wait for other changes to process
        _mod.Api.Event.RegisterCallback(_ =>
        {
            // reload the config
            _mod.ReloadConfig(_mod.Api);

            // wait some more to remove this change from the queue since the reload triggers another write
            _mod.Api.Event.RegisterCallback(_ =>
            {
                // unmark as queued
                Queued = false;
            }, 100);
        }, 100);
    }

    public void Dispose() {
        _watcher.Changed -= Changed;
        _watcher.Created -= Changed;
        _watcher.Deleted -= Changed;
        _watcher.Renamed -= Changed;
        _watcher.Error -= Error;

        _watcher.Dispose();
    }
}
