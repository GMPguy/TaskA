using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Loading;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random=UnityEngine.Random;
using UnityEngine.UI;

public class WorldManager : MonoBehaviour {
    
    // Randomizer
    public int Seed = 999999;
    public bool RandomGeneration = false;
    static Vector2 perlinOffset;

    // Cell info
    const int MapSize = 100;
    const int PushDist = 25;
    readonly int[] ParseSpeed = {100, 1000};
    Cell[,] Loaded;
    Vector2 currPos;
    Vector2 prevPos;

    class Cell{
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
    Vector2 loadPos = Vector2.zero;
    Cell[,] newCache;
    int[] loadChunk = {0, 1};
    int chungID;
    // Loader

    // References
    public Transform POV;
    float POVscroll = 10f;

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
        if (RandomGeneration) Seed = Random.Range(1, 999999);
        Random.InitState(Seed);
        perlinOffset = new Vector2(Random.value* 999999f, Random.value * 999999f);
        currPos = Vector3.one*9999f;
        beginLoad(Vector3.zero);

        // temp tiles
        ttt = new GameObject[MapSize, MapSize];
        for(int tx = 0; tx < MapSize; tx++) for (int ty = 0; ty < MapSize; ty++) {
            GameObject newT = Instantiate(TestTile);
            ttt[tx, ty] = newT;
        }
    }

    void Update(){
        checkForChunkUpdate();
        
        POV.transform.position += new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * Time.deltaTime * 4f;
        if(Input.GetMouseButton(0)) POV.transform.position -= new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 4f;
        if(Input.mouseScrollDelta.y != 0f) POVscroll = Mathf.Clamp(POVscroll - Input.mouseScrollDelta.y, 5f, 200f);
        POV.GetComponent<Camera>().orthographicSize = Mathf.Lerp(POV.GetComponent<Camera>().orthographicSize, POVscroll, Time.deltaTime * 10f);

        Stats.text = statsUpdate();
    }

    void checkForChunkUpdate(){
        if(loadChunk[0] < loadChunk[1]) {
            int ps = (int)Mathf.Lerp(ParseSpeed[0], ParseSpeed[1], (Vector3.Distance(loadPos, currPos) - MapSize/2f) / MapSize);
            for (int ql = Mathf.Clamp(ps, 0, loadChunk[1]-loadChunk[0]); ql > 0; ql--) {
                Load(loadChunk[0]);
                loadChunk[0]++;
            }
        } else if (loadChunk[0] == loadChunk[1]) {
            Loaded = newCache;
            loadChunk[0] = loadChunk[1]+1;
            currPos = loadPos;
        }

        if((Mathf.Abs(POV.position.x-prevPos.x) > PushDist || Mathf.Abs(POV.position.y-prevPos.y) > PushDist) && loadChunk[0] > loadChunk[1]){
            beginLoad(POV.position);
            prevPos = POV.position;
        }
    }

    void beginLoad(Vector3 there){
        there = new(Mathf.Round(there.x), Mathf.Round(there.y));
        chungID++;
        loadPos = there;
        newCache = new Cell[MapSize, MapSize];
        loadChunk = new[]{0, MapSize*MapSize};
    }

    void Load(int loadID){
        Vector2 diff = loadPos-currPos;
        int x = loadID%MapSize;
        int y = loadID/MapSize;
        if(diff.x < 0f) x = MapSize - x - 1;
        if(diff.y < 0f) y = MapSize - y - 1;

        if( (diff.x < 0 && x >= -diff.x || diff.x > 0 && x < MapSize-diff.x || diff.x == 0f) && (diff.y < 0 && y >= -diff.y || diff.y > 0 && y < MapSize-diff.y || diff.y == 0f) ) {
            newCache[x, y] = Loaded[x + (int)diff.x, y + (int)diff.y];
            setTile(ttt[x, y], newCache[x, y]);
        } else {
            newCache[x, y] = new(new(MapSize/-2 + x + (int)loadPos.x, MapSize/-2 + y + (int)loadPos.y));
            setTile(ttt[x, y], newCache[x, y]);
        }
    }

    void setTile(GameObject Vis, Cell target){
        Vis.transform.position = target.getPos();

        if(target.isWater) Vis.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.black, Color.blue, target.Height);
        else {
            //Vis.GetComponent<SpriteRenderer>().color = Color.Lerp(new Color(0.5f, 1f, 0f), new Color(0f, 0.25f, 0f), target.Height);
            Vis.GetComponent<SpriteRenderer>().color = biomeColors[target.biome];//Color.Lerp(biomeColors[target.biome], biomeColors[target.biome]/10f, target.biomeSaturation);
            //Vis.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.white, Color.red, getWater(target.getPos()) *12f);
            //Vis.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.white, Color.black, riverBias(target.getPos()));
        }
        //Vis.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.white, Color.black, riverBias(target.getPos()));

        Vis.transform.GetChild(0).GetComponent<TextMesh>().text = "x" + target.getPos().x + "\ny" + target.getPos().y + "\nw" + target.isWater;// + " / " + target.biomeSaturation;
    }
    // Cell operators

    // Stats monitor
    public Text Stats;
    string statsUpdate(){
        string result = "FPS: " + (1f/Time.unscaledDeltaTime).ToString();
        if(loadChunk[0] < loadChunk[1]) result += "\nLoading tiles: " + loadChunk[0] + "/" + loadChunk[1];
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
