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
    readonly int[] ParseSpeed = {25, 1000};
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
            float perlin = getHeight(new(Pos.x, Pos.y));
            float islandM = getContinent(new(Pos.x, Pos.y));
            if(perlin < islandM){
                isWater = true;
                Height = perlin / islandM;
            } else {
                isWater = false;
                Height = (perlin - islandM) / (1f - islandM);
                biome = (int)getBiome(new(Pos.x, Pos.y));
                biomeSaturation = Mathf.Sin( getBiome(new(Pos.x, Pos.y)) / Mathf.PI );
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

    // test tiles
    public GameObject TestTile;
    GameObject[,] ttt;
    Color[] biomeColors = {
        new(0.5f, 1f, 0f), 
        new(1f, 1f, 0.5f), 
        new(0.5f, 0.5f, 0.5f), 
        new(1f,1f,1f),
        new(1f, 0.5f, 1f),
        new(0f, 0.5f, 0f),
        new(1f, 0.5f, 0f)
    };

    void Start(){
        if (RandomGeneration) Seed = Random.Range(1, 999999);
        Random.InitState(Seed);
        perlinOffset = new Vector2(Random.value, Random.value);
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

        Stats.text = statsUpdate();
    }

    void checkForChunkUpdate(){
        if(loadChunk[0] < loadChunk[1]) {
            int ps = (int)Mathf.Lerp(ParseSpeed[0], ParseSpeed[1], Vector3.Distance(loadPos, currPos) / MapSize);
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
        }

        Vis.transform.GetChild(0).GetComponent<TextMesh>().text = "x" + target.getPos().x + "\ny" + target.getPos().y + "\nb" + target.biome;// + " / " + target.biomeSaturation;
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
    public static float[] islandMargin = {1f, -1f, 40};
    public static float islandSize = 4;
    public static float biomeSize = 40;
    public static float getHeight(Vector2 tilePos){
        return Mathf.Pow( Mathf.PerlinNoise((tilePos.x * 0.333f + perlinOffset.x) / islandSize, (tilePos.y * 0.333f + perlinOffset.y) / islandSize) , 2f );
    }
    public static float getContinent(Vector2 tilePos){
        return Mathf.Clamp( Mathf.Lerp(islandMargin[0], islandMargin[1], Mathf.Pow( Mathf.PerlinNoise((tilePos.x * 0.333f + perlinOffset.x) / islandMargin[2], (tilePos.y * 0.333f + perlinOffset.y) / islandMargin[2]) , 2f )) , 0f, 1f);
    }
    public static float getBiome(Vector2 tilePos){
        return Mathf.Lerp(0f, 6f, Mathf.PerlinNoise((tilePos.x * 3.333f + perlinOffset.y) / biomeSize, (tilePos.y * 3.333f + perlinOffset.x) / biomeSize) );
    }
    // World generation values

}
