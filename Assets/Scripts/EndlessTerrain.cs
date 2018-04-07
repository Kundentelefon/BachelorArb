using UnityEngine;
using System.Collections.Generic; //for dictionary

public class EndlessTerrain : MonoBehaviour
{

    const float scale = 5f;  //scale it uniformly. for example scale it to match the player. bigger = bigger brush size

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate; //sqr is always faster than getting the actual distance, because it doesnt need to make sqrt

    public LODInfo[] detailLevels;
    public static float maxViewDst; //how far viewer can see. intialised with the last number of the detailvalue array

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition; //static so its easier accessable from other classes
    Vector2 viewerPositionOld; //keep track of the viewer position
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;  //chunk MeshDetails

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>(); //helds the non visible. static so terrain chunks can add it self to that list

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold; //last element of detaillevel
        chunkSize = MapGenerator.mapChunkSize - 1;
        //Brush Size cannot be changed, because MeshDetails needs more chunks to work!
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize); //How many chunks are visible


        UpdateVisibleChunks(); //first ones do get one, befor update method looks for changes in viewing distance
    }

    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale; //instead of manual updating like v viewerMoveThreashold or viewer Threshold we can scale the user position

        //so chunks dont update every frame, threshhold distance has to be moved before updating them
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks(); //only if is true, only then the visible chunks will be updated
        }
    }

    //update all the terrainchunks within the viewable range
    void UpdateVisibleChunks()
    {

        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(true); // sets chunks inbisible which are not in viewing range
        }
        //terrainChunksVisibleLastUpdate.Clear();  //clears the list: makes chunks outside of viewer invisible

        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize); //Gets Chunk size eg. middle = 0:0, left = -1:0
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)) //dictionary to maintain all of the coordinates and terrain chunks to prevent duplicates
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk(); //if already generated the terrrain chunk on that coord. simply updates it
                }
                else
                {
                    //instantiate a new terrain chunk
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }

            }
        }
    }

    public class TerrainChunk
    {

        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1; //so its impossible to be 0 the first time round so it has to update

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;

            position = coord * size; //find point of a parameter which is closest to another parameter
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk"); //has MeshFilter Component and Mesh Renderer
            meshRenderer = meshObject.AddComponent<MeshRenderer>(); //Addcomponent returns the component which its adds
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material; //adds Material. Can be passt for create a new terrain Chunk

            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent; //gets new transform. parent is not mapgenerator anymore
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false); //default not visible

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived); //position of this chunk, so its not always the same chunk (look at centre)
        }

        //stores mapData, when received
        void OnMapDataReceived(MapData mapData)
        {
            //for tests
            //mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
            // print("Map data received");
            //We dont use a plane. wie use a mesh. so wie dont need to modify the scale
            this.mapData = mapData;
            mapDataReceived = true;

            //Gets the texture working on updateChunks
            Texture2D texture = TextureGenerator.ColourMapTexture(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }



        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition)); //SqrDistance returns the smallest given point between the given point and this bounding box. sqrt reverts sqr
                bool visible = viewerDstFromNearestEdge <= maxViewDst;  //viewerdistance: when its visible

                //look at the distance of the viewer from the nearest edge and compares it to the distance threshold of each of the detaillevels to determine which one to display
                if (visible)
                {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold) //its not needed to look at the last one, because bool visible = false, because viewerDstFromNearestEdge > maxViewDst
                        {
                            lodIndex = i + 1; //lod is the next one
                        }
                        else
                        {
                            break; //if not greater and its the correct one, break out of loop
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];  //the lodMesh to work with
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        } 
                        else if (!lodMesh.hasRequestedMesh) //it lodMesh hat NOT yet requested, it will requested and past in the mapData
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    terrainChunksVisibleLastUpdate.Add(this); //if its visible, it adds it self to the list
                }

                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        //not used?
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

    }

    //each Terrain chunk has an array of LODMesh. Fetching its Mesh from the MeshingGenerator
    class LODMesh
    {

        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback; //so it doesnt have to be manuelly updated when we receive mapdata/meshdata

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback(); //when we receive the mapdata, update Callback. //call it as soon as the MeshdataReceived, so we dont have any sort of delay
        }

        //we already request LODMesh whens its required
        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }

    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDstThreshold; //Distance within the lod is active. if he is outside it will change to the next level
    }

}
