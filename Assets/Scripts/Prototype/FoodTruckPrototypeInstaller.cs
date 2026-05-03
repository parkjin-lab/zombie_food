using UnityEngine;

namespace ZombieFoodcenter.Prototype
{
    public static class FoodTruckPrototypeInstaller
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePrototypeHud()
        {
            if (Object.FindFirstObjectByType<FoodTruckPrototypeHud>() != null)
            {
                return;
            }

            GameObject root = new GameObject("FoodTruckPrototypeRoot");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<FoodTruckPrototypeHud>();
        }
    }
}
