using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Recycler.Inventory
{
    public class InventoryRecycling : InventoryBase, ISlotProvider
    {
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

        public InventoryRecycling(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            // slot 0 = fuel
            // slot 1 = input
            // slot 2,3,4,5 = output
            slots = GenEmptySlots(6);
            outputSlots = new ItemSlot[] { slots[2], slots[3], slots[4], slots[5] };
            baseWeight = 4f;
        }

        public InventoryRecycling(string className, string instanceID, ICoreAPI api)
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
            if (sourceSlot.Itemstack?.Collectible?.CombustibleProps?.BurnTemperature > 0)
            {
                return new WeightedSlot() { slot = slots[0], weight = 3f }; // Prefer fuel slot
            }

            if (sourceSlot.Itemstack?.Collectible?.CombustibleProps?.SmeltedStack != null)
            {
                return new WeightedSlot() { slot = slots[1], weight = 2f }; // Prefer input slot
            }

            return base.GetBestSuitedSlot(sourceSlot, op, skipSlots);
        }
    }
}
