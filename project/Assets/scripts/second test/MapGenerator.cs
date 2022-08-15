 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        noiseMap,colorMap,Mesh, FallofMap
    };
    public Noice.NormaliseMode normaliseMode;
    public DrawMode drawMode;

    public const int mapChunkSize = 239;
    [Range(0,6)]
    public int editorPreviewLOD;
    public float noiseScale;
    public bool autoUpdate;

    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    public int seed;
    public Vector2 offet;

    public bool useFallof;
    public GenerateStructures generator;
    public TerrainType[] regions;

    float[,] fallofMap;

    public Vector3 meshheight;
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();


    //=================================
    //  START DrawMapInEditor FUNCTION
    //  filling the mapdata by generating it in the generatedata function
    //  and filling the mapdisplay by finding the object with the MapDisplay script in the scene
    //  draws the map coresponding to the drawmode selected in the inspector

    //=================================
    private void Awake()
    {

        fallofMap = FallofGen.GenerateFallofMap(mapChunkSize);
    }
    public void DrawMapInEditor()
    {
        generator.updateCity();
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        List<GameObject> l = new List<GameObject>();
        if (drawMode == DrawMode.noiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.colorMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));

        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD, generator,Vector3.zero,l), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.FallofMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FallofGen.GenerateFallofMap(mapChunkSize)));
        }
    }
    //=================================
    //  END DrawMapInEditor FUNCTION
    //=================================


    //=================================
    //  START RequestMapData FUNCTION
    //  starts mapdata thread with callback value
    //=================================
    public void RequestMapData(Vector2 center,Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center,callback);
        };
        new Thread(threadStart).Start();

    }
    //=================================
    //  END RequestMapData FUNCTION
    //=================================

    //=================================
    // START MapDataThread FUNCTION
    // generate Mapdata ACTION
    // enqueue the thread info from the callback and the mapData
    //=================================

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }

    }
    //=================================
    //  END MapDataThread FUNCTION
    //=================================


    public void RequestMeshData(MapData mapData,int lod, Action<MeshData> callback,Vector3 _position, List<GameObject> _parent)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData,lod,callback,_position, _parent);
        };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData,int lod, Action<MeshData> callback,Vector3 _position, List<GameObject> _parent)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier,meshHeightCurve,lod, generator,_position,_parent);
        
        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }


    //=================================
    //  START Update FUNCTION
    // go through MapDataThreadQueue
    // dequeue the thread after Regertering and adding the paremeter to the callback value in the threadInfo
    //=================================


    void Update()
    {
        if (mapDataThreadInfoQueue.Count >0)
        {
            for (int i = 0; i < mapDataThreadInfoQueue.Count;i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.paremeter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.paremeter);
            }
        }
    }

    //=================================
    //  END Update FUNCTION
    //=================================

    //=================================
    //  START GenerateData CLASS
    // create a 2d array for the noisemap and filling it with the generate noisemap function from the Noice class with the added parameters
    // creat color array the size of the map chunk
    // looping though the chunk comparing the color data and coloring the color map with the correspoding color to heightvalue
    // returning the mapdata
    //=================================

    MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noice.GenerateNoiseMap(mapChunkSize+2, mapChunkSize+2,seed, noiseScale,octaves,persistance,lacunarity,center+offet,normaliseMode);
        Color[] colorMap = new Color[mapChunkSize* mapChunkSize];
        //Debug.Log(noiseMap.x);
        for (int y = 0; y< mapChunkSize;y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if (useFallof)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y]-fallofMap[x,y]);
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0;i<regions.Length;i++)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        
                        //if (regions[i].name == "grass")
                        //{
                        //    Debug.Log("HASSSSA");
                        //}
                        
                    }
                   
                    else
                    {
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap,colorMap);
    }

    //=================================
    //  END GenerateData CLASS
    //=================================

    //=================================
    //  START OnValidate FUNCTION
    // adding restictions to lacunarity and octives so they wont go below there minimum values
    //=================================

    private void OnValidate()
    {
       
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }

        fallofMap = FallofGen.GenerateFallofMap(mapChunkSize);


    }

    //=================================
    //  END OnValidate FUNCTION
    //=================================

    //=================================
    //  START MapThreadInfo STRUCT WITH PAREMETER T
    //  design of structure Of the MapThreadInfoQueue
    //=================================
    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T  paremeter;

        public MapThreadInfo(Action<T> callback, T paremeter)
        {
            this.callback = callback;
            this.paremeter = paremeter;
        }

       
    }
    //=================================
    //  END MapThreadInfo STRUCT
    //=================================
}


//=================================
//  START Serializable TerrainType STRUCT
//  design of the structure from the terrain type that you wil be able to costumise in the inspector
//=================================
[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}
//=================================
//  END Serializable TerrainType STRUCT
//=================================

//=================================
//  START MapData STRUCT
//  design of structure Of the MapData adding its parameter 
//=================================
public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
//=================================
//  END MapData STRUCT
//=================================
