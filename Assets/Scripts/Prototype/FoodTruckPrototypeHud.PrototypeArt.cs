using UnityEngine;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private const string FoodTruckSpriteResourcesPath = "FoodTruckPrototype/Sprites/Truck/";
        private const string KitchenModuleSpriteResourcesPath = "FoodTruckPrototype/Sprites/KitchenModules/";

        private void TryAutoLoadPrototypeArt()
        {
            if (!autoLoadPrototypeArt)
            {
                return;
            }

            foodTruckSprite = foodTruckSprite != null
                ? foodTruckSprite
                : LoadFirstPrototypeSprite(
                    FoodTruckSpriteResourcesPath,
                    "FoodTruck",
                    "food_truck",
                    "Truck",
                    "truck");

            kitchenModuleSprite = kitchenModuleSprite != null
                ? kitchenModuleSprite
                : LoadFirstPrototypeSprite(
                    KitchenModuleSpriteResourcesPath,
                    "KitchenModule",
                    "kitchen_module",
                    "Kitchen",
                    "kitchen");
        }

        private static Sprite LoadFirstPrototypeSprite(string resourcePath, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Sprite sprite = Resources.Load<Sprite>(resourcePath + names[i]);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private void ApplyKitchenModuleCellSprite(UnityEngine.UI.Image image, bool active)
        {
            if (image == null)
            {
                return;
            }

            bool showModule = active && kitchenModuleSprite != null;
            image.sprite = showModule ? kitchenModuleSprite : null;
            image.preserveAspect = showModule;
            image.type = UnityEngine.UI.Image.Type.Simple;
        }
    }
}
