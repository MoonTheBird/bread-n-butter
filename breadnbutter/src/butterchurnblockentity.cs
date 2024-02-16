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
    public enum EnumChurnState
    {
        Empty = 0,
        Salted = 1,
        Milked = 2,
        SaltedMilked = 3,
        Butter = 4
    }
    public class ButterChurnBE : BlockEntityContainer
    {
        public int CapacityLitres { get; set; } = 50;
        InventoryGeneric inv;
        public override InventoryBase Inventory => inv;
        EnumChurnState state;
        public override string InventoryClassName => "butterchurn";

        public ButterChurnBE()
        {
            inv = new InventoryGeneric(1, null, null, (id, self) =>
            {
                return new ItemSlotLiquidOnly(self, 50);
            });
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inv.LateInitialize("butterchurn-" + Pos, api);

            if (api.Side == EnumAppSide.Client)
            {
                animUtil?.InitializeAnimator("butterchurn", (Block as BlockButterChurn).GetShape(), null);
            }
        }

        public EnumChurnState State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
                MarkDirty(true);
            }
        }

        BlockEntityAnimationUtil animUtil
        {
            get { return GetBehavior<BEBehaviorAnimatable>()?.animUtil; }
        }
        long listenerId;

        internal void StartChurn(IPlayer byPlayer)
        {
            if (state != EnumChurnState.SaltedMilked || listenerId != 0) return;

            if (Api.Side == EnumAppSide.Client)
            {
                startChurnAnim();
            } 
            else
            {
                (Api as ICoreServerAPI).Network.BroadcastBlockEntityPacket(Pos.X, Pos.Y, Pos.Z, 1010);
            }

            //Api.World.PlaySoundAt(new AssetLocation("sounds/player/wetclothsqueeze.ogg"), Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5, byPlayer, false);

            listenerId = Api.World.RegisterGameTickListener(onChurning, 20);
            secondsPassed = 0;
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

                Api.World.UnregisterGameTickListener(listenerId);
            }
        }
        private void startChurnAnim()
        {
            animUtil.StartAnimation(new AnimationMetaData()
            {
                Animation = "Crank",
                Code = "crank",
                AnimationSpeed = 0.25f,
                EaseOutSpeed = 3,
                EaseInSpeed = 3
            });
        }
        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            Api.World.UnregisterGameTickListener(listenerId);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            state = (EnumChurnState)tree.GetInt("state");

        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("state", (int)state);
        }
    }
}