using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace BreadNButter
{
    public class ItemBoule : Item
    {
        public string Type => Variant["type"];
        public string State => Variant["state"];
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (byEntity.Controls.ShiftKey && blockSel != null)
            {
                IPlayer byPlayer = null;
                if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
                if (byPlayer == null) return;

                if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
                {
                    return;
                }


                Block placeblock = api.World.GetBlock(new AssetLocation("breadnbutter:boule-" + Type + "-" + State));
                BlockPos targetPos = blockSel.Position.AddCopy(blockSel.Face);
                string failureCode="";

                BlockSelection placeSel = blockSel.Clone();
                placeSel.Position.Add(blockSel.Face);

                if (placeblock.TryPlaceBlock(api.World, byPlayer, slot.Itemstack, placeSel, ref failureCode))
                {
                    BEBoule bec = api.World.BlockAccessor.GetBlockEntity(targetPos) as BEBoule;
                    if (bec != null)
                    {
                        slot.TakeOut(1);
                        slot.MarkDirty();
                    }

                    api.World.PlaySoundAt(placeblock.Sounds.Place, targetPos.X + 0.5, targetPos.Y, targetPos.Z + 0.5, byPlayer);

                    handling = EnumHandHandling.PreventDefault;
                } else
                {
                    (api as ICoreClientAPI)?.TriggerIngameError(this, failureCode, Lang.Get("placefailure-" + failureCode));
                }

                return;
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
        }

    }
}