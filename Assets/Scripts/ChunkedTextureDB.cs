using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static getStatic.WorldManager;

public class ChunkedTextureDB : DrawBase {

    public Transform[,] chunkTransforms;
    public Texture2D[,] chunkTextures;
    public Transform PaintBrush;
    public int chunkSize = 32;
    public int amountOfMaps = 2;
    int currMap = 0;
    public int ss = 1;
    int[,] pushColored;
    public override void initializeSystem(){
        PushDist /= chunkSize;
        pushColored = new int[MapSize/chunkSize, MapSize/chunkSize];
        GameObject FirstChunk = this.transform.GetChild(0).gameObject;
        int chunksPerWidth = MapSize/chunkSize;
        chunkTransforms = new Transform[chunksPerWidth, chunksPerWidth];
        chunkTextures = new Texture2D[chunksPerWidth, chunksPerWidth];
        for(int smY = 0; smY < chunksPerWidth; smY++) for(int smX = 0; smX < chunksPerWidth; smX++){
            GameObject anotherChunk = Instantiate(FirstChunk);
            chunkTransforms[smY, smX] = anotherChunk.transform;
            chunkTransforms[smY, smX].localScale = Vector3.one*chunkSize;
            Texture2D nt = new Texture2D(chunkSize*ss, chunkSize*ss);
            chunkTransforms[smY, smX].GetComponent<MeshRenderer>().material.mainTexture = nt;
            chunkTextures[smY, smX] = nt;
        }
        Destroy(FirstChunk);
    }

    public override void beginLoad(Vector3 there){
        currMap = (currMap+1)%2;
        there = new(Mathf.Round(there.x / chunkSize) * chunkSize, Mathf.Round(there.y / chunkSize) * chunkSize);
        chungID++;
        loadPos = there;
        newCache = new Cell[MapSize, MapSize];
        loadChunk = new[]{0, MapSize*MapSize};
    }

    
    public override void load(int loadID, int lastCall){
        Vector2 diff = loadPos-currPos;
        int x = loadID%MapSize;
        int y = loadID/MapSize;
        int[] chunkMargin = {chunkSize-1, chunkSize-1};
        int[] blur = {x-chunkSize+1, y-chunkSize+1, x, y};
        if(diff.x < 0f) {
            x = MapSize - x - 1; 
            chunkMargin[0] = 0;
            blur[0] = x;
            blur[2] = x+chunkSize-1;
        }
        if(diff.y < 0f) {
            y = MapSize - y - 1; 
            chunkMargin[1] = 0;
            blur[1] = y;
            blur[3] = y+chunkSize-1;
        }
        
        
        if( (diff.x < 0 && x >= -diff.x || diff.x > 0 && x < MapSize-diff.x || diff.x == 0f) && (diff.y < 0 && y >= -diff.y || diff.y > 0 && y < MapSize-diff.y || diff.y == 0f) ) {
            newCache[x, y] = Loaded[x + (int)diff.x, y + (int)diff.y];
        } else { 
            newCache[x, y] = new(new(MapSize/-2 + x + (int)loadPos.x, MapSize/-2 + y + (int)loadPos.y));
            pushColored[x/chunkSize, y/chunkSize] = 1;
        }

        if(x%chunkSize == chunkMargin[0] && y%chunkSize == chunkMargin[1]){
            setChunk(
                blur, // xy for bl - xy for ur
                new int[] {x/chunkSize, y/chunkSize}
            );
        }

        if(loadChunk[0] >= loadChunk[1]-1) { }
    }

    void setChunk(int[] cc, int[] gc){
        Cell[] ca = {newCache[cc[0], cc[1]], newCache[cc[2], cc[3]]};
        chunkTransforms[gc[0], gc[1]].position = Vector3.Lerp(ca[0].getPos(), ca[1].getPos(), 0.5f);
        chunkTransforms[gc[0], gc[1]].localScale = Vector3.one * chunkSize * 0.98f;
        for (int y = cc[1]; y <= cc[3]; y++) for (int x = cc[0]; x <= cc[2]; x++) {
            setTile(newCache[x, y], chunkTransforms[gc[0], gc[1]].position, chunkTextures[gc[0], gc[1]]);
        }
        chunkTextures[gc[0], gc[1]].Apply();
    }

    void setTile(Cell target, Vector3 tChunk, Texture2D tTexture){
        if(!target.isWater) StampColor( target.getPos(), tChunk, tTexture, biomeColors[target.biome]);
        else StampColor(target.getPos(), tChunk, tTexture, Color.Lerp(Color.blue, Color.black, target.Height));
    }

    void StampColor(Vector2 coor, Vector2 chunkPos, Texture2D sTex, Color sColor){
        Vector3 corrected = ((coor-chunkPos + new Vector2(chunkSize/2f, chunkSize/2f)) * ss) - ((Vector2.one*ss)/2f);
        sTex.SetPixels32((int)corrected.x, (int)corrected.y, ss, ss, giveColorArray(sColor));
    }

    Color32[] giveColorArray(Color32 DesiredColor){
        Color32[] colorArray = new Color32[ss*ss];
        for(int ca = 0; ca < ss*ss; ca++) colorArray[ca] = DesiredColor;
        return colorArray;
    }

}
