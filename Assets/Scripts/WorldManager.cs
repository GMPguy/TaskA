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
    Vector2 perlinOffset;

    // Cell info
    const int MapSize = 100;
    const int PushDist = 10;
    const int ParseSpeed = 10;
    Cell[,] Loaded;
    Vector2 currPos;
    Vector2 prevPos;

    class Cell{
        Vector2Int Pos;
        int TileID = 0;
        public Cell(Vector2Int sPos, int setID){
            Pos = sPos; TileID = setID;
        }
        public int getID() { return TileID; }
        public void pushPos(Vector2 newP){ Pos += new Vector2Int((int)newP.x, (int)newP.y); }
        public void pushPosPos(Vector2Int newP){ Pos += new Vector2Int(newP.x, newP.y); }
        public Vector2 getPos(){ return new(Pos.x, Pos.y); }
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

    void Start(){
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
        Stats.text = statsUpdate();
    }

    void checkForChunkUpdate(){
        if((Mathf.Abs(POV.position.x-prevPos.x) > PushDist || Mathf.Abs(POV.position.y-prevPos.y) > PushDist) && loadChunk[0] >= loadChunk[1]){
            beginLoad(POV.position);
            prevPos = POV.position;
        }

        if(loadChunk[0] < loadChunk[1]) {
            for (int ql = Mathf.Clamp(ParseSpeed, 0, loadChunk[1]-loadChunk[0]); ql > 0; ql--) {
                Load(loadChunk[0]);
                loadChunk[0]++;
            }
        } else if (loadChunk[0] == loadChunk[1]) {
            Loaded = newCache;
            loadChunk[0] = loadChunk[1]+1;
            currPos = loadPos;
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
            newCache[x, y] = new(new(MapSize/-2 + x + (int)loadPos.x, MapSize/-2 + y + (int)loadPos.y), chungID);
            setTile(ttt[x, y], newCache[x, y]);
        }
    }

    void setTile(GameObject Vis, Cell target){
        Vis.transform.position = target.getPos();
        Vis.GetComponent<SpriteRenderer>().color = Color.HSVToRGB((float)target.getID()/10f%1f, 0.5f, 0.5f);
        Vis.transform.GetChild(0).GetComponent<TextMesh>().text = "x" + target.getPos().x + "\ny" + target.getPos().y + "\nid" + target.getID();
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

}
