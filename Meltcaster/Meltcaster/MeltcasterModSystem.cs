using Meltcaster.BlockEntities;
using Meltcaster.Blocks;
using Meltcaster.Config;
using System;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Meltcaster
{
    public class MeltcasterModSystem : ModSystem
    {
        public ICoreAPI Api { get; private set; } = null!;
        public static MeltcasterConfig? Config { get; private set; }
        public string ModId => Mod.Info.ModID;
        private FileWatcher? _fileWatcher;
        private bool assetsFinalized = false;

        public override void Start(ICoreAPI api)
        {
            Api = api;
            base.Start(api);

            api.RegisterBlockClass(Mod.Info.ModID + ".blockmeltcaster", typeof(BlockMeltcaster));
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".blockentitymeltcaster", typeof(BlockEntityMeltcaster));

            ReloadConfig(api);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            assetsFinalized = true;
            
            api.Logger.Debug("[Meltcaster] Assets finalized. Loading config...");
            
            // Wait til all assets are loaded to resolve recipes in config
            Config?.ResolveAll(api.World);
        }

        public void ReloadConfig(ICoreAPI api)
        {
            (_fileWatcher ??= new FileWatcher(this)).Queued = true;
            try
            {
                var _config = api.LoadModConfig<MeltcasterConfig>("meltcaster.json");
                if (_config == null)
                {
                    Mod.Logger.Warning("[Meltcaster] Missing config! Using default.");
                    Config = api.Assets.Get(new AssetLocation("meltcaster:config/default.json")).ToObject<MeltcasterConfig>();
                    api.StoreModConfig(Config, "meltcaster.json");
                }
                else
                {
                    Config = _config;
                    Mod.Logger.Notification($"[Meltcaster] Loaded {Config.MeltcastRecipes?.Count ?? 0} meltcasting recipes.");
                }
            }
            catch (Exception ex)
            {
                Mod.Logger.Error("[Meltcaster] Could not load mod config! Trying to generate from scratch instead.");
                Mod.Logger.Error(ex);
                Config = MeltcasterConfig.GetDefault();
            }
            
            if (assetsFinalized) Config?.ResolveAll(api.World);

            Api.Event.RegisterCallback(_ => _fileWatcher.Queued = false, 100);
        }

        public override void Dispose()
        {
            _fileWatcher?.Dispose();
            _fileWatcher = null;
        }
    }
}
