using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using System.Collections.Generic;
using Vintagestory.API.Util;

namespace BreadNButter
{
    public class BlockBoule : Block
    {
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            interactions = ObjectCacheUtil.GetOrCreate(api, "breadInteractions-", () =>
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-bread-cut",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = BlockUtil.GetKnifeStacks(api),
                        GetMatchingStacks = (wi, bs, es) => {
                            BEBoule beb = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BEBoule;
                            if (beb != null)
                            {
                                return wi.Itemstacks;
                            }
                            return null;
                        }
                    }
                };
            });
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            EnumTool? tool = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible.Tool;
            if (tool == EnumTool.Knife || tool == EnumTool.Sword)
            {
                BEBoule beb = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBoule;

                ItemStack stack = beb?.CutUp();
                if (stack != null)
                {
                    if (!byPlayer.InventoryManager.TryGiveItemstack(stack, true))
                    {
                        world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                    }
                }

                return true;
            } else
            {
                BEBoule beb = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEBoule;
                var stack = beb.Inventory[0].Itemstack;
                if (stack != null)
                {
                    if (!byPlayer.InventoryManager.TryGiveItemstack(stack, true))
                    {
                        world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                    }
                }

                world.BlockAccessor.SetBlock(0, blockSel.Position);
                return true;
            }
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}