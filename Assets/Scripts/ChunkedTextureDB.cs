using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static getStatic.WorldManager;

public class ChunkedTextureDB : DrawBase {

    public Transform POV;
    float prevZ = 0f;
    public Transform[,] chunkTransforms, objRefs;
    int[,] updateRots;
    Texture2D[,] chunkTextures;
    Material[,] objTexs;
    int[,] needRewrite;
    public int chunkSize = 32;
    int currMap = 0;
    public int ss = 32;
    public int[] objChunkSize = {4, 2};
    bool hasLoadedTiles = false;
    Vector2[] cfoPP;

    public override void initializeSystem(){
        PushDist *= chunkSize;
        GameObject FirstChunk = this.transform.GetChild(0).GetChild(0).gameObject;
        int chunksPerWidth = MapSize/chunkSize;
        chunkTransforms = new Transform[chunksPerWidth, chunksPerWidth];
        chunkTextures = new Texture2D[chunksPerWidth, chunksPerWidth];
        needRewrite = new int[chunksPerWidth, chunksPerWidth];
        for(int smY = 0; smY < chunksPerWidth; smY++) for(int smX = 0; smX < chunksPerWidth; smX++){
            GameObject anotherChunk = Instantiate(FirstChunk);
            chunkTransforms[smX, smY] = anotherChunk.transform;
            chunkTransforms[smX, smY].SetParent(this.transform.GetChild(0));
            chunkTransforms[smX, smY].localScale = Vector3.one*chunkSize;
            Texture2D nt = new Texture2D(chunkSize*ss, chunkSize*ss);
            chunkTransforms[smX, smY].GetComponent<MeshRenderer>().material.mainTexture = nt;
            chunkTransforms[smX, smY].GetComponent<MeshRenderer>().material.mainTexture.filterMode = FilterMode.Point;
            chunkTextures[smX, smY] = nt;
        }

        GameObject firstObject = this.transform.GetChild(1).GetChild(0).gameObject;
        objRefs = new Transform[objChunkSize[0], objChunkSize[0]];
        objTexs = new Material[objChunkSize[0], objChunkSize[0]];
        updateRots = new int[objChunkSize[0], objChunkSize[0]];
        for(int smY = 0; smY < objChunkSize[0]; smY++) for(int smX = 0; smX < objChunkSize[0]; smX++){
            GameObject anotherObj = Instantiate(firstObject);
            objRefs[smX, smY] = anotherObj.transform;
            objRefs[smX, smY].localScale = Vector3.one * 0.9f;
            objRefs[smX, smY].SetParent(this.transform.GetChild(1));
            objRefs[smX, smY].GetComponent<MeshRenderer>().material.mainTexture.filterMode = FilterMode.Point;
            objTexs[smX, smY] = objRefs[smX, smY].GetComponent<MeshRenderer>().material;
            objRefs[smX, smY].gameObject.SetActive(false);
        }
        cfoPP = new Vector2[]{ Vector3.one * 999f, Vector3.zero};

        Destroy(FirstChunk);
        Destroy(firstObject);

    }

    void Update(){
        if(Mathf.Abs(POV.position.x-cfoPP[0].x) > objChunkSize[1] || Mathf.Abs(POV.position.y-cfoPP[0].y) > objChunkSize[1]){
            if(Loaded != null) updateTileObjects(Loaded);
            cfoPP[0] = new(Mathf.Round(POV.position.x / objChunkSize[1]) * objChunkSize[1], Mathf.Round(POV.position.y / objChunkSize[1]) * objChunkSize[1]);
        }

        if(POV.eulerAngles.z != prevZ){
            prevZ = POV.eulerAngles.z;
            for (int ry = 0; ry < objChunkSize[0]-1; ry++) for (int rx = 0; rx < objChunkSize[0]-1; rx++) if (updateRots[rx, ry] == 1) {
                Vector2Int cellOffset = new Vector2Int((int)cfoPP[1].x + rx - objChunkSize[0]/2, (int)cfoPP[1].y + ry - objChunkSize[0]/2);
                if(cellOffset.x > 0f && cellOffset.x < MapSize-1 && cellOffset.y > 0f && cellOffset.y < MapSize-1){
                    rotObj(rx, ry, Loaded[cellOffset.x, cellOffset.y]);
                }
            }
        }
    }

    void updateTileObjects(Cell[,] targetArray){
        cfoPP[1] = new Vector2(POV.position.x, POV.position.y) - (currPos - new Vector2(MapSize/2f, MapSize/2f));
        for(int sy = 0; sy < objChunkSize[0]-1; sy++) for (int sx = 0; sx < objChunkSize[0]-1; sx++) {
            Vector2Int cellOffset = new Vector2Int((int)cfoPP[1].x + sx - objChunkSize[0]/2, (int)cfoPP[1].y + sy - objChunkSize[0]/2);
            if(cellOffset.x > 0f && cellOffset.x < MapSize-1 && cellOffset.y > 0f && cellOffset.y < MapSize-1){
                setObj(sx, sy, targetArray[cellOffset.x, cellOffset.y]);
            }
        }
    }

    public override void beginLoad(Vector3 there){
        currMap = (currMap+1)%2;
        there = new(Mathf.Round(there.x / chunkSize) * chunkSize, Mathf.Round(there.y / chunkSize) * chunkSize);
        chungID++;
        loadPos = there;
        newCache = new Cell[MapSize, MapSize];
        loadChunk = new[]{0, MapSize*MapSize};
        hasLoadedTiles = false;
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
            needRewrite[x/chunkSize, y/chunkSize] = 1;
        }

        if(x%chunkSize == chunkMargin[0] && y%chunkSize == chunkMargin[1]){
            int[] getOld = new[]{MapSize/chunkSize - 1 - x/chunkSize, MapSize/chunkSize - 1 - y/chunkSize};
            int[] pushOld = new[]{(int)(diff.x/chunkSize), (int)(diff.y/chunkSize)};
            if(needRewrite[x/chunkSize, y/chunkSize] == 1){
                setChunk(
                    blur, // xy for bl - xy for ur
                    getOld
                );
            } else {
                copyChunk(
                    new[]{getOld[0]+pushOld[0], getOld[1]+pushOld[0]},
                    getOld
                );
            }
        }

        if(loadChunk[0] >= loadChunk[1]-1) {
            currPos = loadPos;
            cfoPP[0] = Vector2.one * -9999f;
            Loaded = newCache;
            hasLoadedTiles = true;
            newCache = new Cell[0,0];
         }
    }

    void setObj(int x, int y, Cell t){
        if(t != null && t.cellObject != null){
            tileObject o = t.cellObject;
            if (!objRefs[x, y].gameObject.activeSelf) objRefs[x, y].gameObject.SetActive(true);
            objRefs[x, y].position = t.getPos() + o.getPivot(ref POV);
            objRefs[x, y].eulerAngles = POV.eulerAngles;
            objRefs[x, y].position -= Vector3.forward*o.getZ(-POV.up, objRefs[x, y].position - POV.position, objChunkSize[0]);
            objRefs[x, y].localScale = o.getScale();
            objTexs[x, y].mainTexture = o.getTexture();
            updateRots[x, y] = 1;
        } else if (objRefs[x, y].gameObject.activeSelf) {
            objRefs[x, y].gameObject.SetActive(false);
            updateRots[x, y] = 0;
        }
    }

    void rotObj(int x, int y, Cell t){
        tileObject o = t.cellObject;
        objRefs[x, y].position = t.getPos() + o.getPivot(ref POV);
        objRefs[x, y].eulerAngles = POV.eulerAngles;
        objRefs[x, y].position -= Vector3.forward*o.getZ(-POV.up, objRefs[x, y].position - POV.position, objChunkSize[0]);
    }

    void setChunk(int[] cc, int[] gc){
        Cell[] ca = {newCache[cc[0], cc[1]], newCache[cc[2], cc[3]]};
        chunkTransforms[gc[0], gc[1]].position = Vector3.Lerp(ca[0].getPos(), ca[1].getPos(), 0.5f);
        chunkTransforms[gc[0], gc[1]].localScale = Vector3.one * chunkSize;
        for (int y = cc[1]; y <= cc[3]; y++) for (int x = cc[0]; x <= cc[2]; x++) {
            setTile(newCache[x, y], chunkTransforms[gc[0], gc[1]].position, chunkTextures[gc[0], gc[1]]);
        }
        chunkTextures[gc[0], gc[1]].Apply();
    }

    void copyChunk(int[] from, int[] to){
        chunkTransforms[to[0], to[1]].position = chunkTransforms[from[0], from[1]].position;
        chunkTransforms[to[0], to[1]].localScale = Vector3.one * chunkSize;
        chunkTextures[to[0], to[1]].LoadRawTextureData(chunkTextures[from[0], from[0]].GetRawTextureData());
    }

    void setTile(Cell target, Vector3 tChunk, Texture2D tTexture){
        if(!target.isWater) StampImage( target.getPos(), tChunk, tTexture, getTM(target.ground.tileID, target.biomeSaturation));
        else StampColor(target.getPos(), tChunk, tTexture, Color.Lerp(new(0f, 0f, 0f, 0f), new(0f,0.1f,0.2f,1f), target.Height));
    }

    void StampImage(Vector2 coor, Vector2 chunkPos, Texture2D sTex, Color32[] sImg){
        Vector3 corrected = ((coor-chunkPos + new Vector2(chunkSize/2f, chunkSize/2f)) * ss) - (Vector2.one*ss/2f);
        sTex.SetPixels32((int)corrected.x, (int)corrected.y, ss, ss, sImg);
    }

    void StampColor(Vector2 coor, Vector2 chunkPos, Texture2D sTex, Color sColor){
        Vector3 corrected = ((coor-chunkPos + new Vector2(chunkSize/2f, chunkSize/2f)) * ss) - (Vector2.one*ss/2f);
        sTex.SetPixels32((int)corrected.x, (int)corrected.y, ss, ss, giveColorArray(sColor));
    }

    Color32[] giveColorArray(Color32 DesiredColor){
        Color32[] colorArray = new Color32[ss*ss];
        for(int ca = 0; ca < ss*ss; ca++) colorArray[ca] = DesiredColor;
        return colorArray;
    }

}
