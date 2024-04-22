using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random=UnityEngine.Random;

public class WorldManager : MonoBehaviour {
    
    // Randomizer
    public int Seed = 999999;
    Vector2 perlinOffset;

    // Cell info
    public int MapSize = 256;
    public int PushDist = 128;
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

    // References
    public Transform POV;
    public GameObject TestTile;

    void Start(){
        Random.InitState(Seed);
        perlinOffset = new Vector2(Random.value, Random.value);
        initializeWorld();
    }

    // Cell operators
    void initializeWorld(){
        Loaded = new Cell[MapSize, MapSize];
        for (int y = 0; y < MapSize; y++) for (int x = 0; x < MapSize; x++) {
            Loaded[x,y] = new(new(MapSize/-2 + x, MapSize/-2 + y), y+x);
        }
        currPos = Vector2.zero;  
        this.transform.position = currPos;
        CreateTestTiles();
    }

    void Update(){
        if(Mathf.Abs(POV.position.x-prevPos.x) > PushDist || Mathf.Abs(POV.position.y-prevPos.y) > PushDist){
            pushWorld(POV.position);
            prevPos = POV.position;
        }
        POV.transform.position += new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")) * Time.deltaTime * 4f;
    }

    void pushWorld(Vector2 there){
        there = new(Mathf.Round(there.x), Mathf.Round(there.y));
        Vector2 diff = there-currPos;
        int pushMarker = Random.Range(0, 10);

        Cell[,] Shifted = new Cell[MapSize, MapSize];
        for (int y = 0; y < MapSize; y++) for (int x = 0; x < MapSize; x++) {
            if( (diff.x < 0 && x >= -diff.x || diff.x > 0 && x < MapSize-diff.x || diff.x == 0f) && (diff.y < 0 && y >= -diff.y || diff.y > 0 && y < MapSize-diff.y || diff.y == 0f) ){
                Shifted[x, y] = Loaded[x + (int)diff.x, y + (int)diff.y];
            } else {
                Shifted[x, y] = new(new(MapSize/-2 + x + (int)there.x, MapSize/-2 + y + (int)there.y), pushMarker);
            }
        }
        Loaded = Shifted;
        currPos = there;
        this.transform.position = currPos;
        CreateTestTiles();
    }

    void CreateTestTiles(){
        foreach(GameObject cleanup in GameObject.FindGameObjectsWithTag("TestTile")) Destroy(cleanup);
        foreach(Cell instNew in Loaded){
            GameObject newT = Instantiate(TestTile);
            newT.transform.position = instNew.getPos();
            newT.GetComponent<SpriteRenderer>().color = Color.HSVToRGB((float)instNew.getID()/10f%1f, 0.5f, 0.5f);
            newT.transform.GetChild(0).GetComponent<TextMesh>().text = "x" + instNew.getPos().x + "\ny" + instNew.getPos().y;
        }
    }
    // Cell operators

}
