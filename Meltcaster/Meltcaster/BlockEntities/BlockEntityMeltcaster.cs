using Meltcaster.Inventory;
using Meltcaster.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Meltcaster.Gui;

namespace Meltcaster.BlockEntities
{
    internal class BlockEntityMeltcaster : BlockEntityOpenableContainer
    {
        private MeltcasterConfig? Config => MeltcasterModSystem.Config;

        private string? currentInputCode = null;
        private MeltcastOutput? selectedOutput = null;
        private int itemsRemainingForGroup = 0;

        internal InventoryMeltcasting inventory;

        // Temperature before the half second tick
        public float prevFurnaceTemperature = 20;
        // Current temperature of the furnace
        public float furnaceTemperature = 20;
        // Current meltcast recipe temperature
        public float currentRecipeTemperature = 20;
        // Maximum temperature that can be reached with the currently used fuel
        public int maxTemperature;
        // For how long the ore has been cooking
        public float inputStackCookingTime;
        // How much of the current fuel is consumed
        public float fuelBurnTime;
        // How much fuel is available
        public float maxFuelBurnTime;
        // How much smoke the current fuel burns?
        public float smokeLevel;
        // If true, then the fire pit is currently hot enough to ignite fuel
        public bool canIgniteFuel;

        public float cachedFuel;

        public double extinguishedTotalHours;

        public float SpeedModifier
        {
            get
            {
                return Block.Attributes?["speedModifier"]?.AsFloat(1f) ?? 1f;
            }
        }

        GuiDialogBlockEntityMeltcaster clientDialog;
        bool clientSidePrevBurning;

        bool shouldRedraw;

        public bool IsHot => IsBurning;
        public float emptyFirepitBurnTimeMulBonus = 4f;

        private BlockEntityAnimationUtil animUtil
        {
            get { return GetBehavior<BEBehaviorAnimatable>()?.animUtil; }
        }

        #region Config

        public virtual bool BurnsAllFuell
        {
            get { return true; }
        }
        
        public virtual float HeatModifier
        {
            get { return 1f; }
        }
        
        public virtual float BurnDurationModifier
        {
            get { return 1f; }
        }

        // Resting temperature
        public virtual int enviromentTemperature()
        {
            return 20;
        }

        // seconds it requires to melt the ore once beyond melting point
        public virtual float maxCookingTime()
        {
            var MeltcastProps = GetMeltcastProps(inputSlot.Itemstack);
            return (inputSlot.Itemstack == null || MeltcastProps == null) ? 30f : MeltcastProps.MeltcastTime / SpeedModifier;
        }

        public override string InventoryClassName
        {
            get { return "Meltcaster"; }
        }

        public virtual string DialogTitle
        {
            get { return Lang.Get("Meltcaster"); }
        }

        public override InventoryBase Inventory
        {
            get { return inventory; }
        }

        #endregion

        public BlockEntityMeltcaster()
        {
            inventory = new InventoryMeltcasting(null, null);
            inventory.SlotModified += OnSlotModified;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            inventory.Pos = Pos;
            inventory.LateInitialize("smelting-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);

            inventory.OnInventoryClosed += OnInvClosed;
            inventory.OnInventoryOpened += OnInvOpened;

            RegisterGameTickListener(OnBurnTick, 100);
            RegisterGameTickListener(On500msTick, 500);

            if (api.World.Side == EnumAppSide.Client)
            {
                animUtil.InitializeAnimator("Meltcaster:Meltcaster");
            }

            Api.Logger.Debug($"[Meltcaster] Speed modifier for {Block.Code}: {SpeedModifier}");
        }

        private void OnSlotModified(int slotid)
        {
            Block = Api.World.BlockAccessor.GetBlock(Pos);

            MarkDirty(Api.Side == EnumAppSide.Server); // Save useless triple-remesh by only letting the server decide when to redraw
            shouldRedraw = true;

            if (Api is ICoreClientAPI && clientDialog != null)
            {
                SetDialogValues(clientDialog.Attributes);
            }

            Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
        }

        protected virtual void OnInvOpened(IPlayer player)
        {
            //if (Api.Side == EnumAppSide.Client)
            //{
            //    OpenDoor();
            //}

            OpenDoor();
        }

        protected virtual void OnInvClosed(IPlayer player)
        {
            if (LidOpenEntityId.Count == 0)
            {
                CloseDoor();
            }

            // This is already handled elsewhere and also causes a stackoverflowexception, but seems needed somehow?
            var inv = invDialog;
            invDialog = null; // Weird handling because to prevent endless recursion
            if (inv?.IsOpened() == true) inv?.TryClose();
            inv?.Dispose();
        }

        public void CloseDoor()
        {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("lidopen") == true)
            {
                animUtil?.StopAnimation("lidopen");
            }
        }
        public void OpenDoor()
        {
            if (animUtil?.activeAnimationsByAnimCode.ContainsKey("lidopen") == false)
            {
                animUtil?.StartAnimation(new AnimationMetaData()
                {
                    Animation = "lidopen",
                    Code = "lidopen",
                    AnimationSpeed = 1.8f,
                    EaseOutSpeed = 10,
                    EaseInSpeed = 10
                });
            }
        }

        public bool IsSmoldering => canIgniteFuel;

        public bool IsBurning
        {
            get { return fuelBurnTime > 0; }
        }

        // Sync to client every 500ms
        private void On500msTick(float dt)
        {
            if (Api is ICoreServerAPI && (IsBurning || prevFurnaceTemperature != furnaceTemperature))
            {
                MarkDirty();
            }

            prevFurnaceTemperature = furnaceTemperature;
        }

        private void OnBurnTick(float dt)
        {
            // Only tick on the server and merely sync to client
            if (Api is ICoreClientAPI)
            {
                return;
            }

            // Use up fuel
            if (fuelBurnTime > 0)
            {
                bool lowFuelConsumption = Math.Abs(furnaceTemperature - maxTemperature) < 50 && inputSlot.Empty;

                fuelBurnTime -= dt / (lowFuelConsumption ? emptyFirepitBurnTimeMulBonus : 1);

                if (fuelBurnTime <= 0)
                {
                    fuelBurnTime = 0;
                    maxFuelBurnTime = 0;
                    if (!CanBurn()) // check avoids light flicker when a piece of fuel is consumed and more is available
                    {
                        SetBlockState("extinct");
                        extinguishedTotalHours = Api.World.Calendar.TotalHours;
                    }
                }
            }

            // Too cold to ignite fuel after 2 hours
            if (!IsBurning && Block.Variant["burnstate"] == "extinct" && Api.World.Calendar.TotalHours - extinguishedTotalHours > 2)
            {
                canIgniteFuel = false;
                SetBlockState("cold");
            }

            // Furnace is burning: Heat furnace
            if (IsBurning)
            {
                furnaceTemperature = ChangeTemperature(furnaceTemperature, maxTemperature, dt);
            }

            if (GetMeltcastProps(inputSlot.Itemstack)?.MeltcastTemp > 0)
            {
                HeatInput(dt);
            }
            else
            {
                inputStackCookingTime = 0;
            }

            TryHeatAllOutputs(dt);

            // Finished smelting? Turn to smelted item
            if (CanMeltcastInput() && inputStackCookingTime > maxCookingTime())
            {
                MeltcastItems();
            }

            // Furnace is not burning and can burn: Ignite the fuel
            if (!IsBurning && canIgniteFuel && CanBurn())
            {
                IgniteFuel();
            }

            // Furnace is not burning: Cool down furnace and ore also turn of fire
            if (!IsBurning)
            {
                furnaceTemperature = ChangeTemperature(furnaceTemperature, enviromentTemperature(), dt);
            }
        }

        public EnumIgniteState GetIgnitableState(float secondsIgniting)
        {
            if (fuelSlot.Empty) return EnumIgniteState.NotIgnitablePreventDefault;
            if (IsBurning) return EnumIgniteState.NotIgnitablePreventDefault;

            return secondsIgniting > 3 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }

        public float ChangeTemperature(float fromTemp, float toTemp, float dt)
        {
            float diff = Math.Abs(fromTemp - toTemp);

            dt = dt + dt * (diff / 28);


            if (diff < dt)
            {
                return toTemp;
            }

            if (fromTemp > toTemp)
            {
                dt = -dt;
            }

            if (Math.Abs(fromTemp - toTemp) < 1)
            {
                return toTemp;
            }

            return fromTemp + dt;
        }

        private bool CanBurn()
        {
            CombustibleProperties fuelCopts = fuelCombustibleOpts;
            if (fuelCopts == null) return false;

            return BurnsAllFuell && fuelCopts.BurnTemperature * HeatModifier > 0;
        }

        public void HeatInput(float dt)
        {
            MeltcastRecipe? inputMeltcastProps = GetMeltcastProps(inputSlot.Itemstack);
            float oldTemp = InputStackTemp;
            float nowTemp = oldTemp;
            float MeltcastPoint = inputMeltcastProps == null ? 10000 : inputMeltcastProps.MeltcastTemp;

            // Only Heat ore. Cooling happens already in the itemstack
            if (oldTemp < furnaceTemperature)
            {
                float f = (1 + GameMath.Clamp((furnaceTemperature - oldTemp) / 30, 0, 1.6f)) * dt;
                if (nowTemp >= MeltcastPoint) f /= 11;

                float newTemp = ChangeTemperature(oldTemp, furnaceTemperature, f);
                int maxTemp = inputMeltcastProps == null ? 0 : maxTemperature;
                if (maxTemp > 0)
                {
                    newTemp = Math.Min(maxTemp, newTemp);
                }

                if (oldTemp != newTemp)
                {
                    InputStackTemp = newTemp;
                    nowTemp = newTemp;
                }
            }

            // Begin smelting when hot enough
            if (nowTemp >= MeltcastPoint)
            {
                float diff = nowTemp / MeltcastPoint;
                inputStackCookingTime += GameMath.Clamp((int)(diff), 1, 30) * dt;
            }
            else
            {
                if (inputStackCookingTime > 0) inputStackCookingTime--;
            }
        }

        public void TryHeatAllOutputs(float dt)
        {
            List<ItemStack> outputStacks = new List<ItemStack> { outputStackOne, outputStackTwo, outputStackThree, outputStackFour };

            foreach(var stack in outputStacks)
            {
                if (canHeatOutput(stack))
                {
                    float oldTemp = GetTemp(stack);

                    if (oldTemp < currentRecipeTemperature)
                    {
                        float newTemp = ChangeTemperature(oldTemp, furnaceTemperature, 2 * dt);
                        int maxTemp = Math.Max(stack.Collectible.CombustibleProps == null ? 0 : stack.Collectible.CombustibleProps.MaxTemperature, stack.ItemAttributes?["maxTemperature"] == null ? 0 : stack.ItemAttributes["maxTemperature"].AsInt(0));
                        if (maxTemp > 0)
                        {
                            newTemp = Math.Min(maxTemp, newTemp);
                        }

                        if (oldTemp != newTemp)
                        {
                            SetTemp(stack, newTemp);
                        }
                    }
                }
            }
        }

        public void CoolNow(float amountRel)
        {
            Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.5, null, false, 16);

            fuelBurnTime -= (float)amountRel / 10f;

            if (Api.World.Rand.NextDouble() < amountRel / 5f || fuelBurnTime <= 0)
            {
                SetBlockState("cold");
                extinguishedTotalHours = -99;
                canIgniteFuel = false;
                fuelBurnTime = 0;
                maxFuelBurnTime = 0;
            }

            MarkDirty(true);
        }

        public float InputStackTemp
        {
            get
            {
                return GetTemp(inputStack);
            }
            set
            {
                SetTemp(inputStack, value);
            }
        }

        float GetTemp(ItemStack stack)
        {
            if (stack == null) return enviromentTemperature();

            return stack.Collectible.GetTemperature(Api.World, stack);
        }

        void SetTemp(ItemStack stack, float value)
        {
            if (stack == null) return;
            stack.Collectible.SetTemperature(Api.World, stack, value);
        }

        public void IgniteFuel()
        {
            IgniteWithFuel(fuelStack);

            fuelStack.StackSize -= 1;

            if (fuelStack.StackSize <= 0)
            {
                fuelStack = null;
            }            
        }

        public void IgniteWithFuel(IItemStack stack)
        {
            CombustibleProperties fuelCopts = stack.Collectible.CombustibleProps;

            maxFuelBurnTime = fuelBurnTime = fuelCopts.BurnDuration * BurnDurationModifier;
            maxTemperature = (int)(fuelCopts.BurnTemperature * HeatModifier);
            smokeLevel = fuelCopts.SmokeLevel;
            SetBlockState("lit");
            MarkDirty(true);
        }

        public void SetBlockState(string state)
        {
            AssetLocation loc = Block.CodeWithVariant("burnstate", state);
            Block block = Api.World.GetBlock(loc);
            if (block == null) return;

            Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
            Block = block;
        }

        public bool CanMeltcastInput()
        {
            if (inputStack == null) return false;

            if (inputStack.Collectible.OnSmeltAttempt(inventory)) MarkDirty(true);

            return CanMeltcast(Api.World, inputSlot.Itemstack);
        }

        public void MeltcastItems()
        {
            DoMeltcast(Api.World, inputSlot, outputSlots);
            InputStackTemp = enviromentTemperature();
            inputStackCookingTime = 0;
            MarkDirty(true);
            inputSlot.MarkDirty();
        }

        public bool CanMeltcast(IWorldAccessor world, ItemStack inputStack)
        {
            return GetMeltcastProps(inputStack) != null;
        }

        public MeltcastRecipe? GetMeltcastProps(ItemStack inputStack)
        {
            if (inputStack?.Collectible is null || Config?.MeltcastRecipeByCode is null) return null;

            string inputCode = inputStack.Collectible.Code.ToString();
            
            return Config.MeltcastRecipeByCode?.TryGetValue(inputCode);
        }
        
        public bool canHeatOutput(ItemStack outputStack)
        {
            return outputStack?.Collectible.CombustibleProps != null;
        }

        public virtual void DoMeltcast(IWorldAccessor world, ItemSlot inputSlot, ItemSlot[] outputSlots)
        {
            // 1. Check if the item is in the Meltcast list
            if (!CanMeltcast(world, inputSlot.Itemstack)) return;

            // 2. Get the item's Meltcast properties
            MeltcastRecipe? recipe = GetMeltcastProps(inputSlot.Itemstack);
            if (recipe == null || recipe.Outputs == null) return;

            ICoreAPI api = world.Api;

            currentRecipeTemperature = recipe.MeltcastTemp;

            // 3. For each possible output, roll for chance and add to output slots
            foreach (var output in recipe.Outputs)
            {
                if (api.World.Rand.NextDouble() >= output.Chance) continue;

                ItemStack? stackToAdd = null;

                if (output.IsGroup)
                {
                    if (selectedOutput == null || itemsRemainingForGroup <= 0)
                    {
                        selectedOutput = output.WeightedRandomOrFirst(api);
                        itemsRemainingForGroup = output.GroupRollInterval;
                    }

                    stackToAdd = selectedOutput?.GetResolvedStack()?.Clone();
                }
                else
                {
                    stackToAdd = output.GetResolvedStack()?.Clone();
                }

                if (stackToAdd == null) continue;

                SetTemp(stackToAdd, recipe.MeltcastTemp);

                // Try to merge or insert into output slots
                bool placed = false;
                foreach (var slot in outputSlots)
                {
                    if (slot.Empty)
                    {
                        slot.Itemstack = stackToAdd.Clone();
                        slot.MarkDirty();
                        placed = true;
                        break;
                    }

                    if (slot.Itemstack.Collectible.Equals(stackToAdd.Collectible) &&
                        slot.Itemstack.StackSize + stackToAdd.StackSize <= slot.Itemstack.Collectible.MaxStackSize)
                    {
                        slot.Itemstack.StackSize += stackToAdd.StackSize;
                        slot.MarkDirty();
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    // Try to place in block below if container
                    BlockPos belowPos = inputSlot.Inventory.Pos.DownCopy();
                    BlockEntity belowBE = Api.World.BlockAccessor.GetBlockEntity(belowPos);

                    if (belowBE is IBlockEntityContainer containerBE)
                    {
                        foreach (var slot in containerBE.Inventory)
                        {
                            if (slot.Empty)
                            {
                                slot.Itemstack = stackToAdd.Clone();
                                slot.MarkDirty();
                                placed = true;
                                break;
                            }
                            if (slot.Itemstack.Collectible.Equals(stackToAdd.Collectible) &&
                                slot.Itemstack.StackSize + stackToAdd.StackSize <= slot.Itemstack.Collectible.MaxStackSize)
                            {
                                slot.Itemstack.StackSize += stackToAdd.StackSize;
                                slot.MarkDirty();
                                placed = true;
                                break;
                            }
                        }

                        if (!placed)
                        {
                            // If none of the output slots can accept the item, drop it instead
                            world.SpawnItemEntity(stackToAdd, this.Pos.ToVec3d().Add(new Vec3d(0.5, -0.2, 0.5)));
                        }
                    }
                }
            }

            // 4. Decrease input quantity by 1
            inputSlot.TakeOut(1);
            inputSlot.MarkDirty();

            // 5.
            itemsRemainingForGroup--;
        }

        #region Events

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                toggleInventoryDialogClient(byPlayer, () => {
                    SyncedTreeAttribute dtree = new SyncedTreeAttribute();
                    SetDialogValues(dtree);
                    clientDialog = new GuiDialogBlockEntityMeltcaster(DialogTitle, Inventory, Pos, dtree, Api as ICoreClientAPI);
                    return clientDialog;
                });
            }

            return true;
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                (Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
                invDialog?.TryClose();
                invDialog?.Dispose();
                invDialog = null;
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            if (Api != null)
            {
                Inventory.AfterBlocksLoaded(Api.World);
            }

            furnaceTemperature = tree.GetFloat("furnaceTemperature");
            maxTemperature = tree.GetInt("maxTemperature");
            inputStackCookingTime = tree.GetFloat("oreCookingTime");
            fuelBurnTime = tree.GetFloat("fuelBurnTime");
            maxFuelBurnTime = tree.GetFloat("maxFuelBurnTime");
            extinguishedTotalHours = tree.GetDouble("extinguishedTotalHours");
            canIgniteFuel = tree.GetBool("canIgniteFuel", true);
            cachedFuel = tree.GetFloat("cachedFuel", 0);

            if (Api?.Side == EnumAppSide.Client)
            {
                if (clientDialog != null) SetDialogValues(clientDialog.Attributes);
            }


            if (Api?.Side == EnumAppSide.Client && (clientSidePrevBurning != IsBurning || shouldRedraw))
            {
                GetBehavior<BEBehaviorFirepitAmbient>()?.ToggleAmbientSounds(IsBurning);
                clientSidePrevBurning = IsBurning;
                MarkDirty(true);
                shouldRedraw = false;
            }
        }

        void SetDialogValues(ITreeAttribute dialogTree)
        {
            dialogTree.SetFloat("furnaceTemperature", furnaceTemperature);

            dialogTree.SetInt("maxTemperature", maxTemperature);
            dialogTree.SetFloat("oreCookingTime", inputStackCookingTime);
            dialogTree.SetFloat("maxFuelBurnTime", maxFuelBurnTime);
            dialogTree.SetFloat("fuelBurnTime", fuelBurnTime);

            if (inputSlot.Itemstack != null)
            {
                dialogTree.SetFloat("oreTemperature", InputStackTemp);

                if (Config == null || Config.MeltcastRecipeByCode == null)
                {
                    Api.Logger.Warning("[Meltcaster] Config not ready when calling SetDialogValues.");
                    return;
                }

                MeltcastRecipe? recipe = Config?.MeltcastRecipeByCode?.TryGetValue(inputStack.Collectible.Code.ToString());
                
                if (recipe != null)
                {
                    float meltingDuration = recipe.MeltcastTime / SpeedModifier;
                    dialogTree.SetFloat("maxOreCookingTime", meltingDuration);
                }
                else
                {
                    dialogTree.RemoveAttribute("maxOreCookingTime");
                }
            }
            else
            {
                dialogTree.RemoveAttribute("oreTemperature");
            }

            if (Config.ShowRecipeInDialog)
            {
                dialogTree.SetString("outputText", inventory.GetOutputText());
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;

            tree.SetFloat("furnaceTemperature", furnaceTemperature);
            tree.SetInt("maxTemperature", maxTemperature);
            tree.SetFloat("oreCookingTime", inputStackCookingTime);
            tree.SetFloat("fuelBurnTime", fuelBurnTime);
            tree.SetFloat("maxFuelBurnTime", maxFuelBurnTime);
            tree.SetDouble("extinguishedTotalHours", extinguishedTotalHours);
            tree.SetBool("canIgniteFuel", canIgniteFuel);
            tree.SetFloat("cachedFuel", cachedFuel);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if (clientDialog != null)
            {
                clientDialog.TryClose();
                clientDialog?.Dispose();
                clientDialog = null;
            }
        }

        #endregion

        #region Helper getters

        public ItemSlot fuelSlot
        {
            get { return inventory[0]; }
        }

        public ItemSlot inputSlot
        {
            get { return inventory[1]; }
        }

        public ItemSlot[] outputSlots
        {
            get { return inventory.OutputSlots; }
        }

        public ItemStack fuelStack
        {
            get { return inventory[0].Itemstack; }
            set { inventory[0].Itemstack = value; inventory[0].MarkDirty(); }
        }

        public ItemStack inputStack
        {
            get { return inventory[1].Itemstack; }
            set { inventory[1].Itemstack = value; inventory[1].MarkDirty(); }
        }

        public ItemStack outputStackOne
        {
            get { return inventory[2].Itemstack; }
            set { inventory[2].Itemstack = value; inventory[2].MarkDirty(); }
        }

        public ItemStack outputStackTwo
        {
            get { return inventory[3].Itemstack; }
            set { inventory[3].Itemstack = value; inventory[3].MarkDirty(); }
        }

        public ItemStack outputStackThree
        {
            get { return inventory[4].Itemstack; }
            set { inventory[4].Itemstack = value; inventory[4].MarkDirty(); }
        }

        public ItemStack outputStackFour
        {
            get { return inventory[5].Itemstack; }
            set { inventory[5].Itemstack = value; inventory[5].MarkDirty(); }
        }

        public CombustibleProperties fuelCombustibleOpts
        {
            get { return getCombustibleOpts(0); }
        }

        public CombustibleProperties getCombustibleOpts(int slotid)
        {
            ItemSlot slot = inventory[slotid];
            if (slot.Itemstack == null) return null;
            return slot.Itemstack.Collectible.CombustibleProps;
        }

        #endregion

        public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
        {
            foreach (var slot in Inventory)
            {
                if (slot.Itemstack == null) continue;

                if (slot.Itemstack.Class == EnumItemClass.Item)
                {
                    itemIdMapping[slot.Itemstack.Item.Id] = slot.Itemstack.Item.Code;
                }
                else
                {
                    blockIdMapping[slot.Itemstack.Block.BlockId] = slot.Itemstack.Block.Code;
                }

                slot.Itemstack.Collectible.OnStoreCollectibleMappings(Api.World, slot, blockIdMapping, itemIdMapping);
            }
        }

        public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
        {
            base.OnLoadCollectibleMappings(worldForResolve, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
        }

        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
        {
            return IsBurning ? 10 : (IsSmoldering ? 0.25f : 0);
        }
    }
}
