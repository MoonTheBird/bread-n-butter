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
    public class BlockButterChurn : BlockLiquidContainerBase
    {

        public override bool AllowHeldLiquidTransfer => true;

        public override int GetContainerSlotId(BlockPos pos)
        {
            return 1;
        }

        public override int GetContainerSlotId(ItemStack containerStack)
        {
            return 1;
        }

        public int GetChurnerHashCode(ItemStack contentStack, ItemStack liquidStack)
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
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (Attributes?["capacityLitres"].Exists == true)
            {
                capacityLitresFromAttributes = Attributes["capacityLitres"].AsInt(50);
            }

            ItemStack[] saltStack = new ItemStack[] { new ItemStack(api.World.GetItem(new AssetLocation("salt")), 5) };

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
                ItemStack[] linenStack = new ItemStack[] { new ItemStack(api.World.GetBlock(new AssetLocation("linen-normal-down"))) };

                return new WorldInteraction[] {
                new WorldInteraction() {
                    ActionLangCode = "blockhelp-butterchurn-churn",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = null,
                    ShouldApply = (WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection) =>
                    {
                        ButterChurnBE bcbe = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as ButterChurnBE;
                        return saltStack != null ? (bcbe.Done ? false : true) : false;
                    }
                },
                new WorldInteraction() {
                    ActionLangCode = "blockhelp-butterchurn-addsalt",
                    MouseButton = EnumMouseButton.Right,
                    
                    Itemstacks = saltStack,
                    GetMatchingStacks = (WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection) =>
                    {
                        ButterChurnBE bcbe = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as ButterChurnBE;
                        return saltStack;
                    }
                },
                new WorldInteraction() {
                    ActionLangCode = "blockhelp-butterchurn-removebutter",
                    MouseButton = EnumMouseButton.Right,
                    Itemstacks = null,
                    ShouldApply = (WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection) =>
                    {
                        ButterChurnBE bcbe = api.World.BlockAccessor.GetBlockEntity(blockSelection.Position) as ButterChurnBE;
                        return bcbe.Done;
                    }
                }
            };
            });
        }
        public Shape GetShape()
        {
            string path = "shapes/block/food/butterchurn.json";

            return Vintagestory.API.Common.Shape.TryGet(api, path);
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ButterChurnBE bcbe = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as ButterChurnBE;
            ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (bcbe == null) return false;

            if (!bcbe.Done) { 
                bcbe.StartChurn(byPlayer);
                return true;
            }

            if (hotbarSlot.Itemstack?.Collectible.Code.Path == "salt" && hotbarSlot.StackSize >= 5)
            {
                hotbarSlot.TakeOut(5);
                hotbarSlot.MarkDirty();
                return true;
            }

            if (bcbe.Done)
            {
                ItemStack butterStick = new ItemStack(api.World.GetItem(new AssetLocation("butter")));
                if (!byPlayer.InventoryManager.TryGiveItemstack(butterStick, true))
                {
                    api.World.SpawnItemEntity(butterStick, byPlayer.Entity.Pos.XYZ.Add(0, 0.5, 0));
                }
                return true;
            }
            return true;
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

        public void SetContents(ItemStack blockstack, ItemStack contents)
        {
            blockstack.Attributes.SetItemstack("contents", contents);
        }

        public ItemStack GetContents(ItemStack blockstack)
        {
            ItemStack stack = blockstack.Attributes.GetItemstack("contents");
            stack?.ResolveBlockOrItem(api.World);
            return stack;
        }


        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }
    }

    public class ButterChurnBE : BlockEntityContainer
    {
        InventoryGeneric inv;
        public override InventoryBase Inventory => inv;
        public override string InventoryClassName => "butterchurn";
        bool done;
        public bool Done => done;
        float meshangle;
        public virtual float MeshAngle
        {
            get { return meshangle; }
            set
            {
                meshangle = value;
                animRot.Y = value * GameMath.RAD2DEG;
            }
        }

        public ButterChurnBE()
        {
            inv = new InventoryGeneric(1, null, null);
        }
        Vec3f animRot = new Vec3f();
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inv.LateInitialize("butterchurn-" + Pos, api);

            if (api.Side == EnumAppSide.Client)
            {
                animUtil?.InitializeAnimator("butterchurn", (Block as BlockButterChurn).GetShape(), null, animRot);
            }
        }
        BlockEntityAnimationUtil animUtil
        {
            get { return GetBehavior<BEBehaviorAnimatable>()?.animUtil; }
        }
        long listenerId;
        internal void StartChurn(IPlayer byPlayer)
        {
            if (listenerId != 0) return;

            if (Api.Side == EnumAppSide.Client)
            {
                startChurnAnim();
            } else
            {
                (Api as ICoreServerAPI).Network.BroadcastBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1010);
            }

            Api.World.PlaySoundAt(new AssetLocation("sounds/player/squeezehoneycomb.ogg"), Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5, byPlayer, false);

            listenerId = Api.World.RegisterGameTickListener(onChurning, 20);
            secondsPassed = 0;
        }
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (byItemStack != null)
            {
                inv[0].Itemstack = (byItemStack.Block as BlockButterChurn)?.GetContents(byItemStack);
            }
        }

        private void startChurnAnim()
        {
            animUtil.StartAnimation(new AnimationMetaData()
            {
                Animation = "Churn",
                Code = "churn",
                AnimationSpeed = 0.25f,
                EaseOutSpeed = 3,
                EaseInSpeed = 3
            });
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == 1010)
            {
                startChurnAnim();
            }

            base.OnReceivedServerPacket(packetid, data);
        }
        float secondsPassed;
        private void onChurning(float dt)
        {
            secondsPassed += dt;

            if (secondsPassed > 20)
            {
                animUtil?.StopAnimation("churn");
                done = true;
                Api.World.UnregisterGameTickListener(listenerId);
            }
        }
        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            Api.World.UnregisterGameTickListener(listenerId);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            done = tree.GetBool("churned");

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetBool("churned", done);
        }
        
    }
}
