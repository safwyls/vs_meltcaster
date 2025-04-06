using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace Meltcaster.Config
{
    public class MeltcasterConfig
    {        
        [JsonProperty("meltcastRecipes")]
        public List<MeltcastRecipe>? MeltcastRecipes { get; set; }
        
        [JsonProperty("meltcastGroups")]
        public Dictionary<string, List<MeltcastOutput>>? MeltcastGroups { get; set; }
        
        [JsonProperty("commonOutputs")]
        public Dictionary<string, List<MeltcastOutput>>? CommonOutputs { get; set; }

        [JsonIgnore]
        public Dictionary<string, MeltcastRecipe>? MeltcastRecipeByCode { get; private set; }

        public void ResolveAll(ICoreAPI api)
        {
            // Resolve all groups
            foreach (var group in MeltcastGroups)
            {
                foreach (var output in group.Value)
                {
                    output.ParentConfig = this;
                    output.Resolve(api);
                }
            }

            // Resolve all recipes
            foreach (var recipe in MeltcastRecipes)
            {
                // Resolve the input
                recipe.Input.Resolve(api);

                // Resolve each output
                foreach (var output in recipe.Output)
                {
                    if (output.IsGroup && output.Group != null)
                    {
                        foreach (var groupOutput in output.Group)
                        {
                            groupOutput.ParentConfig = this;
                            groupOutput.Resolve(api);
                        }
                    }
                    else
                    {
                        output.ParentConfig = this;
                        output.Resolve(api);
                    }
                }

                var codeStr = recipe.Input.ItemCode.ToString();
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
                // Default recipes
                MeltcastRecipes = new List<MeltcastRecipe>
                {
                    new()
                    {
                        Input = new MeltcastInput { ItemCode = "game:metal-scraps", Type= "block", Quantity = 1 },
                        MeltcastTemp = 1200,
                        MeltcastTime = 60,
                        Output = new List<MeltcastOutput>
                        {
                            new() { ItemCode = "metal-bits", Type="group", Quantity = 1, Chance = 1f },
                            new() { ItemCode = "game:gear-rusty", Type="item", Quantity = 1, Chance = 0.05f },
                            new() { ItemCode = "game:gear-temporal", Type="item", Quantity = 1, Chance = 0.01f },
                        }
                    },
                    new()
                    {
                        Input = new MeltcastInput { ItemCode = "game:metal-parts", Type="block", Quantity = 1 },
                        MeltcastTemp = 1200,
                        MeltcastTime = 60,
                        Output = new List<MeltcastOutput>
                        {
                            new() { ItemCode = "jonas-metal-bits", Type="group", Quantity = 1, Chance = 1f },
                            new() { ItemCode = "game:gear-rusty", Type="item", Quantity = 1, Chance = 0.01f },
                            new() { ItemCode = "game:gear-temporal", Type="item", Quantity = 1, Chance = 0.02f },
                        }
                    },
                    new()
                    {
                        Input = new MeltcastInput { ItemCode = "game:jonasframes-gearbox01", Type="item", Quantity = 1 },
                        MeltcastTemp = 1200,
                        MeltcastTime = 60,
                        Output = new List<MeltcastOutput>
                        {
                            new() { ItemCode = "game:metal-parts", Type="block", Quantity = 1 },
                            new() { ItemCode = "game:gear-temporal", Type="item", Quantity = 1 },
                            new() { ItemCode = "game:rod-cupronickel", Type="item", Quantity = 1 },
                        }
                    },
                    new()
                    {
                        Input = new MeltcastInput { ItemCode = "game:jonasframes-gearbox02", Type="item", Quantity = 1 },
                        MeltcastTemp = 1200,
                        MeltcastTime = 60,
                        Output = new List<MeltcastOutput>
                        {
                            new() { ItemCode = "meltcaster:jonas-nails-and-strips", Type="group", Quantity = 1, Chance = 1f },
                            new() { ItemCode = "game:metal-parts", Type="block", Quantity = 1, Chance = 1f },
                            new() { ItemCode = "game:gear-temporal", Type="item", Quantity = 1, Chance = 1f },
                            new() { ItemCode = "game:rod-cupronickel", Type="item", Quantity = 1, Chance = 1f },
                        }
                    },
                    new()
                    {
                        Input = new MeltcastInput { ItemCode = "game:jonasframes-spring01", Type="item", Quantity = 1 },
                        MeltcastTemp = 1200,
                        MeltcastTime = 60,
                        Output = new List<MeltcastOutput>
                        {
                            new() { ItemCode = "meltcaster:jonas-nails-and-strips", Type="group", Quantity = 1, Chance = 1f },
                            new() { ItemCode = "game:metal-parts", Type="block", Quantity = 1, Chance = 1f },
                            new() { ItemCode = "game:gear-temporal", Type="item", Quantity = 1, Chance = 1f },
                            new() { ItemCode = "game:rod-cupronickel", Type="item", Quantity = 1, Chance = 1f },
                        }
                    }
                },
                // Groups of outputs, used for weighted selection groups
                MeltcastGroups = new Dictionary<string, List<MeltcastOutput>>
                {
                    {
                        "metal-bits",
                        new List<MeltcastOutput>
                        {
                            new() { ItemCode = "game:metalbit-copper",          Type="item", Quantity = 1, Chance = 1f    },
                            new() { ItemCode = "game:metalbit-tinbronze",       Type="item", Quantity = 1, Chance = 0.7f  },
                            new() { ItemCode = "game:metalbit-bismuthbronze",   Type="item", Quantity = 1, Chance = 0.6f  },
                            new() { ItemCode = "game:metalbit-blackbronze",     Type="item", Quantity = 1, Chance = 0.5f  },
                            new() { ItemCode = "game:metalbit-iron",            Type="item", Quantity = 1, Chance = 0.5f  },
                            new() { ItemCode = "game:metalbit-steel",           Type="item", Quantity = 1, Chance = 0.4f  },
                            new() { ItemCode = "game:metalbit-nickel",          Type="item", Quantity = 1, Chance = 0.3f  },
                            new() { ItemCode = "game:metalbit-lead",            Type="item", Quantity = 1, Chance = 0.3f  },
                            new() { ItemCode = "game:metalbit-zinc",            Type="item", Quantity = 1, Chance = 0.3f  },
                            new() { ItemCode = "game:metalbit-bismuth",         Type="item", Quantity = 1, Chance = 0.3f  },
                            new() { ItemCode = "game:metalbit-molybdochalkos",  Type="item", Quantity = 1, Chance = 0.3f  },
                            new() { ItemCode = "game:metalbit-cupronickel",     Type="item", Quantity = 1, Chance = 0.2f  },
                            new() { ItemCode = "game:metalbit-brass",           Type="item", Quantity = 1, Chance = 0.2f  },
                            new() { ItemCode = "game:metalbit-chromium",        Type="item", Quantity = 1, Chance = 0.2f  },
                            new() { ItemCode = "game:metalbit-silver",          Type="item", Quantity = 1, Chance = 0.2f  },
                            new() { ItemCode = "game:metalbit-leadsolder",      Type="item", Quantity = 1, Chance = 0.2f  },
                            new() { ItemCode = "game:metalbit-silversolder",    Type="item", Quantity = 1, Chance = 0.1f  },
                            new() { ItemCode = "game:metalbit-meteoriciron",    Type="item", Quantity = 1, Chance = 0.1f  },
                            new() { ItemCode = "game:metalbit-gold",            Type="item", Quantity = 1, Chance = 0.1f  },
                            new() { ItemCode = "game:metalbit-electrum",        Type="item", Quantity = 1, Chance = 0.05f },
                            new() { ItemCode = "game:metalbit-titanium",        Type="item", Quantity = 1, Chance = 0.05f },
                        }
                    },
                    {
                        "common-metal-bits",
                        new List<MeltcastOutput>
                        {
                            new() { ItemCode = "game:metalbit-copper",      Type="item", Quantity = 1, Chance = 1f   },
                            new() { ItemCode = "game:metalbit-tinbronze",   Type="item", Quantity = 1, Chance = 0.8f },
                            new() { ItemCode = "game:metalbit-iron",        Type="item", Quantity = 1, Chance = 0.7f },
                            new() { ItemCode = "game:metalbit-steel",       Type="item", Quantity = 1, Chance = 0.5f }
                        }
                    },
                    {
                        "jonas-metal-bits",
                        new List<MeltcastOutput>
                        {
                            new() { ItemCode = "game:metalbit-cupronickel", Type="item", Quantity = 1, Chance = 1f   },
                            new() { ItemCode = "game:metalbit-iron",        Type="item", Quantity = 1, Chance = 0.7f },
                            new() { ItemCode = "game:metalbit-steel",       Type="item", Quantity = 1, Chance = 0.5f }
                        }
                    },
                    {
                        "jonas-nails-and-strips",
                        new List<MeltcastOutput>
                        {
                            new() { ItemCode="game:metalnailsandstrips-cupronickel",    Type="item", Quantity = 1, Chance = 1f },
                            new() { ItemCode="game:metalnailsandstrips-iron",           Type="item", Quantity = 1, Chance = 1f },
                            new() { ItemCode="game:metalnailsandstrips-steel",          Type="item", Quantity = 1, Chance = 1f }
                        }
                    },
                    {
                        "nails-and-strips",
                        new List<MeltcastOutput>
                        {
                            new() { ItemCode="game:metalnailsandstrips-copper",         Type="item", Quantity = 1, Chance = 1f },
                            new() { ItemCode="game:metalnailsandstrips-tinbronze",      Type="item", Quantity = 1, Chance = 1f },
                            new() { ItemCode="game:metalnailsandstrips-bismuthbronze",  Type="item", Quantity = 1, Chance = 1f },
                            new() { ItemCode="game:metalnailsandstrips-blackbronze",    Type="item", Quantity = 1, Chance = 1f },
                            new() { ItemCode="game:metalnailsandstrips-iron",           Type="item", Quantity = 1, Chance = 1f },
                            new() { ItemCode="game:metalnailsandstrips-steel",          Type="item", Quantity = 1, Chance = 1f }
                        }
                    }
                }
            };
        }
    }

    public class MeltcastRecipe
    {
        public MeltcastInput? Input { get; set; }
        public float MeltcastTemp { get; set; }
        public float MeltcastTime { get; set; }
        public List<MeltcastOutput>? Output { get; set; }
    }

    public class MeltcastInput
    { 
        // Serialized properties
        public required string? ItemCode { get; set; }
        
        public required string Type { get; set; }
        
        public int Quantity { get; set; } = 1;

        // Non-serialized properties
        [JsonIgnore]
        public string? Domain => ItemCode?.Split(':')[0] ?? "meltcaster";

        [JsonIgnore]
        public ItemStack? ResolvedStack { get; set; }

        public void Resolve(ICoreAPI api)
        {
            if (ItemCode == null) return;

            // Try to get the type from the output code
            bool valid = Enum.TryParse(Type, true, out EnumItemClass type);
            if (!valid) return;

            // Build a JsonItemStack from the output code
            JsonItemStack json = new JsonItemStack()
            {
                Type = type,
                Code = ItemCode,
                StackSize = Quantity
            };

            // Resolve
            json.Resolve(api.World, Domain);

            // Clone
            if (json.ResolvedItemstack == null) return;
            ResolvedStack = json.ResolvedItemstack.Clone();
        }
    }

    public class MeltcastOutput
    {
        // Serialized properties
        public required string? ItemCode { get; set; }

        public required string? Type { get; set; }

        public int Quantity { get; set; } = 1;

        public float Chance { get; set; } = 1f;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? GroupRollInterval { get; set; } // How often to reroll the group output. 16 = every 16 items.

        // / Non-serialized properties
        [JsonIgnore]
        public string? Domain => ItemCode?.Split(':')[0] ?? "meltcaster";

        [JsonIgnore]
        public bool IsGroup => Type == "group";

        [JsonIgnore]
        public string? Description => string.Join(" ", ItemCode.Contains(':') ? ItemCode.Split(':')[1].Split('-').Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)) : ItemCode.Split('-').Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)));

        [JsonIgnore]
        public ItemStack? ResolvedStack { get; set; }

        [JsonIgnore]
        public MeltcasterConfig? ParentConfig { get; set; }

        [JsonIgnore]
        public List<MeltcastOutput>? Group
        {
            get
            {
                if (ItemCode == null  || ParentConfig == null) return null;
                if (ParentConfig.MeltcastGroups.TryGetValue(ItemCode, out var group))
                {
                    return group;
                }
                return null;
            }
        }

        /// <summary>
        /// Returns a weighted random output from the group
        /// </summary>
        /// <param name="api"></param>
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

        public void Resolve(ICoreAPI api)
        {
            if (ItemCode == null) return;

            // Try to get the type from the output code
            bool valid = Enum.TryParse(Type, true, out EnumItemClass type);
            if (!valid) return;

            // Build a JsonItemStack from the output code
            JsonItemStack json = new JsonItemStack()
            {
                Type = type,
                Code = ItemCode,
                StackSize = Quantity
            };

            // Resolve
            json.Resolve(api.World, Domain);

            // Clone
            if (json.ResolvedItemstack == null) return;
            ResolvedStack = json.ResolvedItemstack.Clone();
        }
    }
}
