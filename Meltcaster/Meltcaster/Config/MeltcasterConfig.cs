using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Channels;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Meltcaster.Config
{
    public class MeltcasterConfig
    {
        private static ILogger ModLogger => MeltcasterModSystem.Instance.Logger;
        public required string RecipesPath { get; set; } = Path.Combine("meltcaster", "recipes");
        public required List<MeltcastRecipe> MeltcastRecipes { get; set; }

        [JsonIgnore]
        public Dictionary<AssetLocation, MeltcastRecipe>? MeltcastRecipeByCode { get; private set; }

        private static bool IsRecipeValid(MeltcastRecipe? recipe, string file)
        {
            var fileName = Path.GetFileName(file);
            if (recipe == null) return false;

            // Check input
            if (string.IsNullOrEmpty(recipe.Input?.Code?.ToShortString()))
            {
                ModLogger.Error($"Missing input code: {fileName}");
                return false;
            }

            //Check output
            if (recipe.Output == null || recipe.Output.Count == 0)
            {
                ModLogger.Error($"Empty output: {fileName}");
                return false;
            }
            
            //Check temp and time
            if (recipe.MeltcastTemp <= 0 || recipe.MeltcastTime <= 0)
            {
                ModLogger.Error($"Invalid temp/time: {fileName}");
                return false;
            }
            
            return true;
        }

        // Checks ModConfig/meltcaster/recipes/ for any custom recipes
        public void LoadIndependentRecipes(ICoreAPI api)
        {
            string recipeFolder = Path.Combine(GamePaths.ModConfig, RecipesPath);

            if (Directory.Exists(recipeFolder))
            {
                foreach (string file in Directory.GetFiles(recipeFolder, "*.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        var recipes = JsonConvert.DeserializeObject<MeltcastRecipe[]>(File.ReadAllText(file));
                        if (recipes is null) continue;
                        foreach (var recipe in recipes)
                        {
                            if (IsRecipeValid(recipe, file))
                            {
                                MeltcastRecipes.Add(recipe);
                                ModLogger.Notification($"Loaded recipe: {Path.GetFileName(file)}");
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ModLogger.Error($"Failed to load {file}: {e.Message}");
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(recipeFolder);
                ModLogger.Warning($"Recipe folder did not exist. Created @ {Path.GetDirectoryName(recipeFolder)}");

                var example = api.Assets.Get(new AssetLocation(Path.Combine("meltcaster:config","recipes","example.json")));
                if (example != null)
                {
                    File.WriteAllText(Path.Combine(recipeFolder, "example.json"), example.ToText());
                    ModLogger.Notification($"Created default recipe: {Path.GetFileName(example.Location)}");
                    LoadIndependentRecipes(api);
                }
            }
        }

        public void ResolveAll(IWorldAccessor world, string domain = "meltcaster")
        {
            LoadIndependentRecipes(world.Api);

            foreach (var recipe in MeltcastRecipes)
            {
                recipe?.Input?.Resolve(world, domain);
                if (recipe?.Input.ResolvedItemstack is null)
                {
                    ModLogger.Error($"Failed to resolve input itemstack for recipe input: {recipe?.Input?.Code}");
                }

                if (recipe?.Output == null) return;
                foreach (var output in recipe.Output)
                {
                    output.Resolve(world, domain);
                    if (output.ResolvedItemstack is null && !output.IsGroup)
                    {
                        ModLogger.Error($"Failed to resolve recipe output itemstack: {output.Code}");
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
                RecipesPath = Path.Combine("meltcaster","recipes"),
                MeltcastRecipes = new List<MeltcastRecipe>()
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
                        resolver.Logger.Error($"[meltcaster] Failed to resolve recipe output itemstack for Group: {Code}, output: {output.Code}");
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
