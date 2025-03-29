using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Recycler.BlockEntities;
using Recycler.Blocks;
using Recycler.Config;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using System.Threading.Channels;

namespace Recycler
{
    public class RecyclerModSystem : ModSystem
    {
        public ICoreAPI Api { get; private set; } = null!;
        public static RecyclerConfig? Config { get; private set; }
        public string ModId => Mod.Info.ModID;
        private RecyclerConfig? _config;

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            Api = api;
            base.Start(api);

            var _config = api.LoadModConfig<RecyclerConfig>("recycler.json");
            if (_config == null)
            {
                api.Logger.Warning("[Recycler] Missing config! Using default.");
                Config = RecyclerConfig.GetDefault();
            }
            else
            {
                Config = _config;
                api.Logger.Notification($"[Recycler] Loaded {Config.RecycleRecipes?.Count ?? 0} recycling recipes.");
            }

            api.RegisterBlockClass(Mod.Info.ModID + ".blockrecycler", typeof(BlockRecycler));
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".blockentityrecycler", typeof(BlockEntityRecycler));
        }

        public override void StartServerSide(ICoreServerAPI sapi)
        {
            ReloadConfig();
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            // Wait til all assets are loaded to resolve recipes in config
            Config.ResolveAll(api);
        }

        public void ReloadConfig()
        {


            //(_fileWatcher ??= new FileWatcher(this)).Queued = true;

            string json = JsonConvert.SerializeObject(_config, new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            FileInfo fileInfo = new(Path.Combine(GamePaths.ModConfig, $"{ModId}.json"));
            GamePaths.EnsurePathExists(fileInfo.Directory!.FullName);
            File.WriteAllText(fileInfo.FullName, json);

            //Api.Event.RegisterCallback(_ => _fileWatcher.Queued = false, 100);
        }
    }
}
