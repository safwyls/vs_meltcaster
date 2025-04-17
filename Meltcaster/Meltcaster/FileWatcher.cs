using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Meltcaster;

public class FileWatcher 
{
    private readonly MeltcasterModSystem _mod;
    private DateTime lastChange = DateTime.MinValue;
    private readonly List<FileSystemWatcher> watchers = new();
    private static ILogger ModLogger => MeltcasterModSystem.Instance.Logger;

    public bool Queued { get; set; }

    public FileWatcher(MeltcasterModSystem mod) {
        _mod = mod;

        var paths = new[]
        {
            (GamePaths.ModConfig, $"{mod.ModId}.json", false),
            (Path.Combine(GamePaths.ModConfig, "meltcaster", "recipes"), "*.json", true)
        };

        foreach (var (path, filter, scanSubDir) in paths)
        {
            var watcher = new FileSystemWatcher(path)
            {
                Filter = filter,
                IncludeSubdirectories = scanSubDir,
                EnableRaisingEvents = true
            };

            watcher.Changed += Changed;
            watcher.Created += Changed;
            watcher.Deleted += Changed;
            watcher.Renamed += Changed;
            watcher.Error += Error;

            watchers.Add(watcher);
        }
    }

    private void Changed(object sender, FileSystemEventArgs e)
    {
        // Debounce chcnages
        var now = DateTime.UtcNow;
        if ((now - lastChange).TotalMilliseconds < 200) return;

        lastChange = now;
        _mod.Api.Event.EnqueueMainThreadTask(() => QueueReload(true), "queueReload");
    }

    private void Error(object sender, ErrorEventArgs e) {
        ModLogger.Error(e.GetException().ToString());
        _mod.Api.Event.EnqueueMainThreadTask(() => QueueReload(), "queueReload");
    }

    /// <summary>
    /// Workaround for <a href='https://github.com/dotnet/runtime/issues/24079'>dotnet#24079</a>.
    /// </summary>
    private void QueueReload(bool changed = false) 
    {
        // Check if already queued for reload
        if (Queued) return;

        // Mark as queued
        Queued = true;

        // Inform console/log
        if (changed) ModLogger.Event("Detected meltcaster config was changed, reloading.");

        // Wait for other changes to process
        _mod.Api.Event.RegisterCallback(_ => {
            // Reload the config
            _mod.ReloadConfig(_mod.Api);

            // Wait some more to remove this change from the queue since the reload triggers another write
            _mod.Api.Event.RegisterCallback(_ => {
                // Unmark as queued
                Queued = false;
            }, 100);
        }, 100);
    }

    public void Dispose() 
    {
        foreach (var watcher in watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Changed -= Changed;
            watcher.Created -= Changed;
            watcher.Deleted -= Changed;
            watcher.Renamed -= Changed;
            watcher.Error -= Error;
            watcher.Dispose();
        }

        watchers.Clear();
    }
}
