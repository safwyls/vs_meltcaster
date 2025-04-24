using Meltcaster.BlockEntities;
using Meltcaster.Blocks;
using Meltcaster.Config;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Meltcaster
{
    public class MeltcasterModSystem : ModSystem
    {
        public ICoreAPI Api { get; private set; } = null!;
        public static MeltcasterConfig? Config { get; private set; }
        public static MeltcasterModSystem Instance { get; private set; }
        public ILogger Logger => Mod.Logger;
        public string ModId => Mod.Info.ModID;
        private FileWatcher? _fileWatcher;
        public bool AssetsFinalized { get; private set; } = false;

        public override void Start(ICoreAPI api)
        {
            Instance = this;
            Api = api;
            base.Start(api);

            api.RegisterBlockClass(Mod.Info.ModID + ".blockmeltcaster", typeof(BlockMeltcaster));
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".blockentitymeltcaster", typeof(BlockEntityMeltcaster));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            AssetsFinalized = true;
            
            Mod.Logger.Debug("Assets finalized. Loading config...");

            // Wait til all assets are loaded to resolve recipes in config
            ReloadConfig(api);
        }

        public void ReloadConfig(ICoreAPI api)
        {
            (_fileWatcher ??= new FileWatcher(this)).Queued = true;
            
            var _config = api.LoadModConfig<MeltcasterConfig>("meltcaster.json");
            if (_config == null)
            {
                Mod.Logger.Warning("Missing config! Using default.");
                Config = MeltcasterConfig.GetDefault();
                api.StoreModConfig(Config, "meltcaster.json");
            }
            else
            {
                Config = _config;
            }

            if (AssetsFinalized)
            {
                Config?.ResolveAll(api.World);
                Mod.Logger.Notification($"Loaded {Config?.MeltcastRecipes.ToArray().Where(r => r.Input.ResolvedItemstack != null).Count() ?? 0} meltcasting recipes.");
            }

            Api.Event.RegisterCallback(_ => _fileWatcher.Queued = false, 100);
        }

        public override void Dispose()
        {
            _fileWatcher?.Dispose();
            _fileWatcher = null;
        }
    }
}
