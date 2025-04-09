using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Vintagestory.API.Common;

namespace Meltcaster.Config
{
    public class MeltcasterConfig
    {
        public int TemporalBurnRate { get; set; } = 5;
        public required List<MeltcastRecipe> MeltcastRecipes { get; set; }

        [JsonIgnore]
        public Dictionary<AssetLocation, MeltcastRecipe>? MeltcastRecipeByCode { get; private set; }

        public void ResolveAll(IWorldAccessor world, string domain = "meltcaster")
        {
            foreach(var recipe in MeltcastRecipes)
            {
                recipe?.Input?.Resolve(world, domain);

                if (recipe?.Output == null) return;
                foreach (var output in recipe.Output)
                {
                    output.Resolve(world, domain);
                }

                if (recipe.Input.Code != null)
                {
                    MeltcastRecipeByCode ??= new();
                    MeltcastRecipeByCode[recipe.Input.Code] = recipe;
                }
            }
        }
    }

    public class MeltcastRecipe
    {
        public required JsonItemStack Input { get; set; }
        public float MeltcastTemp { get; set; }
        public float MeltcastTime { get; set; }
        public required List<MeltcastOutput> Output { get; set; }
        public bool Temporal { get; set; } = false;
    }

    public class MeltcastOutput : JsonItemStack
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float Chance { get; set; } = 1f;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? GroupRollInterval { get; set; } // How often to reroll the group output. 16 = every 16 items.

        // Non-serialized properties
        [JsonIgnore]
        public bool IsGroup => Group is not null && Group.Count > 0;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<MeltcastOutput>? Group { get; set; }

        /// <summary>
        /// Returns a weighted random output from the group
        /// </summary>
        /// <param name = "api"></param>
        /// <returns></returns>
        public MeltcastOutput? WeightedRandom(ICoreAPI api)
        {
            if (Group == null) return null;

            if (Group.Count > 0)
            {
                float totalWeight = Group.Sum(o => o.Chance);
                float random = (float)api.World.Rand.NextDouble() * totalWeight;

                float accum = 0f;
                foreach (MeltcastOutput option in Group)
                {
                    accum += option.Chance;
                    if (accum >= random)
                    {
                        return option;
                    }
                }
            }

            return null;
        }

        public new bool Resolve(IWorldAccessor resolver, string sourceForErrorLogging, bool printWarningOnError = true)
        {
            // To-do: Something wrong with this, group is never being deserialized properly from json
            // Resolve group items inside this item
            if (IsGroup && Group?.Count > 0)
            {
                foreach (var output in Group)
                {
                    output.Resolve(resolver, sourceForErrorLogging, printWarningOnError);
                }

                return true;
            }
            else
            {
                if (Code is null) return false;
                return base.Resolve(resolver, sourceForErrorLogging, printWarningOnError);
            }
        }
    }
}
