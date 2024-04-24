using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static getStatic.WorldManager;

public class TestDB : DrawBase {

    [SerializeField] GameObject TestTile;
    GameObject[,] ttt;

    override public void initializeSystem(){
        ttt = new GameObject[MapSize, MapSize];
        for(int tx = 0; tx < MapSize; tx++) for (int ty = 0; ty < MapSize; ty++) {
            GameObject newT = Instantiate(TestTile);
            ttt[tx, ty] = newT;
        }
    }

    override public void beginLoad(Vector3 there){
        there = new(Mathf.Round(there.x), Mathf.Round(there.y));
        chungID++;
        loadPos = there;
        newCache = new Cell[MapSize, MapSize];
        loadChunk = new[]{0, MapSize*MapSize};
    }

    public override void load(int loadID, int lastCall){
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

}
