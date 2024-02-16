using System;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace BreadNButter
{
    public class ItemBreadSlice : Item
    {
        public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
        {
            ItemSlot mealSlot = allInputslots.FirstOrDefault(slot => slot.Itemstack?.Collectible is BlockMeal);
            if (mealSlot != null)
            {
                BlockMeal food = mealSlot.Itemstack.Collectible as BlockMeal;
                FoodNutritionProperties[] Foods =  food.GetContentNutritionProperties(api.World, mealSlot, api.World.AllOnlinePlayers[0].Entity);
                float servings = food.GetQuantityServings(api.World, mealSlot.Itemstack);
                ITreeAttribute attr = mealSlot.Itemstack.Attributes;
                string code = food.GetRecipeCode(api.World, mealSlot.Itemstack);
                if (servings < 1.0 || code != "jam")
                {
                    return;
                }

                string name = food.GetContainedInfo(mealSlot);
                string type = "";
                //sobbing
                if (name.Contains("White currant"))
                {
                    type = "whitecurrant";
                }
                else if (name.Contains("Red currant"))
                {
                    type = "redcurrant";
                }
                else if (name.Contains("Black currant"))
                {
                    type = "blackcurrant";
                }
                else if (name.Contains("Cranberry"))
                {
                    type = "cranberry";
                }
                else if (name.Contains("Pineapple slices"))
                {
                    type = "pineapple";
                }
                else if (name.Contains("Saguaro fruit"))
                {
                    type = "cactifruit";
                }
                else if (name.Contains("Blueberry"))
                {
                    type = "blueberry";
                }
                else if (name.Contains("Red apple"))
                {
                    type = "redapple";
                }
                else if (name.Contains("Yellow apple"))
                {
                    type = "yellowapple";
                }
                else if (name.Contains("Pink apple"))
                {
                    type = "pinkapple";
                }
                else if (name.Contains("Orange"))
                {
                    type = "orange";
                }
                else if (name.Contains("Pear"))
                {
                    type = "pear";
                }
                else if (name.Contains("Pomegranate"))
                {
                    type = "pomegranate";
                }
                else if (name.Contains("Mango"))
                {
                    type = "mango";
                }
                else if (name.Contains("Cherry"))
                {
                    type = "cherry";
                }
                else if (name.Contains("Lychee"))
                {
                    type = "lychee";
                }
                else if (name.Contains("Peach"))
                {
                    type = "peach";
                }
                else if (name.Contains("Breadfruit"))
                {
                    type = "breadfruit";
                }
                else if (name.Contains("-"))
                {
                    type = "mixed";
                }

                float totalSatiety = 0f;
                float jamSatiety = 0f;

                Item item = api.World.GetItem(new AssetLocation("breadnbutter:slicedbread-" + Variant["type"] + "-" + Variant["state"] + "-" + type));
                if (food != null && Foods != null)
                {
                    foreach(FoodNutritionProperties fod in Foods)
                    {
                        totalSatiety += fod.Satiety;
                    }
                    jamSatiety = totalSatiety * 1.2f; //boosts the satiety of jam/butter
                }
                ItemStack outStack = new ItemStack(item);
                outStack.StackSize = 1;
                outputSlot.Itemstack = outStack;
                outStack.Attributes.SetFloat("jamSatiety", jamSatiety);
                outputSlot.MarkDirty();
            }

            base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe);
        }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (!tryBeginEatBread(slot, byEntity, ref handHandling))
            {
                return;
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            return tryContinueEatBread(secondsUsed, slot, byEntity);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            tryFinishEatBread(secondsUsed, slot, byEntity, true);
        }

        protected virtual bool tryBeginEatBread(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handHandling)
        {
            if (!byEntity.Controls.ShiftKey && GetContentNutritionProperties(api.World, slot, byEntity) != null)
            {
                byEntity.World.RegisterCallback((dt) =>
                {
                    if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
                    {
                        byEntity.PlayEntitySound("eat", (byEntity as EntityPlayer)?.Player);
                    }
                }, 500);

                handHandling = EnumHandHandling.PreventDefault;
                return true;
            }

            return false;
        }
        public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
        {
            return null;
        }
        protected virtual bool tryContinueEatBread(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            if (GetContentNutritionProperties(byEntity.World, slot, byEntity) == null) return false;

            Vec3d pos = byEntity.Pos.AheadCopy(0.4f).XYZ.Add(byEntity.LocalEyePos);
            pos.Y -= 0.4f;

            IPlayer player = (byEntity as EntityPlayer).Player;

            if (secondsUsed > 0.5f && (int)(30 * secondsUsed) % 7 == 1)
            {
                byEntity.World.SpawnCubeParticles(pos, slot.Itemstack, 0.3f, 4, 1, player);
            }

            if (byEntity.World is IClientWorldAccessor)
            {
                return secondsUsed <= 1.5f;
            }

            // Let the client decide when he is done eating
            return true; 
        }
        //this works
        public static FoodNutritionProperties[] GetContentNutritionProperties(IWorldAccessor world, ItemSlot inSlot, EntityAgent forEntity, bool mulWithStacksize = false, float nutritionMul = 1, float healthMul = 1)
        {
            List<FoodNutritionProperties> foodProps = new List<FoodNutritionProperties>();
            ItemStack bread = inSlot.Itemstack;
            FoodNutritionProperties breadprops = bread.Collectible.NutritionProps;
            FoodNutritionProperties props = breadprops.Clone();

            DummySlot slot = new DummySlot(inSlot.Itemstack, inSlot.Inventory);
            TransitionState state = inSlot.Itemstack.Collectible.UpdateAndGetTransitionState(world, slot, EnumTransitionType.Perish);
            float spoilState = state != null ? state.TransitionLevel : 0;
            float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, forEntity);
            float healthLoss = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, forEntity);
            props.Satiety *= satLossMul * nutritionMul;
            props.Health *= healthLoss * healthMul;
            foodProps.Add(props);
            FoodNutritionProperties props2 = props.Clone();
            props2.Satiety = bread.Attributes.GetFloat("jamSatiety");
            props2.FoodCategory = EnumFoodCategory.Fruit;
            props2.Satiety *= satLossMul * nutritionMul;
            foodProps.Add(props2);

            return foodProps.ToArray();
        }
        public virtual string GetNutritionFacts(IWorldAccessor world, ItemSlot inSlotorFirstSlot, EntityAgent forEntity)
        {
            FoodNutritionProperties[] props = GetContentNutritionProperties(world, inSlotorFirstSlot, forEntity);

            Dictionary<EnumFoodCategory, float> totalSaturation = new Dictionary<EnumFoodCategory, float>();
            float totalHealth = 0;

            for (int i = 0; i < props.Length; i++)
            {
                FoodNutritionProperties prop = props[i];
                if (prop == null) continue;

                float sat;
                totalSaturation.TryGetValue(prop.FoodCategory, out sat);

                DummySlot slot = new DummySlot(inSlotorFirstSlot.Itemstack, inSlotorFirstSlot.Inventory);
                
                TransitionState state = inSlotorFirstSlot.Itemstack.Collectible.UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish);
                float spoilState = state != null ? state.TransitionLevel : 0;

                float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, forEntity);
                float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, forEntity);

                totalHealth += prop.Health * healthLossMul;
                totalSaturation[prop.FoodCategory] = sat + prop.Satiety * satLossMul;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(Lang.Get("Nutrition Facts"));

            foreach (var val in totalSaturation)
            {
                sb.AppendLine(Lang.Get("nutrition-facts-line-satiety", Lang.Get("foodcategory-" + val.Key.ToString().ToLowerInvariant()), Math.Round(val.Value)));
            }

            if (totalHealth != 0)
            {
                sb.AppendLine("- " + Lang.Get("Health: {0}{1} hp", totalHealth > 0 ? "+" : "", totalHealth));
            }

            return sb.ToString();
        }
        protected virtual bool tryFinishEatBread(float secondsUsed, ItemSlot slot, EntityAgent byEntity, bool handleAllServingsConsumed)
        {
            FoodNutritionProperties[] multiProps = GetContentNutritionProperties(byEntity.World, slot, byEntity);

            if (byEntity.World.Side == EnumAppSide.Client || multiProps == null || secondsUsed < 1.45) return false;
            IPlayer player = (byEntity as EntityPlayer).Player;
            if (Consume(byEntity.World, player, slot))
            {
                slot.TakeOut(1);
                slot.MarkDirty();
            }

            return true;
        }
        public virtual bool Consume(IWorldAccessor world, IPlayer eatingPlayer, ItemSlot inSlot)
        {

            FoodNutritionProperties[] multiProps = GetContentNutritionProperties(world, inSlot, eatingPlayer.Entity);

            float totalHealth = 0;
            EntityBehaviorHunger ebh = eatingPlayer.Entity.GetBehavior<EntityBehaviorHunger>();
            float satiablePoints = ebh.MaxSaturation - ebh.Saturation;
            

            float breadSatpoints = 0;
            for (int i = 0; i < multiProps.Length; i++)
            {
                FoodNutritionProperties nutriProps = multiProps[i];
                if (nutriProps == null) continue;

                breadSatpoints += nutriProps.Satiety;
            }


            for (int i = 0; i < multiProps.Length; i++)
            {
                FoodNutritionProperties nutriProps = multiProps[i];
                if (nutriProps == null) continue;
                float sat = nutriProps.Satiety;
                float satLossDelay = Math.Min(1.3f, 3) * 10 + sat / 70f * 60f;

                eatingPlayer.Entity.ReceiveSaturation(sat, nutriProps.FoodCategory, satLossDelay, 1f);

                if (nutriProps.EatenStack?.ResolvedItemstack != null)
                {
                    if (eatingPlayer == null || !eatingPlayer.InventoryManager.TryGiveItemstack(nutriProps.EatenStack.ResolvedItemstack.Clone(), true))
                    {
                        world.SpawnItemEntity(nutriProps.EatenStack.ResolvedItemstack.Clone(), eatingPlayer.Entity.SidedPos.XYZ);
                    }
                }

                totalHealth += nutriProps.Health;
            }


            if (totalHealth != 0)
            {
                eatingPlayer.Entity.ReceiveDamage(new DamageSource()
                {
                    Source = EnumDamageSource.Internal,
                    Type = totalHealth > 0 ? EnumDamageType.Heal : EnumDamageType.Poison
                }, Math.Abs(totalHealth));
            }


            return true;
        }
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            string facts = GetNutritionFacts(world, inSlot, null);

            if (facts != null)
            {
                dsc.Append(facts);
            }
        }
    }
}