using System.Collections.Generic;
using System.Linq;
using JsonAssets.Data;
using SpaceShared.APIs;

namespace JsonAssets.Framework.ContentPatcher
{
    internal class ContentPatcherIntegration
    {
        private static IContentPatcherApi Cp;
        private static IApi Ja;

        internal static bool IdsAssigned;
        internal static int IdsAssignedGen = -1;

        private static List<BaseToken> Tokens;

        public static void Initialize()
        {
            ContentPatcherIntegration.Cp = Mod.instance.Helper.ModRegistry.GetApi<IContentPatcherApi>("Pathoschild.ContentPatcher");
            ContentPatcherIntegration.Ja = Mod.instance.Helper.ModRegistry.GetApi<IApi>("spacechase0.JsonAssets");
            if (ContentPatcherIntegration.Cp == null)
                return;

            Mod.instance.Helper.Events.GameLoop.ReturnedToTitle += (s, e) => ContentPatcherIntegration.IdsAssigned = false;

            ContentPatcherIntegration.Tokens = new List<BaseToken>
            {
                new IdToken("Object",Ja.GetAllObjectIds),
                new IdToken("Crop", Ja.GetAllCropIds),
                new IdToken("FruitTree", Ja.GetAllFruitTreeIds),
                new IdToken("BigCraftable", Ja.GetAllBigCraftableIds),
                new IdToken("Hat", Ja.GetAllHatIds),
                new IdToken("Weapon", Ja.GetAllWeaponIds),
                new IdToken("Pants", Ja.GetAllPantsIds),
                new IdToken("Shirt", Ja.GetAllShirtIds),
                new IdToken("Boots", Ja.GetAllBootsIds),
                new IdToken("Clothing", Ja.GetAllClothingIds),
                new SpriteTilesheetToken("Object", () => Mod.instance.Objects.ToList<DataNeedsIdWithTexture>()),
                new SpriteCoordinateToken("Object", true, () => Mod.instance.Objects.ToList<DataNeedsIdWithTexture>()),
                new SpriteCoordinateToken("Object", false, () => Mod.instance.Objects.ToList<DataNeedsIdWithTexture>()),
                new SpriteTilesheetToken("Crop", () => Mod.instance.Crops.ToList<DataNeedsIdWithTexture>()),
                new SpriteCoordinateToken("Crop", true, () => Mod.instance.Crops.ToList<DataNeedsIdWithTexture>()),
                new SpriteCoordinateToken("Crop", false, () => Mod.instance.Crops.ToList<DataNeedsIdWithTexture>()),
                new SpriteTilesheetToken("FruitTree", () => Mod.instance.FruitTrees.ToList<DataNeedsIdWithTexture>()),
                new SpriteCoordinateToken("FruitTree", true, () => Mod.instance.FruitTrees.ToList<DataNeedsIdWithTexture>()),
                new SpriteCoordinateToken("FruitTree", false, () => Mod.instance.FruitTrees.ToList<DataNeedsIdWithTexture>()),
                new SpriteTilesheetToken("BigCraftable", () => Mod.instance.BigCraftables.ToList<DataNeedsIdWithTexture>()),
                new SpriteCoordinateToken("BigCraftable", true, () => Mod.instance.BigCraftables.ToList<DataNeedsIdWithTexture>()),
                new SpriteCoordinateToken("BigCraftable", false, () => Mod.instance.BigCraftables.ToList<DataNeedsIdWithTexture>()),
                new SpriteTilesheetToken("Hat", () => Mod.instance.Hats.ToList<DataNeedsIdWithTexture>()),
                new SpriteCoordinateToken("Hat", true, () => Mod.instance.Hats.ToList<DataNeedsIdWithTexture>()),
                new SpriteCoordinateToken("Hat", false, () => Mod.instance.Hats.ToList<DataNeedsIdWithTexture>()),
                new SpriteTilesheetToken("Weapon", () => Mod.instance.Weapons.ToList<DataNeedsIdWithTexture>()),
                new SpriteCoordinateToken("Weapon", true, () => Mod.instance.Weapons.ToList<DataNeedsIdWithTexture>()),
                new SpriteCoordinateToken("Weapon", false, () => Mod.instance.Weapons.ToList<DataNeedsIdWithTexture>())
            };
            // TODO: Shirt tilesheet
            // TODO: Shirt x
            // TODO: Shirt y
            // TODO: Pants tilesheet
            // TODO: Pants x
            // TODO: Pants y

            foreach (var token in ContentPatcherIntegration.Tokens)
            {
                //cp.RegisterToken(Mod.instance.ModManifest, token.TokenName, token.UpdateContext, token.IsReady, token.GetValue, true, true);
                ContentPatcherIntegration.Cp.RegisterToken(Mod.instance.ModManifest, token.TokenName, token);
            }
        }
    }
}
