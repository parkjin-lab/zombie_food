using System;
using UnityEditor;
using UnityEngine;

namespace ZombieFoodcenter.Editor
{
    public sealed class FoodTruckPrototypeTexturePostprocessor : AssetPostprocessor
    {
        private const string AutoSpriteRoot = "Assets/Resources/FoodTruckPrototype/";

        private void OnPreprocessTexture()
        {
            string normalizedPath = assetPath.Replace('\\', '/');
            if (!normalizedPath.StartsWith(AutoSpriteRoot, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!(assetImporter is TextureImporter textureImporter))
            {
                return;
            }

            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.alphaIsTransparency = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.maxTextureSize = 512;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
