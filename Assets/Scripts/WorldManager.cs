using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Loading;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random=UnityEngine.Random;
using UnityEngine.UI;

namespace getStatic {
public class WorldManager : MonoBehaviour {
    
    // Randomizer
    public int Seed = 999999;
    public bool RandomGeneration = false;
    static Vector2 perlinOffset;

    // Cell info
    public static int MapSize = 320;
    public static int PushDist = 1;
    public static readonly int[] ParseSpeed = {10, 3000};
    public static Cell[,] Loaded;
    public static Vector2 currPos;
    Vector2 prevPos;

    public class Cell{
        Vector2Int Pos;
        public bool isWater;
        public float Height;
        public int biome;
        public float biomeSaturation;
        public Cell(Vector2Int sPos){
            Pos = sPos;
            setData();
        }
        public void pushPos(Vector2 newP){ Pos += new Vector2Int((int)newP.x, (int)newP.y); }
        public void pushPosPos(Vector2Int newP){ Pos += new Vector2Int(newP.x, newP.y); }
        public Vector2 getPos(){ return new(Pos.x, Pos.y); }
        void setData(){
            float checkWater = getWater(new(Pos.x, Pos.y));
            if(checkWater <= 0f){
                isWater = true;
                Height = -checkWater;
            } else {
                isWater = false;
                Height = checkWater;
                biome = (int)getBiome(new(Pos.x, Pos.y)).x;
                biomeSaturation = Mathf.Sin( getBiome(new(Pos.x, Pos.y)).y / Mathf.PI );
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
    public Texture2D[] tileSprites;
    static pixelMap[] tileSpritesRef;

    struct pixelMap{
        public Color32[] theMap;
        public pixelMap(Color32[] setMap){ theMap = setMap; }
    };

    void setupTileSprites(){
        tileSpritesRef = new pixelMap[tileSprites.Length];
        for (int gt = 0; gt < tileSprites.Length; gt++){
            tileSpritesRef[gt] = new (tileSprites[gt].GetPixels32());
        }
    }
    public static Color32[] getTileMap (int mapID) {
        return tileSpritesRef[mapID].theMap;
    }

    // test tiles
    public GameObject TestTile;
    GameObject[,] ttt;
    public static Color[] biomeColors = {
        new(.75f, .75f, 0.5f), // 0 - Sand
        new(0.5f, 1f, 0f), // 1 - Plain
        new(1f, 1f, 0f), // 2 - Farms
        new(1f, 0.5f, 1f), // 3 - Moor
        new(0f,0.5f,0f), // 4 - Forest
        new(0.5f, 0.5f, 0f), // 5 - Tundra
        new(0.5f, 0.5f, 0.5f), // 6 - Mountain
        new(1f, 1f, 1f) // 7 - Snow
    };

    void Start(){
        setupTileSprites();
        if (RandomGeneration) Seed = Random.Range(1, 999999);
        Random.InitState(Seed);
        perlinOffset = new Vector2(Random.value* 999999f, Random.value * 999999f);
        currPos = Vector3.one*9999f;
        drawingMechanism.initializeSystem();
        drawingMechanism.beginLoad(Vector3.zero);
    }

    void Update(){
        checkForChunkUpdate();
        
        POV.transform.position += new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * Time.deltaTime * 4f;
        if(Input.GetMouseButton(0)) POV.transform.position -= new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 4f;
        if(Input.mouseScrollDelta.y != 0f) POVscroll = Mathf.Clamp(POVscroll - Input.mouseScrollDelta.y, 5f, 200f);
        POV.GetComponent<Camera>().orthographicSize = Mathf.Lerp(POV.GetComponent<Camera>().orthographicSize, POVscroll, Time.deltaTime * 10f);

        Stats.text = statsUpdate();
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
            Loaded = newCache;
            loadChunk[0] = loadChunk[1]+1;
            currPos = loadPos;
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
        string result = "FPS: " + (1f/Time.unscaledDeltaTime).ToString();
        if(loadChunk[0] < loadChunk[1]) result += "\nLoading tiles: " + loadChunk[0] + "/" + loadChunk[1] + " speed: " + (int)Mathf.Lerp(ParseSpeed[0], ParseSpeed[1], psLerp) + "/" + ParseSpeed[1];
        else result += "\nMap loaded";
        result += "\nLoad instance " + chungID + "\n\n" + "\nLoad offset: " + loadPos + "\nWorld offset: " + currPos + "\nCamera offset: " + POV.transform.position;
        result += "\n\nMap size: " + MapSize + "\nRefresh trigger: " + PushDist + "\nRefresh speed: " + ParseSpeed;
        return result;
    }
    // Stats monitor

    // World generation values
    public static float[] islandMargin = {1f, -1f, 200};
    public static float islandSize = 20;
    public static float biomeSize = 320;
    public static float getHeight(Vector2 tilePos){
        return Mathf.Pow( erode((tilePos.x * 0.333f + perlinOffset.x) / islandSize, (tilePos.y * 0.333f + perlinOffset.y) / islandSize, 5f) , 2f );
    }
    public static float getContinent(Vector2 tilePos){
        float land = Mathf.Pow( erode((tilePos.x * 0.333f + perlinOffset.x) / islandMargin[2], (tilePos.y * 0.333f + perlinOffset.y) / islandMargin[2], 0f) , 2f );
        return Mathf.Clamp( Mathf.Lerp(islandMargin[0], islandMargin[1], land) , 0f, 1f);
    }

    public static int[,] biomeProgression = new int[7, 9]{
        {0,1,2,3,4,5,6,7,0}, // Normal
        {0,1,1,2,3,3,2,4,1}, // Grasslands
        {1,5,7,5,7,7,4,6,2}, // Snow
        {0,1,2,5,4,4,5,4,3}, // Woodlands
        {0,0,0,1,0,0,2,6,0}, // Desert
        {0,5,5,4,5,5,3,5,4}, // Tundra
        {0,1,3,3,1,3,3,2,2}, // Pure moor
    };
    public static Vector2 getBiome(Vector2 tilePos){
        float sector = erode((tilePos.x*3.333f + perlinOffset.x) / (biomeSize*10f), (tilePos.y*3.333f + perlinOffset.y) / (biomeSize*10f), 15f) * 6.9f;
        float partition = erode((tilePos.x * 3.333f + perlinOffset.y) / biomeSize, (tilePos.y * 3.333f + perlinOffset.x) / biomeSize, 10f) * Mathf.Clamp(getWater(tilePos, 1)*24f, 0f, 7.9f);
        //float partition = Mathf.Lerp(1f, Mathf.Clamp(7f, 0f, getWater(tilePos, 1)*14f), erode((tilePos.x * 3.333f + perlinOffset.y) / biomeSize, (tilePos.y * 3.333f + perlinOffset.x) / biomeSize, 1f) % 1f );
        try {
            return new(biomeProgression[(int)sector, (int)partition], partition%1f);
        } catch (Exception e) {
            print("Biome progression breached " + sector + " - " + e);
            return Vector2.zero;
        }
        //return Mathf.Lerp(1f, Mathf.Clamp(7f, 0f, getWater(tilePos)*14f), erode((tilePos.x * 3.333f + perlinOffset.y) / biomeSize, (tilePos.y * 3.333f + perlinOffset.x) / biomeSize, 1f) % 1f );
    }
    public static float riverDensity = 1600f;
    public static float[] riverMargin = {0.45f, 0.5f, 0.05f};
    public static float riverBias(Vector2 Pos){
        float riverBase = erode((Pos.y * 3.333f - perlinOffset.x) / riverDensity, (Pos.x * 3.333f - perlinOffset.y) / riverDensity, 5f);
        if(riverBase >= riverMargin[0] && riverBase <= riverMargin[1]) return Mathf.Pow( Mathf.Sin((riverBase-riverMargin[0]) / riverMargin[2] * Mathf.PI) , 3f);
        else return 0f;
    }
    public static float getWater(Vector2 Pos, int nega = -1){
        float perlin = getHeight(new(Pos.x, Pos.y));
        float islandM = getContinent(new(Pos.x, Pos.y)) + Mathf.Lerp(riverBias(Pos), 0f, perlin/2f);
        if(perlin < islandM) return perlin / (islandM*nega);
        else return (perlin - islandM) / (1f - islandM);
    }

    public static float erode(float x, float y, float p = 10f){
        float normal = Mathf.PerlinNoise(x, y);
        float eroded = Mathf.PerlinNoise(x * p, y * p);
        float mask = Mathf.PerlinNoise(y / 3f, x / 3f);
        return Mathf.Lerp(normal, eroded, mask/2f);
    }
    // World generation values

}
}
