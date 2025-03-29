using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Newtonsoft.Json;
using HarmonyLib;
using System;
using Vintagestory.API.Datastructures;
using ProtoBuf;
using Vintagestory.API;

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
                        output.ItemStack?.Resolve(api.World, domain);
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
                        RecycleTemp = 600,
                        RecycleTime = 5,
                        Outputs = new List<RecycleOutput>
                        {
                            new()
                            {
                                ItemStack = new JsonItemStack
                                {
                                    Code = new AssetLocation("metalbit-copper"),
                                    Type = EnumItemClass.Item,
                                    StackSize = 2
                                },
                                Chance = 1.0f
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

        [JsonProperty("chance")]
        public float Chance { get; set; } = 1f;  // 1.0 = 100% chance

        public ItemStack? GetResolvedStack()
        {
            return ItemStack?.ResolvedItemstack?.Clone();
        }
    }
}
