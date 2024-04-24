using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static getStatic.WorldManager;

public class TextureDB : DrawBase {

    public Transform[] mapTransforms;
    public Texture2D[] mapTextures;
    public Transform PaintBrush;
    int amountOfMaps = 2;
    int currMap = 0;
    int ss = 32;

    public override void initializeSystem(){
        for(int setMaps = 0; setMaps < amountOfMaps; setMaps++){
            mapTransforms[setMaps].localScale = Vector3.zero;
            Texture2D nt = new Texture2D(MapSize*ss, MapSize*ss);
            mapTransforms[setMaps].GetComponent<MeshRenderer>().material.mainTexture = nt;
            mapTextures[setMaps] = nt;
        }
    }

    public override void beginLoad(Vector3 there){
        currMap = (currMap+1)%2;
        there = new(Mathf.Round(there.x), Mathf.Round(there.y));
        mapTransforms[currMap].position = there;
        mapTransforms[currMap].localScale = Vector3.zero;
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
            setTile(newCache[x, y]);
        } else {
            newCache[x, y] = new(new(MapSize/-2 + x + (int)loadPos.x, MapSize/-2 + y + (int)loadPos.y));
            setTile(newCache[x, y]);
        }

        if(loadChunk[0] >= loadChunk[1]-1) {
            mapTextures[currMap].Apply();
            for (int pt = 0; pt < amountOfMaps; pt++) {
                if(pt == currMap) mapTransforms[pt].localScale = Vector3.one*MapSize;
                else mapTransforms[pt].localScale = Vector3.zero;
            }
        }
    }

    void setTile(Cell target){
        if(!target.isWater) StampColor(target.getPos(), mapTextures[currMap], biomeColors[target.biome]);
        else StampColor(target.getPos(), mapTextures[currMap], Color.Lerp(Color.blue, Color.black, target.Height));
    }

    void StampColor(Vector2 coor, Texture2D sTex, Color sColor){
        Vector3 corrected = (coor-loadPos + new Vector2(MapSize/2f, MapSize/2f)) * ss;
        sTex.SetPixels32((int)corrected.x, (int)corrected.y, ss, ss, giveColorArray(sColor));
    }

    Color32[] giveColorArray(Color32 DesiredColor){
        Color32[] colorArray = new Color32[ss*ss];
        for(int ca = 0; ca < ss*ss; ca++) colorArray[ca] = DesiredColor;
        return colorArray;
    }

}
