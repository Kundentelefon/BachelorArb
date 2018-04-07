using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        MixedHeightMap,
        ColourMap,
        Mesh,
        Crater,
        CraterRing,
        CraterFalloff,
        CraterStripes,
        CraterSinW,
        CraterQuadFalloff,
        CraterSidedParable,
        CraterCentralPeak,
        CraterTerrace,
        CraterPseudoRnd,
    };
    public DrawMode drawMode;

    public NoiseGenerator.NormalizeMode normalizeMode;

    public const int mapChunkSize = 239; //less than 255^2: w-1 = 240: 240 has properties of 2,4,6,8,10,12: -2 because of border vertices
    [Range(0, 6)] //makes it to slider
    public int LOD; //lod only for editor

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
    [Range(0,1f)]
    public float terrainHeight;
    public bool cleanChunks;

    [Header("Perlin Noise Generator Settings")]
    public float noiseScale;

    public int octaves;
    [Range(0, 1)]
    public float persistance; //decreas in amplitude of octaves. how small these features the whole map changes
    public float lacunarity; //frequencys of octaves. increases number of small features

    public int seed;
    public Vector2 offset;

    [Header("Graininess")]
    [Range(0, 0.1f)]
    public float pseudoIntensity;
    public float pseudoPeaks;
    public float pseudoDensity;
    public float pseudoVal;

    [Header("Single Crater Settings")]
    public int Cseed;
    [Range(0, 100)]
    public int craterProbability;

    [Range(0, 2)]
    public float craterSize;
    [Range(0, 10)] //exponent: everything above or below will crash unity
    public float craterIntensity;
    [Range(0, 100)]
    public float NoiseIntensity;

    public Vector2 position;
    public Vector2 ellipse;

    public RockTypes[] rockLevels;

    public CraterType[] craters;

    public bool defaultCrater;

    float[,] craterMap; //stores craterMap
    float[,] craterRing;
    float[,] craterFalloff;
    float[,] craterStripes;
    float[,] craterQuadFalloff;
    float[,] craterSinW;
    float[,] craterSidedParable;
    float[,] craterPseudoRnd;
    float[,] craterCentralPeak;
    float[,] craterTerrace;

    [Header("Randomizer")]
    public bool activateRandomizer;
    //not yet integrated
    public enum TypeOfTerrain
    {
        ownType ,
        Earthlike,
        Moonlike
    };
    public TypeOfTerrain terrainType;

    [Range(1,3)]
    public int terrainTypeNr;

    [Header("Settings for Own Type")]
    public float size;
    public float age;
    public float angle;
    public float terrainBrittleness;
    public float gravitation;
    public Vector2 tectonicPlateMovement;
    
    public bool autoUpdate;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueueNr = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueueNr = new Queue<MapThreadInfo<MeshData>>();

    private int craterTypeNr;

    private int craterSeed;

    //use FallowMap
    void Awake()
    {
        //falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
        //craterMap = CraterGenerator.GenerateCrater(mapChunkSize + 2);
    }

    public void DrawMapInEditor()
    {
        craterTypeNr = RandomizerType();

        MapData mapData = GenerateMapData(Vector2.zero);

        MapDisplay display = FindObjectOfType<MapDisplay>(); //reference to mapdisplay, gives different options
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(NoiseGenerator.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, seed, noiseScale, octaves, persistance, lacunarity, Vector2.zero + offset, normalizeMode)));
        }
        if (drawMode == DrawMode.MixedHeightMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            display.DrawTexture(TextureGenerator.ColourMapTexture(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, LOD),
                TextureGenerator.ColourMapTexture(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Crater)
        {
            display.DrawTexture(
                TextureGenerator.TextureFromHeightMap(CraterGenerator.GenerateCrater(mapChunkSize, craterSize, craterIntensity, position.x, position.y,
                    ellipse.x, ellipse.y)));
        }
        else if (drawMode == DrawMode.CraterRing)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(CraterRingGenerator.GenerateCraterRing(mapChunkSize, craterSize + craters[craterTypeNr].ringWidth,
            craterIntensity + craterSize + craters[craterTypeNr].ringWeight, position.x, position.y, ellipse.x, ellipse.y)));
        }
        else if (drawMode == DrawMode.CraterFalloff)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(CraterFalloffGenerator.GenerateCraterFalloff(mapChunkSize, craterSize + craters[craterTypeNr].ringWidth + craters[craterTypeNr].falloffstart,
            craterIntensity + craterSize + craters[craterTypeNr].falloffWeight, position.x, position.y, ellipse.x, ellipse.y)));
        }
        else if (drawMode == DrawMode.CraterStripes)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(CraterStripesGenerator.GenerateCraterStripes(mapChunkSize, craters[craterTypeNr].stripeSin, craters[craterTypeNr].stripeQuantity)));
        }
        else if (drawMode == DrawMode.CraterSinW)
        {
            display.DrawTexture(
                TextureGenerator.TextureFromHeightMap(CraterSinW.GenerateCraterSinW(mapChunkSize, craters[craterTypeNr].sinWCentress, position.x, position.y, ellipse.x, ellipse.y, craters[craterTypeNr].sinWQuantity)));
        }
        else if (drawMode == DrawMode.CraterQuadFalloff)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(CraterQuadFalloffGenerator.GenerateCraterQuadFalloff(mapChunkSize, position.x, position.y, craters[craterTypeNr].lineStart, 0)));
        }
        //else if (drawMode == DrawMode.CraterSidedParable)
        //{
        //    display.DrawTexture(TextureGenerator.TextureFromHeightMap(CraterSidedParable.GenerateCraterSidedParable(mapChunkSize, craters[craterTypeNr].diagScale, craters[craterTypeNr].diagParable, craters[craterTypeNr].diagDirection)));
        //}
        else if (drawMode == DrawMode.CraterCentralPeak)
        {
            display.DrawTexture(
                TextureGenerator.TextureFromHeightMap(CraterCentralPeak.GenerateCreaterCentralPeak(mapChunkSize, craters[craterTypeNr].centralSize, craters[craterTypeNr].centralPeakness, position.x + craters[craterTypeNr].centralPosition.x, position.y + craters[craterTypeNr].centralPosition.y, ellipse.x,
            ellipse.y)));
        }
        else if (drawMode == DrawMode.CraterTerrace)
        {
            display.DrawTexture(
                TextureGenerator.TextureFromHeightMap(CraterTerrace.GenerateCraterTerrace(mapChunkSize, craters[craterTypeNr].terraceSize, craters[craterTypeNr].terracePeakness, position.x + craters[craterTypeNr].terracePosition.x, position.y + craters[craterTypeNr].terracePosition.y, ellipse.x,
            ellipse.y)));
        }
        else if (drawMode == DrawMode.CraterPseudoRnd)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(CraterPseudoRndGenerator.GenerateCraterPseudoRnd(mapChunkSize, pseudoDensity, pseudoPeaks, pseudoVal)));
        }
    }

    //Threadening
    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callback); //centre so its not always the same chunk
        };

        new Thread(threadStart).Start();
    }

    //Threadening
    void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(centre);
        lock (mapDataThreadInfoQueueNr) //lock: so no other thread cann access this thread
        {
            mapDataThreadInfoQueueNr.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate  //Meshdata Thread
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    //Gets the Height Map from GeneratteTerrainmesh
    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQueueNr)
        {
            meshDataThreadInfoQueueNr.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update()
    {
        if (mapDataThreadInfoQueueNr.Count > 0) //as long there are threads
        {
            for (int i = 0; i < mapDataThreadInfoQueueNr.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueueNr.Dequeue(); //Dequeue: the next thing in the queue
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueueNr.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueueNr.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueueNr.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    public MapData GenerateMapData(Vector2 centre)
    {
        craterTypeNr = RandomizerType();

        float[,] map = new float[mapChunkSize + 2, mapChunkSize + 2];

        //fetching 2D NoiseMap from the NoiseGenerator Class
        // +2 for the border vertices. generates 1 extra noise value on left and right side
        float[,] noiseMask = NoiseGenerator.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, seed, noiseScale, octaves, persistance, lacunarity, centre + offset, normalizeMode);
        float[,] noiseMaskMOD = NoiseGenerator.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, seed, noiseScale+10, octaves+2, persistance+0.3f, lacunarity+5, centre + offset, normalizeMode);

        float ringWeightRND = craters[craterTypeNr].ringWeight;
        float ringWidthRND = craters[craterTypeNr].ringWidth;
        //float falloffIntensityRND = craters[craterTypeNr].falloffIntensity;
        float falloffWeightRND = craters[craterTypeNr].falloffWeight;
        float falloffstartRND = craters[craterTypeNr].falloffstart;
        float stripeIntensityRND = craters[craterTypeNr].stripeIntensity;
        float stripeSinRND = craters[craterTypeNr].stripeSin;
        float stripeQuantityRND = craters[craterTypeNr].stripeQuantity;
        float sinWIntensityRND = craters[craterTypeNr].sinWIntensity;
        float sinWQuantityRND = craters[craterTypeNr].sinWQuantity;
        float sinwCentressRND = craters[craterTypeNr].sinWCentress;
        float lineStartRND = craters[craterTypeNr].lineStart;
        
        float complexHeight = 0.6f;
        float complexNoiseMod = 0.01f;
        float complexcentral = 0.1f;
        float complexProbability = 0f;

        bool complexTrue = false;
        //Randomizes generated Crater in Play Mode
        System.Random craterRNG = new System.Random();
        if (activateRandomizer)
        {
            ringWeightRND = (float)craterRNG.Next(0, 40) / 100;
            ringWidthRND = (float)craterRNG.Next(-2, 8) / 100;
            //falloffIntensityRND = (float) craterRNG.Next(0, 600)/100;
            //falloffWeightRND = (float)craterRNG.Next(10, 600) / 100;
            //falloffstartRND = (float)craterRNG.Next(0, 7) / 100;
            stripeIntensityRND = (float)craterRNG.Next(0, 200) / 100;
            stripeSinRND = (float) craterRNG.Next(0, 200)/100;
            //stripeQuantityRND = (float)craterRNG.Next(100, 400) / 100;
            //sinWIntensityRND = (float)craterRNG.Next(100, 300)/100;
            sinWQuantityRND = (float) craterRNG.Next(0, 130)/1000;
            //lineStartRND = (float)craterRNG.Next(30, 200) / 100;
            sinwCentressRND = (float) craterRNG.Next(0, 200)/100;

            complexHeight = (float) craterRNG.Next(57, 80)/100;
            complexNoiseMod = (float) craterRNG.Next(0, 10)/1000;
            complexcentral = (float) craterRNG.Next(0, 10)/100;

            //probabiltiy complex craters
            complexProbability = (float)craterRNG.Next(0, 10);
            if (complexProbability < 2)
                complexTrue = true;
        }

        



        //float[,] ringMask = CraterRingGenerator.GenerateCraterRing(mapChunkSize, craterSize + ringWidthRND,
        //    craterIntensity + craterSize + ringWeightRND, position.x, position.y, ellipse.x, ellipse.y);

        //float[,] ringMask = craterRing;

        craterMap = CraterGenerator.GenerateCrater(mapChunkSize, craterSize, craterIntensity, position.x, position.y, ellipse.x, ellipse.y);

        craterRing = CraterRingGenerator.GenerateCraterRing(mapChunkSize, craterSize + ringWidthRND,
            craterIntensity + craterSize + ringWeightRND, position.x, position.y, ellipse.x, ellipse.y);

        craterFalloff = CraterFalloffGenerator.GenerateCraterFalloff(mapChunkSize, craterSize + ringWidthRND + falloffstartRND,
            craterIntensity + craterSize + falloffWeightRND, position.x, position.y, ellipse.x, ellipse.y);

        craterStripes = CraterStripesGenerator.GenerateCraterStripes(mapChunkSize, stripeSinRND,
            stripeQuantityRND);

        craterSinW = CraterSinW.GenerateCraterSinW(mapChunkSize, sinwCentressRND, position.x, position.y, ellipse.x, ellipse.y,
            sinWQuantityRND);

        craterQuadFalloff = CraterQuadFalloffGenerator.GenerateCraterQuadFalloff(mapChunkSize, position.x, position.y, lineStartRND, 0);

        craterCentralPeak = CraterCentralPeak.GenerateCreaterCentralPeak(mapChunkSize, craters[craterTypeNr].centralSize,
            craters[craterTypeNr].centralPeakness, position.x + craters[craterTypeNr].centralPosition.x,
            position.y + craters[craterTypeNr].centralPosition.y, ellipse.x,
            ellipse.y);

        craterTerrace = CraterTerrace.GenerateCraterTerrace(mapChunkSize, craters[craterTypeNr].terraceSize, craters[craterTypeNr].terracePeakness,
            position.x + craters[craterTypeNr].terracePosition.x, position.y + craters[craterTypeNr].terracePosition.y, ellipse.x,
            ellipse.y);


        //Random percentage to get a crater
        System.Random rnd = new System.Random(); 
        int rndFall = rnd.Next(1, 100);

        //all randomizer
        //if (activateRandomizer)
        //{
        //    craters[craterTypeNr].ringWeight = (float)craterRNG.Next(0, 5) / 10;

        //    craters[craterTypeNr].ringWidth = (float)craterRNG.Next(-1, 1) / 10;

        //    craters[craterTypeNr].falloffstart = (float)craterRNG.Next(0, 1) / 10;
        //    craters[craterTypeNr].falloffWeight = (float)craterRNG.Next(5, 50) / 10;
        //    craters[craterTypeNr].falloffIntensity = (float)craterRNG.Next(0, 50) / 10;

        //    craters[craterTypeNr].stripeIntensity = (float)craterRNG.Next(0, 5) / 10;
        //    craters[craterTypeNr].stripeSin = (float)craterRNG.Next(0, 50) / 10;
        //    craters[craterTypeNr].stripeQuantity = (float)craterRNG.Next(20, 50) / 10;
        //    craters[craterTypeNr].stripeWidth = (float)craterRNG.Next(0, 2) / 10;
        //    craters[craterTypeNr].sinWIntensity = (float)craterRNG.Next(1, 3);
        //    craters[craterTypeNr].sinWCentress = (float)craterRNG.Next(0, 10) / 10;

        //    craters[craterTypeNr].sinWQuantity = (float)craterRNG.Next(0, 200) / 100;
        //    craters[craterTypeNr].sinWWidth = (float)craterRNG.Next(20, 50) / 10;

        //    craters[craterTypeNr].lineStart = (float)craterRNG.Next(0, 50) / 10;
        //    craters[craterTypeNr].lineIntensity = (float)craterRNG.Next(0, 10) / 10;

        //    craters[craterTypeNr].centralIntensity = (float)craterRNG.Next(0, 50) / 10;

        //    craters[craterTypeNr].centralNoise = (float)craterRNG.Next(0, 5) / 10;
        //    craters[craterTypeNr].centralHeight = (float)craterRNG.Next(0, 5) / 10;
        //    craters[craterTypeNr].centralSize = (float)craterRNG.Next(0, 20) / 10;
        //    craters[craterTypeNr].centralPeakness = (float)craterRNG.Next(0, 100) / 100;
        //    //craters[craterTypeNr].centralPosition = prng.Next(0, 5);

        //    craters[craterTypeNr].terraceIntensity = (float)craterRNG.Next(0, 20) / 100;

        //    craters[craterTypeNr].terraceNoise = (float)craterRNG.Next(0, 5) / 10;
        //    craters[craterTypeNr].terraceHeight = (float)craterRNG.Next(0, 5) / 10;
        //    craters[craterTypeNr].terraceSize = (float)craterRNG.Next(0, 20) / 10;
        //    craters[craterTypeNr].terracePeakness = (float)craterRNG.Next(0, 5) / 10;
        //    //craters[craterTypeNr].terracePosition = prng.Next(0, 5);


        //    craters[craterTypeNr].ringIntensity = 0;
        //}
        
        //generate 1D Colormap from 2D Noisemap
        Color[] colourMap = new Color[(mapChunkSize + 2) * (mapChunkSize + 2)];
        for (int y = 0; y < mapChunkSize; y++)
        {

            for (int x = 0; x < mapChunkSize; x++)
            {
                
                //float ringMask = Mathf.Clamp01(craters[craterTypeNr].ringIntensity * craterRing[x, y]);
                float falloffMask = Mathf.Clamp01(craters[craterTypeNr].falloffIntensity * craterFalloff[x, y]);
                float stripeMask = Mathf.Clamp01(stripeIntensityRND * craterStripes[x, y] - craterRing[x, y] * craters[craterTypeNr].stripeWidth);
                float sinusMask = Mathf.Clamp01(sinWIntensityRND * craterSinW[x, y] - craterRing[x, y] * craters[craterTypeNr].sinWWidth);
                float lineMask = Mathf.Clamp01((craters[craterTypeNr].lineIntensity * (craterQuadFalloff[x, y] - craterFalloff[x, y] * 10)));
                float centralMask =
                    Mathf.Clamp01(-(craterCentralPeak[x, y] - 1) - (craterMap[x, y] - sinusMask) * craters[craterTypeNr].centralIntensity);
                float terraceMask = Mathf.Clamp01((((craterTerrace[x, y])) - craterMap[x, y]) * craters[craterTypeNr].terraceIntensity);
                float pseudoMask = Mathf.Clamp01(pseudoIntensity*craterPseudoRnd[x, y]);

                    if (falloffMask > terrainHeight)
                        falloffMask = terrainHeight;

                    if (centralMask > craters[craterTypeNr].centralHeight)
                        centralMask = craters[craterTypeNr].centralHeight;

                    if (terraceMask > craters[craterTypeNr].terraceHeight)
                        terraceMask = craters[craterTypeNr].terraceHeight;

                //terrainheight if cant be changed so terrainHeight has to be changed here
                map[x, y] = (-(terrainHeight) + 1) + Mathf.Abs(noiseMask[x, y] / 100 * NoiseIntensity) - pseudoMask;

                if (cleanChunks)
                {
                    map[x, y] = craterMap[x, y] - falloffMask - stripeMask + sinusMask + lineMask + centralMask - terraceMask;
                }
                //while looping through noiseMap. use falloff map
                else if (craterProbability >= rndFall)
                {
                    
                    map[x, y] = (craterMap[x, y] - falloffMask - stripeMask + sinusMask + lineMask + centralMask - terraceMask - pseudoMask) + Mathf.Abs(noiseMask[x, y] / 100 * NoiseIntensity);

                    //For Tests: adds additionally cratertype of complex and old craters
                    if (activateRandomizer) {

                        if (complexTrue) { 
                        if (map[x, y] <= complexHeight)
                        map[x, y] = complexHeight + noiseMaskMOD[x,y]* complexNoiseMod + (centralMask* complexcentral * noiseMaskMOD[x,y]*4) ;
                        }
                    }
                }
                float currentHeight = map[x, y];
                for (int i = 0; i < rockLevels.Length; i++)
                {
                    //sections where we actual assigning the colors
                    if (currentHeight >= rockLevels[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = rockLevels[i].colour; //if its greater, then assign the color
                    }
                    else
                    {
                        break; //only break if its less then the rockLevels height
                    }
                }
            }
        
        }
        craterSeed++;
        return new MapData(map, colourMap);
    }

    //call automatical whenever one of scripts variables changes in its vector
    void OnValidate()
    {
        craterTypeNr = RandomizerType();

        //Generates a predefined Crater
        if (defaultCrater)
        {
            craters[craterTypeNr].ringWeight = 6.91f;
            craters[craterTypeNr].ringWidth = 0.04f;
            craters[craterTypeNr].falloffstart = 0.07f;
            craters[craterTypeNr].falloffWeight = 4.08f;
            craters[craterTypeNr].falloffIntensity = 0.82f;
            craters[craterTypeNr].stripeIntensity = 0.381f;
            craters[craterTypeNr].stripeSin = 4.97f;
            craters[craterTypeNr].stripeQuantity = 2.69f;
            craters[craterTypeNr].stripeWidth = 0.35f;
            craters[craterTypeNr].sinWIntensity = 3.11f;
            craters[craterTypeNr].sinWCentress = 1.49f;
            craters[craterTypeNr].sinWQuantity = 0.1f;
            craters[craterTypeNr].sinWWidth = 0.87f;
            craters[craterTypeNr].lineStart = 0.65f;
            craters[craterTypeNr].lineIntensity = 0.15f;
            craters[craterTypeNr].centralIntensity = 0.51f;
            craters[craterTypeNr].centralNoise = 8.2f;
            craters[craterTypeNr].centralHeight = 1.06f;
            craters[craterTypeNr].centralSize = -0.12f;
            craters[craterTypeNr].centralPeakness = -0.02f;
            craters[craterTypeNr].terraceIntensity = 0.21f;
            craters[craterTypeNr].terraceNoise = 0;
            craters[craterTypeNr].terraceHeight = 0.27f;
            craters[craterTypeNr].terraceSize = 0.18f;
            craters[craterTypeNr].terracePeakness = 0.39f;
}

        //wont allow to drop values below that number
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
        if (octaves < 0)
        {
            octaves = 0;
        }
        if (craterSize < 0.01f)
        {
            craterSize = 0.01f;
        }
        if (ellipse.x < 0.01f)
        {
            ellipse.x = 0.01f;
        }
        if (ellipse.y < 0.01f)
        {
            ellipse.y = 0.01f;
        }
        if (craters[craterTypeNr].stripeSin < 0)
        {
            craters[craterTypeNr].stripeSin = 0;
        }
        if (craters[craterTypeNr].sinWIntensity < 0)
        {
            craters[craterTypeNr].sinWIntensity = 0;
        }
        if (craters[craterTypeNr].lineIntensity < 0)
        {
            craters[craterTypeNr].lineIntensity = 0;
        }
        if (craters[craterTypeNr].lineStart < 0)
        {
            craters[craterTypeNr].lineStart = 0;
        }
        if (craters[craterTypeNr].sinWWidth < 0)
        {
            craters[craterTypeNr].sinWWidth = 0;
        }
        if (pseudoPeaks < 0.01f)
        {
            pseudoPeaks = 0.01f;
        }
        if (pseudoIntensity < 0.001f)
        {
            pseudoIntensity = 0.001f;
        }
        if (pseudoVal < 0.01f)
        {
            pseudoVal = 0.01f;
        }
        if (octaves < 1)
        {
            octaves = 1;
        }
        
        //Dont forget to change Draw Mapeditor, too
        

        //craterRing = CraterRingGenerator.GenerateCraterRing(mapChunkSize, craterSize + craters[craterTypeNr].ringWidth,
        //    craterIntensity + craterSize + craters[craterTypeNr].ringWeight, position.x, position.y, ellipse.x, ellipse.y);



        //craterSidedParable = CraterSidedParable.GenerateCraterSidedParable(mapChunkSize, craters[craterTypeNr].diagScale, craters[craterTypeNr].diagParable, craters[craterTypeNr].diagDirection);



        craterPseudoRnd = CraterPseudoRndGenerator.GenerateCraterPseudoRnd(mapChunkSize, pseudoDensity, pseudoPeaks, pseudoVal);
    }

    //Holds callback data and Mapdata info. Make it Generic <T> so it can handle both mesh data and mapdata
    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback; //structs should be unreadably after creations
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }

    }

    public int RandomizerType()
    {
        if (activateRandomizer)
            //craterTypeNr = terrainTypeNr;
        craterTypeNr = 0;
        else
        {
            craterTypeNr = 0;
        }

        return craterTypeNr;
    }

}

//terrain rockLevels for different color for each height.
[System.Serializable]
public struct RockTypes
{
    public string name; // water, grass, rock, etc.
    public float height;
    public Color colour; // color for terrain
}

[System.Serializable]
public struct CraterType
{
    public int craterNr;
    [Header("Ring")]
    public float ringWeight;
    [Range(-0.1f,0.1f)]
    public float ringWidth;
    [Header("Falloff")]
    [Range(-1, 1)]
    public float falloffstart;
    public float falloffWeight;
    public float falloffIntensity;
    [Header("Stripes")]
    [Range(0,0.5f)]
    public float stripeIntensity;
    public float stripeSin;
    public float stripeQuantity;
    public float stripeWidth;
    public float sinWIntensity;
    public float sinWCentress;
    [Range(0,0.2f)]
    public float sinWQuantity;
    public float sinWWidth;
    [Header("QuadMod")]
    //[Range(0,0)]
    //public int lineDirectNSFW; //linedirection does not work atm
    public float lineStart;
    public float lineIntensity;
    //[Range(0, 6)]
    //public int diagDirection;
    //public float diagIntensity;
    //public float diagScale;
    //public float diagParable;
    [Header("Central Peak")]
    [Range(0,1)]
    public float centralIntensity;
    [Range(0, 100)]
    public float centralNoise;
    public float centralHeight;
    public float centralSize;
    public float centralPeakness;
    public Vector2 centralPosition;
    [Header("Terrace")]
    public float terraceIntensity;
    [Range(0,100)]
    public float terraceNoise;
    public float terraceHeight;
    public float terraceSize;
    public float terracePeakness;
    public Vector2 terracePosition;
}

//Gets the heightMap and Colourmap from the GenerateMapMethod
public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}



//Threadening
/*
spread the calculation of the map data and the mash data over multiple frames.
InfiniteTerrain will gets Mapgenerator data. But it wont get immediately because its generated over multiple frames

    MapGenerator: RequestMapdata will take Action<MapData>callback
    2. MapDataThread: add mapData + callback queue. 
    3. outside of the method in the update methode, for(queue. Count>0) as long item is in the queue we call map data

    InfiniteTerrain: RequestMapData(OnMapDataReceived


*/
