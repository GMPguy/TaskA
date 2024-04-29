using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Loading;
using UnityEngine;
using Random=UnityEngine.Random;
using UnityEngine.UI;
using static getStatic.WorldManager;

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
    };
    
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
    [SerializeField] DrawBase drawingMechanism;
    public Transform POV;
    float POVscroll = 10f;

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
        try {
            pixelMap got = loadedTiles[biomeID].acquiredTiles;
            return got.acquiredTiles[got.properTiles[(int)(mapLerp * (got.acquiredTiles.Length-0.1f))]].Data;
        } catch (Exception e){
            Debug.LogError("An error has occured when accesing tile map; maplerp " + mapLerp + ", biomeID " + biomeID + "\n" + e);
            return null;
        }
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
            return (dist/2f + Vector3.Dot(up, pos)) / dist;
        }
        public Texture2D getTexture(){ return objectSprite; }
    }
    // Map objects

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
    static float[] biomeSize = {4000, 20f}; // biome size, biome diversity size
    public static float getHeight(Vector2 tilePos){
        return Mathf.Pow( erode((tilePos.x * 0.333f + perlinOffset.x) / islandSize, (tilePos.y * 0.333f + perlinOffset.y) / islandSize, 5f) , 2f );
    }

    static float[] continentMargin = {-20f, 20f, 5f};
    public static float[] worldSize = {40000f, 40000f, 0.5f};
    public static float getContinent(Vector2 tilePos){
        float density = worldSize[1]/continentMargin[2];
        Vector2 st = new(tilePos.x + perlinOffset.x, tilePos.y + perlinOffset.y);
        float land = erode((st.x-worldSize[0]) / density, (st.y-worldSize[1]) / density, 5f);//, new[]{0f, density/4f});
        if(Mathf.Abs(tilePos.x) > worldSize[0]/2f || Mathf.Abs(tilePos.y) > worldSize[1]/2f) land = 0f;
        return Mathf.Lerp( continentMargin[0], continentMargin[1], land);
    }

    public static float getContinentNormalized(Vector2 tilePos){
        float density = worldSize[1]/continentMargin[2];
        Vector2 st = new(tilePos.x + perlinOffset.x, tilePos.y + perlinOffset.y);
        float land = erode((st.x-worldSize[0]) / density, (st.y-worldSize[1]) / density, 5f);//, new[]{0f, density/4f});
        if(Mathf.Abs(tilePos.x) > worldSize[0]/2f || Mathf.Abs(tilePos.y) > worldSize[1]/2f) land = 0f;
        return Mathf.Lerp( 0f, 1f, land);
    }

    public static float getLand(Vector2 tilePos){
        Vector2 st = tectonic((tilePos.x + perlinOffset.x) / islandMargin[2], (tilePos.y + perlinOffset.y) / islandMargin[2], new[]{10f, 10f});
        float land = Mathf.Pow( erode(st.x, st.y, 5f) , 2f );
        return Mathf.Clamp( Mathf.Lerp(islandMargin[0], islandMargin[1], land * getContinent(tilePos)) , 0f, 1f);
        //return Mathf.Clamp( Mathf.Lerp(islandMargin[0], islandMargin[1], Mathf.Pow( Mathf.PerlinNoise((tilePos.x * 0.333f + perlinOffset.x) / islandMargin[2], (tilePos.y * 0.333f + perlinOffset.y) / islandMargin[2]) , 2f )) , 0f, 1f);
    }

    [System.Serializable]
    public class biomeData{
        public int biomeID;
        public string biomeName;
        public Color32 biomeColor;
        public float coastline;
        public int[] availableGroundTiles;
    }

    public biomeData[] biomesToLoad;
    public static biomeData[] loadedBiomes;

    public static Vector3 getBiome(Vector2 tilePos){
        float[] errosionFactor = {
            tectonicFloat((tilePos.x + perlinOffset.x) / biomeSize[0], (tilePos.y + perlinOffset.y) / biomeSize[0], new float[]{biomeSize[0], biomeSize[0]}),
            erode((tilePos.y + perlinOffset.y) / biomeSize[1], (tilePos.x + perlinOffset.x) / biomeSize[1], 1f)};
        errosionFactor[0] *= getContinentNormalized(tilePos);
        float sector = Mathf.Clamp(errosionFactor[0] * 1.5f * loadedBiomes.Length-.1f, 0f, loadedBiomes.Length-.1f);
        biomeData bd = loadedBiomes[(int)sector];
        int aot =  bd.availableGroundTiles.Length;
        float coastline = bd.coastline;
        float partition;
        float saturation;
        float water = Mathf.Clamp(getWater(tilePos, 1)*aot*2, 0f, aot-1);
        if(water < 1f) partition = water * coastline;
        else partition = Mathf.Clamp(coastline + errosionFactor[1] * (aot-.1f-coastline), 0f, aot-.1f);
        try {
            tileData refTile = loadedTiles[bd.availableGroundTiles[(int)partition]];
            switch(refTile.saturationMethod){
                case 2: saturation = Mathf.PerlinNoise((tilePos.y + perlinOffset.y) / 5f, (tilePos.x + perlinOffset.x) / 5f)%1f; break; // flower field
                case 1: saturation = Mathf.Abs(Mathf.Cos(partition%1f*Mathf.PI)); break; // sinus proximity
                default: saturation = partition%1f; break; // proximity
            }
            return new Vector3(bd.availableGroundTiles[(int)partition], saturation, (int)sector);
        } catch (Exception e) {
            Debug.LogError("Biome progression breached " + sector + " (errosion " + errosionFactor[0] + ") - " + partition + "/" + aot + " (errosion " + errosionFactor[1] + ")\n" + e);
            return Vector2.zero;
        }
        //return Mathf.Lerp(1f, Mathf.Clamp(7f, 0f, getWater(tilePos)*14f), erode((tilePos.x * 3.333f + perlinOffset.y) / biomeSize, (tilePos.y * 3.333f + perlinOffset.x) / biomeSize, 1f) % 1f );
    }

    public static tileObject checkForObject(Cell target){
        tileData ground = target.ground;
        int choosen = (int)(saturatedPerlin(target.getPos().y / ground.chanceFO[1], target.getPos().x / ground.chanceFO[1], 1f) * ground.possibleObjects.Length-0.1f);
        float chance = erode(target.getPos().x / ground.chanceFO[1], target.getPos().y / ground.chanceFO[1], 10f);
        try {
            if (1f - chance <= ground.chanceFO[0]) return loadedObjects[ground.possibleObjects[choosen]];
            else return null;
        } catch (Exception e) {
            Debug.LogError("can't get tile object, index error. Ground possible objects " + ground.possibleObjects.Length + ", your index " + choosen + "\n" + e);
            return null;
        }
        
    }

    static float riverDensity = 10000f;
    static float[] riverMargin = {0.45f, 0.5f, 0.05f};
    public static float riverBias(Vector2 Pos){
        float riverBase = erodeTectonics((Pos.y * 3.333f - perlinOffset.x) / riverDensity, (Pos.x * 3.333f - perlinOffset.y) / riverDensity, 10f, new[]{100f, 200f});
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
