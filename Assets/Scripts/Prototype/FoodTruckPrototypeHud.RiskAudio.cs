using UnityEngine;

namespace ZombieFoodcenter.Prototype
{
    public sealed partial class FoodTruckPrototypeHud
    {
        private void EnsureRiskIconSprites()
        {
            if (riskIconHighSprite != null && riskIconMidSprite != null && riskIconLowSprite != null)
            {
                return;
            }

            ReleaseRiskIconSprites();
            riskIconLowSprite = CreateRiskIconSprite(RiskIconGlyph.Circle, out riskIconLowTexture);
            riskIconMidSprite = CreateRiskIconSprite(RiskIconGlyph.Diamond, out riskIconMidTexture);
            riskIconHighSprite = CreateRiskIconSprite(RiskIconGlyph.Triangle, out riskIconHighTexture);
        }

        private void ReleaseRiskIconSprites()
        {
            DestroyGeneratedSprite(ref riskIconHighSprite);
            DestroyGeneratedSprite(ref riskIconMidSprite);
            DestroyGeneratedSprite(ref riskIconLowSprite);
            DestroyGeneratedTexture(ref riskIconHighTexture);
            DestroyGeneratedTexture(ref riskIconMidTexture);
            DestroyGeneratedTexture(ref riskIconLowTexture);
        }

        private static Sprite CreateRiskIconSprite(RiskIconGlyph glyph, out Texture2D texture)
        {
            texture = CreateRiskIconTexture(glyph, 24);
            if (texture == null)
            {
                return null;
            }

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            sprite.name = "RiskIcon_" + glyph;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Texture2D CreateRiskIconTexture(RiskIconGlyph glyph, int size)
        {
            if (size < 8)
            {
                size = 8;
            }

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = ((x + 0.5f) / size) * 2f - 1f;
                    float ny = ((y + 0.5f) / size) * 2f - 1f;
                    bool inside = IsRiskIconPixelInside(glyph, nx, ny);
                    pixels[y * size + x] = inside
                        ? new Color32(255, 255, 255, 255)
                        : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static bool IsRiskIconPixelInside(RiskIconGlyph glyph, float x, float y)
        {
            switch (glyph)
            {
                case RiskIconGlyph.Triangle:
                {
                    const float bottom = -0.72f;
                    const float top = 0.78f;
                    if (y < bottom || y > top)
                    {
                        return false;
                    }

                    float t = (y - bottom) / (top - bottom);
                    float halfWidth = Mathf.Lerp(0.84f, 0.04f, t);
                    return Mathf.Abs(x) <= halfWidth;
                }
                case RiskIconGlyph.Diamond:
                    return Mathf.Abs(x) * 1.08f + Mathf.Abs(y) <= 0.80f;
                default:
                    return x * x + y * y <= 0.62f * 0.62f;
            }
        }

        private static void DestroyGeneratedSprite(ref Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            DestroyGeneratedObject(sprite);
            sprite = null;
        }

        private static void DestroyGeneratedTexture(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            DestroyGeneratedObject(texture);
            texture = null;
        }

        private static void DestroyGeneratedObject(Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        private void EnsureSfxAudioSource()
        {
            if (sfxAudioSource == null)
            {
                sfxAudioSource = GetComponent<AudioSource>();
            }

            if (sfxAudioSource == null)
            {
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (sfxAudioSource == null)
            {
                return;
            }

            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.loop = false;
            sfxAudioSource.spatialBlend = 0f;
        }

        private void TryAutoLoadPrototypeSfx()
        {
            if (!autoLoadPrototypeSfx)
            {
                return;
            }

            if (sfxSelectClip == null)
            {
                sfxSelectClip = LoadPrototypeAudioClip("Select");
            }

            if (sfxPlaceSuccessClip == null)
            {
                sfxPlaceSuccessClip = LoadPrototypeAudioClip("Melee0");
            }

            if (sfxPlaceBlockedClip == null)
            {
                sfxPlaceBlockedClip = LoadPrototypeAudioClip("Hit1");
            }

            if (sfxComboBurstClip == null)
            {
                sfxComboBurstClip = LoadPrototypeAudioClip("Range");
            }

            if (sfxOverheatClip == null)
            {
                sfxOverheatClip = LoadPrototypeAudioClip("Lose");
            }

            if (sfxProgressionUnlockClip == null)
            {
                sfxProgressionUnlockClip = LoadPrototypeAudioClip("LevelUp");
            }

            if (sfxHeatWarningClip == null)
            {
                sfxHeatWarningClip = LoadPrototypeAudioClip("Hit0");
            }

            if (sfxHeatStabilizedClip == null)
            {
                sfxHeatStabilizedClip = LoadPrototypeAudioClip("Win");
            }
        }

        private void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxVolume <= 0f)
            {
                return;
            }

            EnsureSfxAudioSource();
            if (sfxAudioSource == null)
            {
                return;
            }

            float finalVolume = Mathf.Clamp01(sfxVolume * Mathf.Max(0f, volumeScale));
            if (finalVolume <= 0.001f)
            {
                return;
            }

            sfxAudioSource.PlayOneShot(clip, finalVolume);
        }

        private static AudioClip LoadPrototypeAudioClip(string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
            {
                return null;
            }

            AudioClip resourcesClip = Resources.Load<AudioClip>("Undead Survivor/Audio/" + clipName);
            if (resourcesClip != null)
            {
                return resourcesClip;
            }

#if UNITY_EDITOR
            string assetPath = "Assets/Resources/Undead Survivor/Audio/" + clipName + ".wav";
            return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
#else
            return null;
#endif
        }
    }
}
