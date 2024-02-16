using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using System.Collections.Generic;
using Vintagestory.API.Util;
using System;

namespace BreadNButter
{
    public class BlockButterChurn : BlockLiquidContainerBase
    {
        public override bool AllowHeldLiquidTransfer => false;

        public override int GetContainerSlotId(BlockPos pos)
        {
            return 1;
        }

        public override int GetContainerSlotId(ItemStack containerStack)
        {
            return 1;
        }

        public int GetChurnHashCode(ItemStack contentStack, ItemStack liquidStack)
        {
            string s = contentStack?.StackSize + "x" + contentStack?.GetHashCode();
            s += liquidStack?.StackSize + "x" + liquidStack?.GetHashCode();
            return s.GetHashCode();
        }
        public override int TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres)
        {
            return base.TryPutLiquid(pos, liquidStack, desiredLitres);
        }

        public override int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
        {
            return base.TryPutLiquid(containerStack, liquidStack, desiredLitres);
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "heldhelp-place",
                    HotKeyCode = "shift",
                    MouseButton = EnumMouseButton.Right,
                    ShouldApply = (wi, bs, es) => {
                        return true;
                    }
                }
            };
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (Attributes?["capacityLitres"].Exists == true)
            {
                capacityLitresFromAttributes = Attributes["capacityLitres"].AsInt(50);
            }


            if (api.Side != EnumAppSide.Client) return;
            ICoreClientAPI capi = api as ICoreClientAPI;

            interactions = ObjectCacheUtil.GetOrCreate(api, "liquidContainerBase", () =>
            {
                List<ItemStack> liquidContainerStacks = new List<ItemStack>();

                foreach (CollectibleObject obj in api.World.Collectibles)
                {
                    if (obj is ILiquidSource || obj is ILiquidSink || obj is BlockWateringCan)
                    {
                        List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                        if (stacks != null) liquidContainerStacks.AddRange(stacks);
                    }
                }

                ItemStack[] lstacks = liquidContainerStacks.ToArray();
                ItemStack[] saltStack = new ItemStack[] { new ItemStack(api.World.GetItem(new AssetLocation("game:salt")), 5) };

                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-bucket-rightclick",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = lstacks,
                        GetMatchingStacks = (wi, bs, ws) =>
                        {
                            ButterChurnBE bechurn = api.World.BlockAccessor.GetBlockEntity(bs.Position) as ButterChurnBE;
                            return bechurn != null ? lstacks : null;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-churn-takebutter",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "shift",
                        Itemstacks = null,
                        ShouldApply = (wi, bs, ws) =>
                        {
                            ButterChurnBE bechurn = api.World.BlockAccessor.GetBlockEntity(bs.Position) as ButterChurnBE;
                            return bechurn?.State == EnumChurnState.Butter;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-churn-churn",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = null,
                        ShouldApply = (wi, bs, ws) =>
                        {
                            ButterChurnBE bechurn = api.World.BlockAccessor.GetBlockEntity(bs.Position) as ButterChurnBE;
                            return bechurn?.State == EnumChurnState.SaltedMilked;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-curdbundle-addsalt",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = saltStack,
                        GetMatchingStacks = (wi, bs, ws) =>
                        {
                            ButterChurnBE bechurn = api.World.BlockAccessor.GetBlockEntity(bs.Position) as ButterChurnBE;
                            return (bechurn?.State == EnumChurnState.Empty || bechurn?.State == EnumChurnState.Milked) ? saltStack : null;
                        }
                    },
                };
            });
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }
            ButterChurnBE bebc=null;
            if (blockSel.Position != null)
                bebc = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as ButterChurnBE;
            ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            bool handled = false;
            if (blockSel != null)
                base.OnBlockInteractStart(world, byPlayer, blockSel);

            if (bebc == null) return false;

            if (bebc.State == EnumChurnState.SaltedMilked) { 
                bebc.StartChurn(byPlayer);
                return true;
            }

            if (bebc.State == EnumChurnState.Empty || bebc.State == EnumChurnState.Milked)
            {
                if (hotbarSlot.Itemstack?.Collectible.Code.Path == "salt" && hotbarSlot.StackSize >= 5)
                {
                    bebc.State++;
                    hotbarSlot.TakeOut(5);
                    hotbarSlot.MarkDirty();
                }

                return true;
            }
            

            if (bebc.State == EnumChurnState.Butter)
            {
                ItemStack butter = new ItemStack(api.World.GetItem(new AssetLocation("breadnbutter:butter")));
                if (!byPlayer.InventoryManager.TryGiveItemstack(butter, true))
                {
                    api.World.SpawnItemEntity(butter, byPlayer.Entity.Pos.XYZ.Add(0, 0.5, 0));
                }

                bebc.State = EnumChurnState.Empty;
                return true;
            }

            return handled;
        }

        public Shape GetShape()
        {
            return Vintagestory.API.Common.Shape.TryGet(api, Shape.Base);
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            return base.OnBlockInteractCancel(secondsUsed, world, byPlayer, blockSel, cancelReason);
        }
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions;
        }

        public override void TryFillFromBlock(EntityItem byEntityItem, BlockPos pos)
        {
            // Don't fill when dropped as item in water
        }
    }
}