using UnityEngine;
using System.Collections.Generic;
public static class SpriteCombiner
{

    // 核心函数：通过叠加多个Sprite创建一个新的Sprite
    public static Sprite CombineSprites(List<Sprite> spritesToCombine, string spriteName)
    {
        // 参数检查
        if (spritesToCombine == null || spritesToCombine.Count == 0)
        {
            Debug.LogError("Sprite列表为空或未初始化！");
            return null;
        }
        Debug.Log($"图片合成器：目前图片数量{spritesToCombine.Count}");

        // 1. 以第一个Sprite的尺寸为基准，创建新的Texture2D
        Sprite firstSprite = spritesToCombine[0];
        // 纹理必须可读（Import Settings 勾选 Read/Write Enabled），否则 GetPixels 会抛异常
        if (!firstSprite.texture.isReadable)
        {
            Debug.LogError($"[SpriteCombiner] 纹理不可读，请在 Import Settings 勾选 Read/Write Enabled: {firstSprite.texture.name}");
            return null;
        }

        Texture2D baseTexture = firstSprite.texture;
        Texture2D combinedTexture = new Texture2D(baseTexture.width, baseTexture.height, TextureFormat.RGBA32, false);

        // 2. 准备颜色数据数组
        Color[] basePixels = baseTexture.GetPixels();
        Color[] combinedPixels = new Color[basePixels.Length];

        // 3. 初始化合成像素（例如，从透明开始，或直接拷贝第一张图的像素）
        // 这里选择直接拷贝第一张图的像素作为基底
        System.Array.Copy(basePixels, combinedPixels, basePixels.Length);

        // 4. 循环叠加后续Sprite的像素
        for (int i = 1; i < spritesToCombine.Count; i++)
        {
            Sprite currentSprite = spritesToCombine[i];

            // 确保Sprite存在且纹理可读
            if (currentSprite == null || currentSprite.texture == null)
            {
                Debug.LogWarning($"列表中的第{i}个Sprite为空，已跳过。");
                if (currentSprite != null)
                {
                    Debug.LogError(currentSprite.name);
                }
                continue;
            }
            Color[] currentPixels = currentSprite.texture.GetPixels();
            // 简单的颜色叠加混合（可根据需要替换混合模式）
            for (int j = 0; j < combinedPixels.Length && j < currentPixels.Length; j++)
            {
                combinedPixels[j] = SimpleBlend(combinedPixels[j], currentPixels[j]);
            }
        }

        // 5. 将处理好的像素数组应用至新纹理
        combinedTexture.SetPixels(combinedPixels);
        combinedTexture.Apply(); // 应用更改

        // 6. 从纹理创建Sprite
        Sprite newSprite = Sprite.Create(combinedTexture,
                                        new Rect(0, 0, combinedTexture.width, combinedTexture.height),
                                        new Vector2(0.5f, 0.5f), // 设置中心点，例如Center (0.5, 0.5)
                                        firstSprite.pixelsPerUnit);
        newSprite.name = spriteName;

        return newSprite;
    }

    // 一个简单的颜色混合函数（"正常"混合模式，考虑Alpha通道）
    private static Color SimpleBlend(Color baseColor, Color overlayColor)
    {
        float alpha = overlayColor.a;
        Color blendedColor;
        blendedColor.r = (overlayColor.r * alpha) + (baseColor.r * (1f - alpha));
        blendedColor.g = (overlayColor.g * alpha) + (baseColor.g * (1f - alpha));
        blendedColor.b = (overlayColor.b * alpha) + (baseColor.b * (1f - alpha));
        blendedColor.a = baseColor.a + overlayColor.a * (1f - baseColor.a); // 也可根据需要调整最终Alpha
        return blendedColor;
    }
}