using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Loading;
using UnityEngine;
using Random=UnityEngine.Random;
using UnityEngine.UI;
using static getStatic.WorldManager;
using Unity.Mathematics;
using System.Linq;

namespace getStatic {
public class WorldManager : MonoBehaviour {

    public static bool Initialized = false;

    // Graphics settings
    public int currRenderSetting, prevCRS;
    public renderSetting[] renderSettings;
    [System.Serializable]
    public struct renderSetting{
        public string settingName;
        public int setMapSize, setChunkSize, setObjSize, maxCamDist, pushDist;
        public Color warningColor;
    };
    public int clampFPS;
    
    // Randomizer
    public int Seed = 999999;
    public bool RandomGeneration = false;
    static Vector2 perlinOffset;

    // Cell info
    public static int MapSize = 160;
    public static int PushDist = 2;
    public static readonly int[] ParseSpeed = {100, 1000};
    public static Cell[,] Loaded;
    public static Vector2 currPos;
    Vector2 prevPos;

    public class Cell{
        Vector2Int Pos;
        public bool isWater;
        public float Height;
        public tileData ground;
        public float biomeSaturation;
        public tileObject cellObject;
        public Cell(Vector2Int sPos){
            Pos = sPos;
            setData();
        }
        public void pushPos(Vector2 newP){ Pos += new Vector2Int((int)newP.x, (int)newP.y); }
        public Vector2 getPos(){ return new(Pos.x, Pos.y); }
        void setData(){
            float checkWater = getWater(new(Pos.x, Pos.y));
            if(checkWater <= 0f){
                isWater = true;
                Height = -checkWater;
            } else {
                isWater = false;
                Height = checkWater;
                ground = loadedTiles[(int)getBiome(new(Pos.x, Pos.y)).x];
                biomeSaturation = getBiome(new(Pos.x, Pos.y)).y;
                if(ground.chanceFO.Length > 0 && ground.chanceFO[0] > 0f) cellObject = checkForObject(this);
                else cellObject = null;
            }
        }
    };

    // Loader
    public static Vector2 loadPos = Vector2.zero;
    public static Cell[,] newCache;
    public static int[] loadChunk = {0, 1};
    public static int chungID;
    // Loader

    // References
    Camera MainCamera;
    [SerializeField] DrawBase drawingMechanism;
    public Transform POV;
    float POVscroll = 10f;

    // Big textures
    public Transform Skybox;
    public Transform Water;
    Material[] waterColors;

    // Map tiles sprites
    public tileData[] tilesToLoad;
    public static tileData[] loadedTiles;
    //public static pixelMap[] pixelMaps;

    [System.Serializable]
    public class tileData{
        public int tileID, saturationMethod;
        public string tileName;
        public Color32 tileColor;
        public Texture2D[] tileTextures;
        public int[] copyTextures;
        public pixelMap acquiredTiles;
        public int[] possibleObjects;
        public float[] chanceFO;
    }

    public struct pixelMap{
        public pixelList[] acquiredTiles;
        public int[] properTiles;

        public struct pixelList{
            public Color32[] Data;
            public pixelList(Color32[] setData){ Data = setData;}
        }

        public void setUp(tileData template){
            acquiredTiles = new pixelList[template.tileTextures.Length];
            properTiles = new int[template.tileTextures.Length];
            int checkCopy = 0;
            for (int gt = 0; gt < template.tileTextures.Length; gt++){
                if(template.tileTextures[gt] != null) {
                    acquiredTiles[gt] = new (template.tileTextures[gt].GetPixels32());
                    properTiles[gt] = gt;
                } else {
                    acquiredTiles[gt] = new (template.tileTextures[template.copyTextures[checkCopy]].GetPixels32());
                    properTiles[gt] = template.copyTextures[checkCopy];
                    checkCopy++;
                }
            }
        }
    }

    public static Color32[] getTM(int biomeID, int mapID = 0){
        pixelMap got = loadedTiles[biomeID].acquiredTiles;
        return got.acquiredTiles[mapID].Data;
    }

    public static Color32[] getTM(int biomeID, float mapLerp){
        mapLerp = Mathf.Clamp(mapLerp, 0f, 1f);
        pixelMap got = loadedTiles[biomeID].acquiredTiles;
        return got.acquiredTiles[got.properTiles[(int)(mapLerp * (got.acquiredTiles.Length-.1f))]].Data;
    }
    // Map tiles sprites

    // Map objects
    public tileObject[] objectsToLoad;
    public static tileObject[] loadedObjects;
    [System.Serializable]
    public class tileObject{
        public int objectID;
        public string objectName;
        public Texture2D objectSprite;
        public int[] objTra = {0,0,0,0};
        public Vector2 getScale(){ return new Vector2(objTra[0]/32, objTra[1]/32); }
        public Vector2 getPivot(ref Transform POV){ 
            return objTra[2]/32 * POV.right + objTra[3]/32 * POV.up; 
        }
        public float getZ(Vector3 up, Vector3 pos, float dist){
            return ((dist/2f + Vector3.Dot(up, pos)) / dist) + 1f;
        }
        public Texture2D getTexture(){ return objectSprite; }
    }
    // Map objects

    void Start(){
        Application.targetFrameRate = clampFPS;
        MainCamera = POV.GetComponent<Camera>();
        waterColors = new Material[2];
        waterColors = new Material[]{
            waterColors[0] = Water.GetComponent<MeshRenderer>().material,
            waterColors[1] = Water.GetChild(0).GetComponent<MeshRenderer>().material
        };
    }

    public void Initialize(){

        loadedTiles = new tileData[tilesToLoad.Length];
        tilesToLoad.CopyTo(loadedTiles, 0);
        for(int setup = 0; setup < loadedTiles.Length; setup++) loadedTiles[setup].acquiredTiles.setUp(loadedTiles[setup]);
        loadedBiomes = new biomeData[biomesToLoad.Length];
        biomesToLoad.CopyTo(loadedBiomes, 0);
        loadedObjects = new tileObject[objectsToLoad.Length];
        objectsToLoad.CopyTo(loadedObjects, 0);

        tilesToLoad = new tileData[0];
        biomesToLoad = new biomeData[0];
        
        if (RandomGeneration) Seed = Random.Range(1, 999999);
        Random.InitState(Seed);
        perlinOffset = new Vector2(Random.value* 999999f, Random.value * 999999f);
        currPos = Vector3.one*9999f;
        drawingMechanism.initializeSystem();
        drawingMechanism.beginLoad(Vector3.zero);

        Initialized = true;

    }

    void Update(){

        if(Initialized){
            checkForChunkUpdate();
            Stats.text = statsUpdate();
        } else {
            if(prevCRS != currRenderSetting){
                prevCRS = currRenderSetting;
                MapSize = renderSettings[prevCRS].setMapSize;
                PushDist = renderSettings[prevCRS].pushDist;
                drawingMechanism.updateSettings( renderSettings[prevCRS]);
            }
            //if (Input.GetKeyDown(KeyCode.Space)) {
            //    Initialize();
            //}
        }
    }

    void LateUpdate(){
        setBigTextures();
    }

    void setBigTextures(){
        // Skybox
        float skySize = MainCamera.orthographicSize;
        Skybox.position = new Vector3(POV.position.x, POV.position.y, 0f) - (POV.up * (19.2f * skySize/100f));
        Skybox.localScale = Vector3.one * skySize/5f;

        // Water
        Vector3 waves = new Vector3(Mathf.Sin(Time.timeSinceLevelLoad/10f), Mathf.Cos(Time.timeSinceLevelLoad/15f)) * 5f;
        Water.position = new Vector3(Mathf.Ceil(POV.position.x/32f)*32f - 16f, Mathf.Ceil(POV.position.y/32f)*32f - 16f, 5f) + waves;
        float sunAngle = Mathf.Sin((POV.eulerAngles.z/360f) * Mathf.PI);
        waterColors[0].color = Color.black * sunAngle;
        waterColors[1].color = Color.white * (1f-sunAngle);
    }

    float psLerp = 0f;
    void checkForChunkUpdate(){
        psLerp = (Vector2.Distance(currPos, POV.position) - PushDist) / ((MapSize/2f) - PushDist);

        if(loadChunk[0] < loadChunk[1]) {
            int ps = (int)Mathf.Lerp(ParseSpeed[0], ParseSpeed[1], psLerp);
            for (int ql = Mathf.Clamp(ps, 0, loadChunk[1]-loadChunk[0]); ql > 0; ql--) {
                drawingMechanism.load(loadChunk[0], ql);
                loadChunk[0]++;
            }
        } else if (loadChunk[0] == loadChunk[1]) {
            loadChunk[0] = loadChunk[1]+1;
        }

        if((Mathf.Abs(POV.position.x-prevPos.x) > PushDist || Mathf.Abs(POV.position.y-prevPos.y) > PushDist) && loadChunk[0] > loadChunk[1]){
            drawingMechanism.beginLoad(POV.position);
            prevPos = POV.position;
        }
    }
    // Cell operators

    // Stats monitor
    public Text Stats;
    string statsUpdate(){
        ChunkedTextureDB fcuk = GameObject.FindObjectOfType<ChunkedTextureDB>();
        string result = "FPS: " + (1f/Time.unscaledDeltaTime).ToString();
        if(loadChunk[0] < loadChunk[1]) result += "\nLoading tiles: " + loadChunk[0] + "/" + loadChunk[1] + " speed: " + (int)Mathf.Lerp(ParseSpeed[0], ParseSpeed[1], psLerp) + "/" + ParseSpeed[1];
        else result += "\nMap loaded";
        result += "\nLoad instance " + chungID + "\n\n" + "\nLoad offset: " + loadPos + "\nWorld offset: " + currPos + "\nCamera offset: " + POV.transform.position;
        result += "\n\nMap size: " + MapSize + "\nRefresh trigger: " + PushDist + "\nRefresh speed: " + ParseSpeed;
        return result;
    }
    // Stats monitor

    // World generation values
    static float[] islandMargin = {1f, -1f, 200};
    static float islandSize = 20;
    public static float[] biomeSize = {4000, 20f, 0f}; // biome size, biome diversity size, is world infinited
    public static float getHeight(Vector2 tilePos){
        return Mathf.Pow( erode((tilePos.x * 0.333f + perlinOffset.x) / islandSize, (tilePos.y * 0.333f + perlinOffset.y) / islandSize, 5f) , 2f );
    }

    public static float[] continentMargin = {-20f, 20f, 5f};
    public static float[] worldSize = {40000f, 40000f, 0f};
    public static float getContinent(Vector2 tilePos){
        float density = worldSize[1]/continentMargin[2];
        Vector2 st = new(tilePos.x + perlinOffset.x, tilePos.y + perlinOffset.y);
        float land = erode((st.x-worldSize[0]) / density, (st.y-worldSize[1]) / density, 5f);//, new[]{0f, density/4f});
        if(Mathf.Abs(tilePos.x) > worldSize[0]/2f || Mathf.Abs(tilePos.y) > worldSize[1]/2f) land *= worldSize[2];
        return Mathf.Lerp( continentMargin[0], continentMargin[1], land);
    }

    public static float getContinentNormalized(Vector2 tilePos){
        float density = worldSize[1]/continentMargin[2];
        Vector2 st = new(tilePos.x + perlinOffset.x, tilePos.y + perlinOffset.y);
        float land = erode((st.x-worldSize[0]) / density, (st.y-worldSize[1]) / density, 5f);//, new[]{0f, density/4f});
        if(Mathf.Abs(tilePos.x) > worldSize[0]/2f || Mathf.Abs(tilePos.y) > worldSize[1]/2f) land *= worldSize[2];
        return land;
    }

    public static float getLand(Vector2 tilePos){
        Vector2 st = tectonic((tilePos.x + perlinOffset.x) / islandMargin[2], (tilePos.y + perlinOffset.y) / islandMargin[2], new[]{10f, 10f});
        float land = Mathf.Pow( erode(st.x, st.y, 5f) , 2f );
        return Mathf.Clamp( Mathf.Lerp(islandMargin[0], islandMargin[1], land * getContinent(tilePos)) , 0f, 1f);
    }

    public static float getLandNormalized(Vector2 tilePos){
        Vector2 st = tectonic((tilePos.x + perlinOffset.x) / islandMargin[2], (tilePos.y + perlinOffset.y) / islandMargin[2], new[]{10f, 10f});
        float land = Mathf.Pow( erode(st.x, st.y, 5f) , 2f );
        return Mathf.Clamp( Mathf.Lerp(0f, 1f, land * getContinent(tilePos)) , 0f, 1f);
    }

    [System.Serializable]
    public class biomeData{
        public int biomeID;
        public string biomeName;
        public Color32 biomeColor;
        public float coastline;
        public int[] availableGroundTiles;
        public int partitionMethond = 1;
    }

    public biomeData[] biomesToLoad;
    public static biomeData[] loadedBiomes;

    public static Vector3 getBiome(Vector2 tilePos) {
        float[] erosionFactors = {
            erodeTectonics((tilePos.x + perlinOffset.x) / biomeSize[0], (tilePos.y + perlinOffset.y) / biomeSize[0], 10f, new float[]{biomeSize[0], biomeSize[0]}),
            0f
        };
        erosionFactors[0] *= getContinentNormalized(tilePos);
        float sector = Mathf.Clamp(erosionFactors[0] * 2f * loadedBiomes.Length - 0.1f, 0f, loadedBiomes.Length - 0.1f);
        biomeData bd = loadedBiomes[(int)sector];
        int aot = bd.availableGroundTiles.Length;
        float coastline = bd.coastline;
        float partition;
        float water = getWater(tilePos);//Mathf.Clamp(getWater(tilePos, 1) * aot * 2, 0f, aot - 1);
        if (water < .2f) {
            partition = water * 5f * coastline;
        } else {
            switch(bd.partitionMethond){
                case 2: erosionFactors[1] = sector%1f * 2f % 1f; break;
                default: erosionFactors[1] = erode((tilePos.y + perlinOffset.y) / biomeSize[1], (tilePos.x + perlinOffset.x) / biomeSize[1], 1f); break;
            }
            partition = Mathf.Lerp(coastline, aot-.1f, erosionFactors[1]);//coastline + (erosionFactors[1] * (aot - 0.1f - coastline)); // may be clamped
        }

        float saturation;
        float partitionFraction = partition % 1f;
        int partitionIndex = (int)partition;

        switch (loadedTiles[bd.availableGroundTiles[partitionIndex]].saturationMethod) {
            case 2:
                saturation = Mathf.PerlinNoise((tilePos.y + perlinOffset.y) / 8f, (tilePos.x + perlinOffset.x) / 8f) % 1f;
                break; // flower field
            case 1:
                saturation = Mathf.Abs(Mathf.Sin(partitionFraction * Mathf.PI));
                break; // sinus proximity
            default:
                saturation = partitionFraction;
                break; // proximity
        }

        return new Vector3(bd.availableGroundTiles[partitionIndex], saturation, (int)sector);
    }

    public static float getBiomeQuick(Vector2 tilePos){
        float erosionFactor = erodeTectonics((tilePos.x + perlinOffset.x) / biomeSize[0], (tilePos.y + perlinOffset.y) / biomeSize[0], 10f, new float[]{biomeSize[0], biomeSize[0]});
        erosionFactor *= getContinentNormalized(tilePos);
        float sector = Mathf.Clamp(erosionFactor * 2f * loadedBiomes.Length - 0.1f, 0f, loadedBiomes.Length - 0.1f);
        return sector;
    }

    public static tileObject checkForObject(Cell target){
        tileData ground = target.ground;
        float Length = ground.possibleObjects.Length;
        float choosen = Mathf.Clamp(Mathf.PerlinNoise(target.getPos().y / ground.chanceFO[1], target.getPos().x / ground.chanceFO[1]) * Length* 1.5f, 0f, Length-.1f);
        float chance = erode(target.getPos().x / ground.chanceFO[1], target.getPos().y / ground.chanceFO[1], 10f);
        if (1f - chance <= ground.chanceFO[0]) return loadedObjects[ground.possibleObjects[(int)choosen]];
        else return null;
    }

    public static float riverDensity = 10000f;
    public static float[] riverMargin = {0.45f, 0.5f, 0.05f};
    public static float riverBias(Vector2 Pos){
        float riverBase = erodeTectonics((Pos.y - perlinOffset.x) / riverDensity, (Pos.x - perlinOffset.y) / riverDensity, 10f, new[]{100f, 200f});
        if(riverBase >= riverMargin[0] && riverBase <= riverMargin[1]) return Mathf.Pow( Mathf.Sin((riverBase-riverMargin[0]) / riverMargin[2] * Mathf.PI) , 3f);
        else return 0f;
    }
    public static float getWater(Vector2 Pos, int nega = -1){
        float perlin = getHeight(new(Pos.x, Pos.y));
        float islandM = getLand(new(Pos.x, Pos.y)) + Mathf.Lerp(riverBias(Pos), 0f, perlin/2f);
        if(perlin < islandM) return perlin / (islandM*nega);
        else return (perlin - islandM) / (1f - islandM);
    }

    public static float erode(float x, float y, float p = 10f){
        float normal = Mathf.PerlinNoise(x, y);
        float eroded = Mathf.PerlinNoise(x * p, y * p);
        float mask = Mathf.PerlinNoise(y / 3f, x / 3f);
        return Mathf.Clamp( Mathf.Lerp(normal, eroded, mask/2f) , 0f, 1f );
    }

    public static Vector2 tectonic(float x, float y, float[] size){
        float mask = Mathf.Clamp(Mathf.PerlinNoise(x / size[1], y / size[1]), 0f, 1f);
        return Vector2.Lerp(new(x, y), new(x+size[0], y+size[0]), mask);
    }

    public static float tectonicFloat(float x, float y, float[] size){
        float mask = Mathf.Clamp(Mathf.PerlinNoise(x / size[1], y / size[1]), 0f, 1f);
        Vector2 getP = Vector2.Lerp(new(x, y), new(x+size[0], y+size[0]), mask);
        return Mathf.PerlinNoise(getP.x, getP.y);
    }

    public static float erodeTectonics(float x, float y, float power, float[] shift){
        Vector2 shi = tectonic(x, y, shift);
        return erode(shi.x, shi.y, power);
    }

    public static float erodeSaturated(float x, float y, float p = 10f, float offset = 1f){
        float normal = Mathf.Clamp(offset/-2f + (Mathf.PerlinNoise(x, y) * offset*2f), 0f, 1f);
        float eroded = Mathf.Clamp(offset/-2f + (Mathf.PerlinNoise(x*p, y*p) * offset*2f), 0f, 1f);
        float mask = Mathf.PerlinNoise(y / 3f, x / 3f);
        return Mathf.Clamp( Mathf.Lerp(normal, eroded, mask/2f) , 0f, 1f );
    }

    public static float saturatedTectonics(float x, float y, float power, float[] shift){
        Vector2 shi = tectonic(x, y, shift);
        return Mathf.Clamp(saturatedPerlin(shi.x, shi.y, power), 0f, 1f);
    }
    // World generation values

    public static float saturatedPerlin(float x, float y, float offset){
        float val = Mathf.PerlinNoise(x, y);
        return  Mathf.Clamp(val + offset*val, 0f, 1f);
    }

}
}
