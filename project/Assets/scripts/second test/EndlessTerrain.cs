using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Water;

public class EndlessTerrain : MonoBehaviour
{
    const float scale=1;
    const float moveThreshholdBeforeUpdate = 25f;
    const float sqrMoveThreshholdBeforeUpdate = moveThreshholdBeforeUpdate * moveThreshholdBeforeUpdate;

    public static float maxViewDst;

    public LODInfo[] detailLevels;
    public Transform viewer;
    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    public Material mapMaterial;
    int chunkSize;
    int chunckVisibleInViewDst;
    public List<GameObject> check = new List<GameObject>();
    public GenerateStructures genstruct;
    public GameObject waterObjects;
    Dictionary<Vector2,TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

     void Start() {
        genstruct = this.GetComponent<GenerateStructures>();
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = MapGenerator.mapChunkSize - 1;
        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshhold;
        chunckVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
        UpdateVisibleChunks();


    }
    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x,viewer.position.z)/scale;
        if ((viewerPositionOld-viewerPosition).sqrMagnitude > sqrMoveThreshholdBeforeUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();

        }
    }
    void UpdateVisibleChunks()
    {
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);


        for (int yOffset = -chunckVisibleInViewDst;yOffset<=chunckVisibleInViewDst;yOffset++)
        {
            for (int xOffset = -chunckVisibleInViewDst; xOffset <= chunckVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChuckCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset) ;

                if (terrainChunkDictionary.ContainsKey(viewedChuckCoord))
                {
                    terrainChunkDictionary[viewedChuckCoord].UpdateTerrainChunk();
                   
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChuckCoord,new TerrainChunk(viewedChuckCoord,chunkSize,detailLevels,transform,mapMaterial, waterObjects,genstruct,check));
                }
            }
        }

    }

    public class TerrainChunk
    {
        GameObject meshObject;
        GameObject waterObject;
        MeshRenderer waterMeshRenderer;
        MeshFilter waterMeshFilter;

        GameObject city;
        GenerateStructures genstructs;

        Vector2 position;
        Bounds bounds;
       

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;
        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLODMesh;
        List<GameObject> checkie;
        Vector3 possie;
        MapData mapData;
        bool mapDataReceived;
        int previousLodIndex =-1;

        public TerrainChunk(Vector2 coord ,int size, LODInfo[] detailLevels,Transform parent,Material material, GameObject _waterObject ,GenerateStructures _genstruct,List<GameObject> _check)
        {
            this.detailLevels = detailLevels;
            genstructs = _genstruct;  
            position = coord * size;
            checkie = new List<GameObject>();
            bounds = new Bounds(position,Vector2.one*size);
            Vector3 positionV3 = new Vector3(position.x,0,position.y);
            possie = positionV3;
            //meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;
            //Debug.Log(coord + " IS COORD " + "===" + position + " IS POSITION "+"====="+ possie + " IS possie ");
            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;

            _check.Add(meshObject);
            checkie = _check;
            waterObject = GameObject.Instantiate(_waterObject);
            //waterMeshRenderer.material = material;
            waterObject.transform.position = new Vector3(positionV3.x * scale, 31.67f, positionV3.z * scale);
            //waterObject.transform.position.z
            waterObject.transform.parent = meshObject.transform;
            waterObject.transform.localScale = new Vector3(24,1,24);

            city = GameObject.CreatePrimitive(PrimitiveType.Cube);
            city.gameObject.transform.parent = meshObject.transform;
            genstructs.adswithwater(city,meshObject.transform);

            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length;i++)
            {
                //lodMeshes[i].gen = FindObjectOfType<GenerateStructures>();
                lodMeshes[i] = new LODMesh(detailLevels[i].lod,UpdateTerrainChunk);
                if (detailLevels[i].useForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }
        
            mapGenerator.RequestMapData(position,OnMapDataReceived);


          //  meshFilter.mesh.triangles[1].


        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;
            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap,MapGenerator.mapChunkSize,MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;
            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNearestEdge <= maxViewDst;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i<detailLevels.Length-1;i++)
                    {
                        //Debug.Log("aantal levels");
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshhold)
                        {
                            //Debug.Log("aantal stappen gemaakt");
                            lodIndex = i + 1;
                        }
                        else
                        {
                            //Debug.Log("aantal nopes");

                            break;
                        }

                    }

                    if (lodIndex != previousLodIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLodIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.ReqeustMesh(mapData,possie, checkie);
                        }
                    }
                    if (lodIndex==0)
                    {
                        if (collisionLODMesh.hasMesh)
                        {
                            meshCollider.sharedMesh = collisionLODMesh.mesh;
                        }else if (!collisionLODMesh.hasRequestedMesh)
                        {
                            collisionLODMesh.ReqeustMesh(mapData,possie, checkie);
                        }
                    }
                    terrainChunksVisibleLastUpdate.Add(this);

                }

                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        } 
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }
    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
       // public fStructures gen;
        int lod;
        System.Action updateCallback;
        //Vector3 trianglepos;
        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;

        }
        void OnMeshDataReceaved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            //trianglepos = meshData.getpos(Vector3.zero);
            //gen.gen(trianglepos);
            updateCallback();
            
        }
        public void ReqeustMesh(MapData mapData,Vector3 _position, List<GameObject> _parent)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData,lod,OnMeshDataReceaved,_position,_parent);
        }
    }
    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDstThreshhold;
        public bool useForCollider;
    }

}
