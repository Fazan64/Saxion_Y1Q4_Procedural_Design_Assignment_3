using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class MushroomTextureGenerator
{
    public Color colorA = Color.white;
    public Color colorB = Color.black;

    public Vector2Int resolution = new Vector2Int(512, 512);
    public Vector2 scale  = Vector2.one;
    public Vector2 offset = Vector2.zero;
    
    public Texture2D GenerateTexture()
    {
        var texture = CreateEmptyTexture();
        
        Color32[] colors = GenerateColors();
        texture.SetPixels32(colors);
        texture.Apply();
        
        return texture;
    }

    public async Task<Texture2D> GenerateTextureAsync()
    {
        return await GenerateTextureAsync(CancellationToken.None);
    }

    public async Task<Texture2D> GenerateTextureAsync(CancellationToken cancellationToken)
    {
        Texture2D texture = CreateEmptyTexture();

        Color32[] colors = await Task.Run(() => GenerateColors(), cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        
        texture.SetPixels32(colors);
        texture.Apply();

        return texture;
    }

    private Texture2D CreateEmptyTexture()
    {
        return new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA32, mipmap: true);
    }

    private Color32[] GenerateColors()
    {
        var colors = new Color32[resolution.x * resolution.y];

        float invWidth  = 1f / resolution.x;
        float invHeight = 1f / resolution.y;
        
        for (int x = 0; x < resolution.x; x++)
        {
            for (int y = 0; y < resolution.y; y++)
            {
                float u = x * invWidth;
                float v = y * invHeight;
                int index = x + y * resolution.x;
                float t = SimplexNoise.SeamlessNoise(u, v, scale.x, scale.y, offset.x + offset.y);
                
                colors[index] = Color.Lerp(colorA, colorB, t);
            }
        }

        return colors;
    }
}