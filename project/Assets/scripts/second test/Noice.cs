using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noice
{
    public enum NormaliseMode { Local, Global}
    public static float[,] GenerateNoiseMap(int mapWidth,int mapHeight,int seed, float scale, int octaves,float persistence,float lacunarity,Vector2 offset,NormaliseMode normaliseMode)
    {
        float[,] noiseMap = new float[mapWidth,mapHeight];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        float amplitude = 1;
        float frequency = 1;

        float maxPossibleHeight = 0;
        for (int i = 0; i < octaves;i++)
        {
            float offsetx = prng.Next(-100000,100000) + offset.x;
            float offsety = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetx,offsety);
            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }
        if (scale <= 0)
        {
            scale = 0.0001f;
        }


        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;
        float halfwidth = mapWidth / 2f;
        float halfheight = mapHeight / 2f;
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                 amplitude = 1;
                 frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i<octaves;i++)
                {
                    float sampleX = (x - halfwidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfheight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY)* 2 -1;
                    noiseHeight += perlinValue * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;


                }
                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }else if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                noiseMap[x, y] = noiseHeight;
            }
        }
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normaliseMode == NormaliseMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else if (normaliseMode == NormaliseMode.Global)
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) /(maxPossibleHeight);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0,int.MaxValue);
                }
            }
        }

                return noiseMap;
    }


    
}
