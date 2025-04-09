using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using Vintagestory.API.Common;

namespace Meltcaster.Config
{
    public class MeltcasterConfig
    {
        public required List<MeltcastRecipe> MeltcastRecipes { get; set; }

        [JsonIgnore]
        public Dictionary<AssetLocation, MeltcastRecipe>? MeltcastRecipeByCode { get; private set; }

        public void ResolveAll(IWorldAccessor world, string domain = "meltcaster")
        {
            foreach(var recipe in MeltcastRecipes)
            {
                recipe?.Input?.Resolve(world, domain);
                if (recipe?.Input.ResolvedItemstack is null)
                {
                    world.Logger.Error($"[Meltcaster] Failed to resolve input itemstack for recipe input: {recipe?.Input?.Code}");
                }

                if (recipe?.Output == null) return;
                foreach (var output in recipe.Output)
                {
                    output.Resolve(world, domain);
                    if (output.ResolvedItemstack is null && !output.IsGroup)
                    {
                        world.Logger.Error($"[Meltcaster] Failed to resolve recipe output itemstack: {output.Code}");
                    }
                }

                if (recipe.Input.Code != null)
                {
                    MeltcastRecipeByCode ??= new();
                    MeltcastRecipeByCode[recipe.Input.Code] = recipe;
                }
            }
        }

        /// <summary>
        /// Backup method in case mod fails to load config from assets folder
        /// This should only contain one recipe to keep this class clean
        /// </summary>
        /// <returns></returns>
        public static MeltcasterConfig GetDefault()
        {
            return new MeltcasterConfig() {
                MeltcastRecipes = new List<MeltcastRecipe>()
                {
                    new()
                    {
                        Input = new() { Code = "game:metal-scraps", Type = EnumItemClass.Block },
                        MeltcastTemp = 1200,
                        MeltcastTime = 60,
                        Output = new List<MeltcastOutput>()
                        {
                            new() { Code = "meltcast:g-metal-bits", StackSize = 1, Chance = 1,
                                Group = new() {
                                    new() { Code = "game:metalbit-copper", Type = EnumItemClass.Item, StackSize = 1, Chance = 1f },
                                    new() { Code = "game:metalbit-tinbronze", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.7f },
                                    new() { Code = "game:metalbit-bismuthbronze", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.6f },
                                    new() { Code = "game:metalbit-blackbronze", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.5f },
                                    new() { Code = "game:metalbit-iron", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.5f },
                                    new() { Code = "game:metalbit-steel", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.4f },
                                    new() { Code = "game:metalbit-nickel", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.3f },
                                    new() { Code = "game:metalbit-lead", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.3f },
                                    new() { Code = "game:metalbit-zinc", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.3f },
                                    new() { Code = "game:metalbit-bismuth", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.3f },
                                    new() { Code = "game:metalbit-molybdochalkos", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.3f },
                                    new() { Code = "game:metalbit-cupronickel", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.2f },
                                    new() { Code = "game:metalbit-brass", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.2f },
                                    new() { Code = "game:metalbit-chromium", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.2f },
                                    new() { Code = "game:metalbit-silver", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.2f },
                                    new() { Code = "game:metalbit-leadsolder", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.2f },
                                    new() { Code = "game:metalbit-silversolder", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.1f },
                                    new() { Code = "game:metalbit-meteoriciron", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.1f },
                                    new() { Code = "game:metalbit-gold", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.1f },
                                    new() { Code = "game:metalbit-electrum", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.05f },
                                    new() { Code = "game:metalbit-titanium", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.05f },
                                }
                            },
                            new() { Code = "game:gear-rusty", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.05f },
                            new() { Code = "game:gear-temporal", Type = EnumItemClass.Item, StackSize = 1, Chance = 0.01f },
                        }
                    }
                }
            };
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
                    if (output.ResolvedItemstack is null)
                    {
                        resolver.Logger.Error($"[Meltcaster] Failed to resolve recipe output itemstack for Group: {Code}, output: {output.Code}");
                    }
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
