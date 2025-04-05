using Meltcaster.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Meltcaster.Inventory
{
    public class InventoryMeltcasting : InventoryBase, ISlotProvider
    {
        private MeltcasterConfig? Config => MeltcasterModSystem.Config;
        private ItemSlot[] slots;
        private readonly ItemSlot[] outputSlots;

        public BlockPos Pos;

        public ItemSlot[] Slots
        {
            get { return slots; }
        }

        public ItemSlot[] OutputSlots => outputSlots;

        public ItemSlot FuelSlot => slots[0];
        public ItemSlot InputSlot => slots[1];

        public InventoryMeltcasting(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            // slot 0 = fuel
            // slot 1 = input
            // slot 2,3,4,5 = output
            slots = GenEmptySlots(6);
            outputSlots = new ItemSlot[] { slots[2], slots[3], slots[4], slots[5] };
            baseWeight = 4f;
        }

        public InventoryMeltcasting(string className, string instanceID, ICoreAPI api)
            : base(className, instanceID, api)
        {
            slots = GenEmptySlots(6); // 0: fuel, 1: input, 2-5: outputs
            outputSlots = new ItemSlot[] { slots[2], slots[3], slots[4], slots[5] };
            baseWeight = 4f;
        }

        public override int Count => slots.Length;

        public override ItemSlot this[int slotId]
        {
            get => (slotId >= 0 && slotId < Count) ? slots[slotId] : null;
            set => slots[slotId] = value ?? throw new ArgumentNullException(nameof(value));
        }

        protected override ItemSlot NewSlot(int i)
        {
            return new ItemSlotSurvival(this);
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            var modifiedSlots = new List<ItemSlot>();
            slots = SlotsFromTreeAttributes(tree, slots, modifiedSlots);
            foreach (var slot in modifiedSlots) DidModifyItemSlot(slot);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
        }

        public override void OnItemSlotModified(ItemSlot slot)
        {
            base.OnItemSlotModified(slot);
        }

        public override bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot)
        {
            int slotId = GetSlotId(sinkSlot);
            return slotId >= 0 && slotId < Count && base.CanContain(sinkSlot, sourceSlot);
        }

        public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
        {
            skipSlots ??= new List<ItemSlot>();

            // Never shift items into output slots
            skipSlots.AddRange(outputSlots);

            var stack = sourceSlot?.Itemstack;
            if (stack == null) return null;

            // Prefer fuel slot
            if (stack.Collectible?.CombustibleProps?.BurnTemperature > 0)
            {
                var fuelSlot = slots[0];
                if (!skipSlots.Contains(fuelSlot) && fuelSlot.CanHold(sourceSlot) && fuelSlot.CanTakeFrom(sourceSlot))
                {
                    return new WeightedSlot
                    {
                        slot = fuelSlot,
                        weight = 1f
                    };
                }
            }

            // Prefer input slot for meltable items
            if (Config?.MeltcastRecipeByCode?.TryGetValue(stack.Collectible.Code.ToString()) is MeltcastRecipe recipe)
            {
                var inputSlot = slots[1];
                if (!skipSlots.Contains(inputSlot) && inputSlot.CanHold(sourceSlot) && inputSlot.CanTakeFrom(sourceSlot))
                {
                    return new WeightedSlot
                    {
                        slot = inputSlot,
                        weight = 0.9f  // Lower than fuel to avoid conflict
                    };
                }
            }

            return base.GetBestSuitedSlot(sourceSlot, op, skipSlots);
        }

        public string GetOutputText()
        {
            ItemStack inputStack = slots[1].Itemstack;

            if (inputStack == null) return null;

            if (inputStack.Collectible is BlockSmeltingContainer)
            {
                return ((BlockSmeltingContainer)inputStack.Collectible).GetOutputText(Api.World, this, slots[1]);
            }
            if (inputStack.Collectible is BlockCookingContainer)
            {
                return ((BlockCookingContainer)inputStack.Collectible).GetOutputText(Api.World, this, slots[1]);
            }

            List<MeltcastOutput> outputs = Config?.MeltcastRecipeByCode?.TryGetValue(inputStack.Collectible.Code.ToString())?.Outputs;

            if (outputs == null) return null;

            string outputText = "";
            foreach (MeltcastOutput output in outputs)
            {
                if (output == null) continue;
                if (output.IsGroup)
                {
                    outputText += $"\t{output?.GroupDesc} - {output?.Chance * 100}%\n";

                    // Don't show all outputs in group, just show the overall chance and group desc
                    //foreach(var item in output?.ItemGroup)
                    //{
                    //    if (item.ResolvedItem == null) continue;

                    //    ItemStack? outputStack = item?.GetResolvedStack();
                    //    if (outputStack == null) continue;
                    //    outputText += $"\t{outputStack?.StackSize}x {outputStack?.GetName()} - {item?.Chance * 100}%\n";
                    //}
                }
                else
                {
                    if (output.ResolvedItem == null) continue;

                    ItemStack? outputStack = output?.GetResolvedStack();
                    if (outputStack == null) continue;
                    outputText += $"\t{outputStack?.GetName()} - {output?.Chance * 100}%\n";
                }
            }

            return Lang.Get("meltcaster:meltcast-gui-recipeoutput", outputText);
        }

    }
}
