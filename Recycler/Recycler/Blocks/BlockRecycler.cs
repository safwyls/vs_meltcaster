using Recycler.BlockEntities;
using Recycler.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Recycler.Blocks
{
    internal class BlockRecycler : Block, IIgnitable, ISmokeEmitter
    {
        private RecyclerConfig? Config = RecyclerModSystem.Config;

        public bool IsExtinct;

        AdvancedParticleProperties[] ringParticles;
        Vec3f[] basePos;
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            IsExtinct = LastCodePart() != "lit";

            if (!IsExtinct && api.Side == EnumAppSide.Client)
            {
                ringParticles = new AdvancedParticleProperties[ParticleProperties.Length * 4];
                basePos = new Vec3f[ringParticles.Length];

                Cuboidf[] spawnBoxes = new Cuboidf[]
                {
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.125f, x2: 0.3125f, y2: 0.5f, z2: 0.875f),
                    new Cuboidf(x1: 0.7125f, y1: 0, z1: 0.125f, x2: 0.875f, y2: 0.5f, z2: 0.875f),
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.125f, x2: 0.875f, y2: 0.5f, z2: 0.3125f),
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.7125f, x2: 0.875f, y2: 0.5f, z2: 0.875f)
                };

                for (int i = 0; i < ParticleProperties.Length; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        AdvancedParticleProperties props = ParticleProperties[i].Clone();

                        Cuboidf box = spawnBoxes[j];
                        basePos[i * 4 + j] = new Vec3f(0, 0, 0);

                        props.PosOffset[0].avg = box.MidX;
                        props.PosOffset[0].var = box.Width / 2;

                        props.PosOffset[1].avg = 0.1f;
                        props.PosOffset[1].var = 0.05f;

                        props.PosOffset[2].avg = box.MidZ;
                        props.PosOffset[2].var = box.Length / 2;

                        props.Quantity.avg /= 4f;
                        props.Quantity.var /= 4f;

                        ringParticles[i * 4 + j] = props;
                    }
                }
            }


            interactions = ObjectCacheUtil.GetOrCreate(api, "recyclerInteractions-lit", () =>
            {
                List<ItemStack> canIgniteStacks = BlockBehaviorCanIgnite.CanIgniteStacks(api, true);

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-recycler-open",
                        MouseButton = EnumMouseButton.Right,
                        ShouldApply = (WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection) =>
                        {
                            return IsExtinct == true;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-recycler-ignite",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "shift",
                        Itemstacks = canIgniteStacks.ToArray(),
                        GetMatchingStacks = (wi, bs, es) => {
                            BlockEntityRecycler bef = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityRecycler;
                            if (bef?.fuelSlot != null && !bef.fuelSlot.Empty && !bef.IsBurning)
                            {
                                return wi.Itemstacks;
                            }
                            return null;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-recycler-refuel",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "shift"
                    }
                };
            });
        }


        public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
        {
            if (world.Rand.NextDouble() < 0.05 && GetBlockEntity<BlockEntityRecycler>(pos)?.IsBurning == true)
            {
                entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Block, SourceBlock = this, Type = EnumDamageType.Fire, SourcePos = pos.ToVec3d() }, 0.5f);
            }

            base.OnEntityInside(world, entity, pos);
        }


        EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
        {
            BlockEntityRecycler bef = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityRecycler;
            if (bef.IsBurning) return secondsIgniting > 2 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
            return EnumIgniteState.NotIgnitable;
        }
        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            BlockEntityRecycler bef = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityRecycler;
            if (bef == null) return EnumIgniteState.NotIgnitable;
            return bef.GetIgnitableState(secondsIgniting);
        }

        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            BlockEntityRecycler bef = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityRecycler;
            if (bef != null && !bef.canIgniteFuel)
            {
                bef.canIgniteFuel = true;
                bef.extinguishedTotalHours = api.World.Calendar.TotalHours;
            }

            handling = EnumHandling.PreventDefault;
        }


        public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
        {
            bool val = base.ShouldReceiveClientParticleTicks(world, player, pos, out _);
            isWindAffected = true;

            return val;
        }

        public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
        {
            if (IsExtinct)
            {
                base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
                return;
            }

            if (manager.BlockAccess.GetBlockEntity(pos) is BlockEntityRecycler bef)
            {
                for (int i = 0; i < ringParticles.Length; i++)
                {
                    AdvancedParticleProperties bps = ringParticles[i];
                    bps.WindAffectednesAtPos = windAffectednessAtPos;
                    bps.basePos.X = pos.X + basePos[i].X;
                    bps.basePos.Y = pos.Y + basePos[i].Y;
                    bps.basePos.Z = pos.Z + basePos[i].Z;

                    manager.Spawn(bps);
                }

                return;
            }

            base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
        }

        public RecycleRecipe? GetRecycleProps(ItemStack inputStack)
        {
            if (inputStack?.Collectible == null || Config?.RecycleRecipes == null) return null;

            string inputCode = inputStack.Collectible.Code.ToString();

            return Config.RecycleRecipes.FirstOrDefault(r => r.InputJson?.Code.ToString() == inputCode);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }

            ItemStack? stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;


            if (world.BlockAccessor.GetBlockEntity(blockSel?.Position) is BlockEntityRecycler bef)
            {
                if (stack?.Block != null && stack.Block.HasBehavior<BlockBehaviorCanIgnite>() && bef.GetIgnitableState(0) == EnumIgniteState.Ignitable)
                {
                    return false;
                }

                if (stack != null && byPlayer.Entity.Controls.ShiftKey)
                {
                    RecycleRecipe? recipe = GetRecycleProps(stack);
                    if (stack != null && recipe != null && recipe.RecycleTemp > 0)
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, 1);
                        byPlayer.InventoryManager.ActiveHotbarSlot?.TryPutInto(bef.inputSlot, ref op);
                        if (op.MovedQuantity > 0)
                        {
                            (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                            return true;
                        }
                    }

                    if (stack?.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.BurnTemperature > 0)
                    {
                        ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, 1);
                        byPlayer.InventoryManager.ActiveHotbarSlot?.TryPutInto(bef.fuelSlot, ref op);
                        if (op.MovedQuantity > 0)
                        {
                            (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

                            var loc = stack.ItemAttributes?["placeSound"].Exists == true ? AssetLocation.Create(stack.ItemAttributes["placeSound"].AsString(), stack.Collectible.Code.Domain) : null;

                            if (loc != null)
                            {
                                api.World.PlaySoundAt(loc.WithPathPrefixOnce("sounds/"), blockSel.Position.X, blockSel.Position.InternalY, blockSel.Position.Z, byPlayer, 0.88f + (float)api.World.Rand.NextDouble() * 0.24f, 16);
                            }

                            return true;
                        }
                    }
                }

                if (GetRecycleProps(stack) != null)
                {
                    if (!bef.inputSlot.Empty || byPlayer.InventoryManager.ActiveHotbarSlot?.TryPutInto(api.World, bef.inputSlot, 1) == 0)
                    {
                        if (blockSel != null)
                        {
                            bef.OnPlayerRightClick(byPlayer, blockSel);
                        }
                    }

                    return true;
                }
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public static bool IsFirewoodPile(IWorldAccessor world, BlockPos pos)
        {
            var beg = world.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(pos);
            return beg != null && beg.Inventory[0]?.Itemstack?.Collectible is ItemFirewood;
        }

        public static int GetFireWoodQuanity(IWorldAccessor world, BlockPos pos)
        {
            var beg = world.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(pos);
            return beg?.Inventory[0]?.StackSize ?? 0;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        public bool EmitsSmoke(BlockPos pos)
        {
            var berecycler = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityRecycler;
            return berecycler?.IsBurning == true;
        }
    }
}