using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using System.Collections.Generic;
using Vintagestory.API.Util;
using Vintagestory.ServerMods.NoObf;

namespace BreadNButter
{
    public class BEBoule : BlockEntityContainer
    {
        InventoryGeneric inv;
        public override InventoryBase Inventory => inv;

        public override string InventoryClassName => "boule";

        public BEBoule()
        {
            inv = new InventoryGeneric(1, null, null);
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inv.LateInitialize("boule-" + Pos, api);
        }
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (byItemStack != null)
            {
                inv[0].Itemstack = byItemStack.Clone();
                inv[0].Itemstack.StackSize = 1;
            }
        }
        public ItemStack CutUp()
        {
            ItemBoule boule = inv[0].Itemstack.Collectible as ItemBoule;
            MarkDirty(true);
            ItemStack stack = new ItemStack(Api.World.GetItem(new AssetLocation ("breadnbutter:slicedbread-" + boule.Type + "-" + boule.State + "-none")));
            inv[0].Itemstack = null;
            stack.StackSize = 8;
            Api.World.BlockAccessor.SetBlock(0, Pos);            
            return stack;
        }

    }
}