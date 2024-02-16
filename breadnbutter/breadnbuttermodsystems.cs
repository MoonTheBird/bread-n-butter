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
    public class BNBMod : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("butterchurnblock", typeof(BlockButterChurn));
            api.RegisterBlockEntityClass("butterchurnbe", typeof(ButterChurnBE));
            api.RegisterBlockClass("BlockBoule", typeof(BlockBoule));
            api.RegisterBlockEntityClass("Boule", typeof(BEBoule));
            api.RegisterItemClass("ItemBoule", typeof(ItemBoule));
            api.RegisterItemClass("slicedbread", typeof(ItemBreadSlice));
        }
    }
}
