using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Newtonsoft.Json;
using HarmonyLib;
using System;
using Vintagestory.API.Datastructures;
using ProtoBuf;
using Vintagestory.API;
using System.Threading.Channels;
using System.Linq;
using Microsoft.VisualBasic.FileIO;

namespace Recycler.Config
{
    public class RecyclerConfig
    {
        [JsonProperty("recycleList")]
        public List<RecycleRecipe>? RecycleRecipes { get; set; }

        public void ResolveAll(ICoreAPI api, string domain = "game")
        {
            if (RecycleRecipes == null) return;

            foreach (var recipe in RecycleRecipes)
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
            }
        }

        public static RecyclerConfig GetDefault()
        {
            return new RecyclerConfig
            {
                RecycleRecipes = new List<RecycleRecipe>
                {
                    new RecycleRecipe
                    {
                        InputJson = new JsonItemStack
                        {
                            Code = new AssetLocation("metal-scraps"),
                            Type = EnumItemClass.Block
                        },
                        RecycleTemp = 150,
                        RecycleTime = 5,
                        Outputs = new List<RecycleOutput>
                        {
                            new()
                            {
                                IsGroup = true,
                                Chance = 1f,
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
                                        Chance = 0.5f
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
                                        Chance = 0.3f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-tinbronze"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.5f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-bismuthbronze"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.5f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-blackbronze"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
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
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-meteoriciron"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-nickel"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-steel"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-blistersteel"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-gold"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-lead"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-chromium"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-platinum"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    //new()
                                    //{
                                    //    Code = new AssetLocation("metalbit-titanium"),
                                    //    Type = EnumItemClass.Item,
                                    //    StackSize = 1
                                    //},
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-zinc"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-silver"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-bismuth"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-molybdochalkos"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-leadsolder"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
                                    },
                                    new()
                                    {
                                        ItemStack = new JsonItemStack
                                        {
                                            Code = new AssetLocation("metalbit-silversolder"),
                                            Type = EnumItemClass.Item,
                                            StackSize = 1
                                        },
                                        Chance = 0.05f
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

    public class RecycleRecipe
    {
        [JsonProperty("input")]
        public JsonItemStack? InputJson { get; set; }

        [JsonProperty("recycleTemp")]
        public float RecycleTemp { get; set; }

        [JsonProperty("recycleTime")]
        public float RecycleTime { get; set; }

        [JsonProperty("outputs")]
        public List<RecycleOutput>? Outputs { get; set; }

        public void Resolve(ICoreAPI api, string domain)
        {
            InputJson?.Resolve(api.World, domain);
            if (Outputs == null) return;
            foreach (var output in Outputs)
            {
                output.ItemStack?.Resolve(api.World, domain);
            }
        }

        public ItemStack? InputStack => InputJson?.ResolvedItemstack?.Clone();
    }

    public class RecycleOutput
    {
        [JsonProperty("item")]
        public JsonItemStack? ItemStack { get; set; }

        [JsonProperty("items")]
        public List<RecycleOutput>? ItemGroup { get; set; }

        [JsonProperty("group")]
        public bool IsGroup { get; set; } = false;

        [JsonProperty("chance")]
        public float Chance { get; set; } = 1f;  // 1.0 = 100% chance

        [JsonIgnore]
        public JsonItemStack? ResolvedItem => IsGroup ? null : ItemStack;

        public RecycleOutput? WeightedRandomOrFirst(ICoreAPI api)
        {
            if (ItemGroup != null && ItemGroup.Count > 0)
            {
                float totalWeight = ItemGroup.Sum(o => o.Chance);
                float random = (float)api.World.Rand.NextDouble() * totalWeight;

                float accum = 0f;
                foreach (RecycleOutput option in ItemGroup)
                {
                    accum += option.Chance;
                    if (accum >= random)
                    {
                        return option;
                    }
                }
            }

            return ItemGroup[0];
        }

        public ItemStack? GetResolvedStack()
        {
            return ItemStack?.ResolvedItemstack?.Clone();
        }
    }
}
