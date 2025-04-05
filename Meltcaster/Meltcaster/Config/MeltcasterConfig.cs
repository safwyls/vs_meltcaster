using System.Collections.Generic;
using Vintagestory.API.Common;
using Newtonsoft.Json;
using System.Linq;

namespace Meltcaster.Config
{
    public class MeltcasterConfig
    {
        [JsonProperty("showRecipeInDialog")]
        public bool ShowRecipeInDialog { get; set; } = false; // show the output item in the dialog when melting

        [JsonProperty("meltcastList")]
        public List<MeltcastRecipe>? MeltcastRecipes { get; set; }

        [JsonIgnore]
        public Dictionary<string, MeltcastRecipe>? MeltcastRecipeByCode { get; private set; }

        public void ResolveAll(ICoreAPI api, string domain = "game")
        {
            if (MeltcastRecipes == null) return;

            foreach (var recipe in MeltcastRecipes)
            {
                recipe.InputJson?.Resolve(api.World, domain);

                if (recipe.Outputs != null)
                {
                    foreach (var output in recipe.Outputs)
                    {
                        if (output.IsGroup && output.ItemGroup?.Count > 0)
                        {
                            foreach (var item in output.ItemGroup)
                            {
                                item.ItemStack?.Resolve(api.World, domain);
                            }
                        }
                        else
                        {
                            output.ItemStack?.Resolve(api.World, domain);
                        }
                    }
                }

                var codeStr = recipe.InputJson?.Code?.ToString();
                if (codeStr != null)
                {
                    MeltcastRecipeByCode ??= new();
                    MeltcastRecipeByCode[codeStr] = recipe;
                }
            }
        }

        public static MeltcasterConfig GetDefault()
        {
            return new MeltcasterConfig
            {
                MeltcastRecipes = new List<MeltcastRecipe>
                {
                    new MeltcastRecipe
                    {
                        InputJson = new JsonItemStack
                        {
                            Code = new AssetLocation("metal-scraps"),
                            Type = EnumItemClass.Block
                        },
                        MeltcastTemp = 1100,
                        MeltcastTime = 30,
                        Outputs = new List<MeltcastOutput>
                        {
                            new()
                            {
                                IsGroup = true,
                                GroupRollInterval = 16,
                                Chance = 1f,
                                GroupDesc = "Metal Bits",
                                ItemGroup = new()
                                {
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-copper"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 1f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-cupronickel"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.2f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-brass"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.2f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-tinbronze"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.7f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-bismuthbronze"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.6f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-blackbronze"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.5f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-electrum"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-iron"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.5f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-meteoriciron"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.1f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-nickel"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.3f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-steel"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.4f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-blistersteel"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.2f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-gold"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.1f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-lead"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.3f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-chromium"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.2f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-titanium"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-zinc"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.3f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-silver"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.2f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-bismuth"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.3f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-molybdochalkos"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.3f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-leadsolder"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.2f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-silversolder"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.1f
                                    }
                                }
                            },
                            new()
                            {
                                ItemStack = new JsonItemStack
                                {
                                    Code = new AssetLocation("gear-rusty"),
                                    Type = EnumItemClass.Item,
                                    StackSize = 1
                                },
                                Chance = 0.05f
                            },
                            new() {
                                ItemStack = new JsonItemStack
                                {
                                    Code = new AssetLocation("gear-temporal"),
                                    Type = EnumItemClass.Item,
                                    StackSize = 1
                                },
                                Chance = 0.01f
                            }
                        }
                    }
                }
            };
        }
    }

    public class MeltcastRecipe
    {
        [JsonProperty("input")]
        public JsonItemStack? InputJson { get; set; }

        [JsonProperty("meltcastTemp")]
        public float MeltcastTemp { get; set; }

        [JsonProperty("meltcastTime")]
        public float MeltcastTime { get; set; }

        [JsonProperty("outputs")]
        public List<MeltcastOutput>? Outputs { get; set; }

        public void Resolve(ICoreAPI api, string domain)
        {
            InputJson?.Resolve(api.World, domain);
            if (Outputs == null) return;
            foreach (var output in Outputs)
            {
                output.ItemStack?.Resolve(api.World, domain);
            }
        }

        [JsonIgnore]
        public ItemStack? InputStack => InputJson?.ResolvedItemstack?.Clone();
    }

    public class MeltcastOutput
    {
        [JsonProperty("item", NullValueHandling = NullValueHandling.Ignore)]
        public JsonItemStack? ItemStack { get; set; }

        [JsonProperty("isGroup", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsGroup { get; set; } = false; 

        [JsonProperty("groupRollInterval", NullValueHandling = NullValueHandling.Ignore)]
        public int? GroupRollInterval { get; set; } // this is the interval to roll for a new item type

        [JsonProperty("groupDesc", NullValueHandling = NullValueHandling.Ignore)]
        public string? GroupDesc { get; set; } // Group item description for gui

        [JsonProperty("chance")]
        public float Chance { get; set; } = 1f; // 1.0 = 100% chance, group items are relatively weights within the overall group chance

        [JsonProperty("itemGroup", NullValueHandling = NullValueHandling.Ignore)]
        public List<MeltcastOutput>? ItemGroup { get; set; }

        [JsonIgnore]
        public JsonItemStack? ResolvedItem => IsGroup ? null : ItemStack;

        public MeltcastOutput? WeightedRandomOrFirst(ICoreAPI api)
        {
            if (ItemGroup != null && ItemGroup.Count > 0)
            {
                float totalWeight = ItemGroup.Sum(o => o.Chance);
                float random = (float)api.World.Rand.NextDouble() * totalWeight;

                float accum = 0f;
                foreach (MeltcastOutput option in ItemGroup)
                {
                    accum += option.Chance;
                    if (accum >= random)
                    {
                        return option;
                    }
                }
            }

            return ItemGroup?[0];
        }

        public ItemStack? GetResolvedStack()
        {
            return ItemStack?.ResolvedItemstack?.Clone();
        }
    }
}
