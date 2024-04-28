using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static getStatic.WorldManager;

public class CanvasScript : MonoBehaviour {
    
    void Start(){
       GenerateMapTexture();
       mcdText = MapCoorData.GetChild(0).GetComponent<Text>();
       mp = MainPlayer.GetComponent<PlayerScript>();
    }

    void Update(){
        MapUpdate();
    }

    // References
    public Transform MainPlayer;
    PlayerScript mp;

    // Map stuff
    bool showBiomes = true;
    [SerializeField] Transform MapAnchor;
    [SerializeField] RectTransform MapCoorData;
    Text mcdText;
    [SerializeField] RawImage Map;
    Texture2D MapTexture;
    int[] TextureSize = {1, 0, 1};
    int[] mapLoad = {1, 0, 1};
    Vector2[] Offset = {Vector2.zero, Vector2.one*9999f};
    int BlocksPerPixel = 25;
    bool mapEnable = false;
    float[] shiftZoom = {1f, 0f};

    void GenerateMapTexture(){
        if(shiftZoom[0] != BlocksPerPixel){
            BlocksPerPixel = (int)shiftZoom[0];
            shiftZoom = new[]{BlocksPerPixel, 1f};
        }
        if(!MapTexture){
            TextureSize = new[]{Screen.width, Screen.height};
            Texture2D nT = new Texture2D(TextureSize[0], TextureSize[1]);
            MapTexture = nT;
            Map.material.SetTexture("_MainTex", nT);
        }
        mapLoad = new[]{0, TextureSize[0]*TextureSize[1], 100000};
    }

    void MapRender(){
        float[] offsetedBlock = {(mapLoad[0]%TextureSize[0]) - TextureSize[0]/2f, (mapLoad[0]/TextureSize[0]) - TextureSize[1]/2f};
        Color biomeColor = Color.blue;
        Vector2 e = Offset[0] + new Vector2(offsetedBlock[0] * BlocksPerPixel, offsetedBlock[1] * BlocksPerPixel);
        if(getWater(e) > 0f) {
            if(showBiomes) biomeColor = loadedBiomes[(int)getBiome(e).z].biomeColor;
            else biomeColor = loadedTiles[(int)getBiome(e).x].tileColor;
        }
        MapTexture.SetPixel(mapLoad[0]%TextureSize[0], mapLoad[0]/TextureSize[0], biomeColor);
        mapLoad[0]++;
    }

    string mcdString(Vector2 tile){
        return "x" + tile.x + "\ny" + tile.y;
    }

    void MapUpdate(){
        if(Input.GetKeyDown(KeyCode.M)) mapEnable = !mapEnable;
        if(mapEnable){
            MapAnchor.transform.localScale = Vector3.one;
            mp.stun = Time.deltaTime*4f;

            float shiftPower = 25f;
            Vector2 mousePos = (Input.mousePosition * this.GetComponent<CanvasScaler>().scaleFactor) - new Vector3(Screen.width/2f, Screen.height/2f);
            Vector2 pointedPos = (mousePos * BlocksPerPixel) + Offset[0];
            MapCoorData.anchoredPosition = mousePos;
            mcdText.text = mcdString(pointedPos);
            if(Input.GetMouseButton(0) && mapEnable && shiftZoom[0] == BlocksPerPixel){
                Offset[0] -= new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * BlocksPerPixel * shiftPower;
                Map.GetComponent<RectTransform>().anchoredPosition += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * shiftPower;
            } else if (Offset[1] != Offset[0]){
                Offset[1] = Offset[0];
                Map.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                GenerateMapTexture();
            } else if (Input.GetMouseButtonDown(1)){
                Offset[0] = pointedPos;
                MainPlayer.position = pointedPos;
                GenerateMapTexture();
            }

            if(Input.mouseScrollDelta.y != 0f)  shiftZoom = new[]{Mathf.Clamp(shiftZoom[0] - Input.mouseScrollDelta.y, 1f, 100f), 1f};
            if(shiftZoom[0] != BlocksPerPixel){
                Map.GetComponent<RectTransform>().localScale = Vector2.one * (BlocksPerPixel / shiftZoom[0]);
                shiftZoom[1] -= Time.deltaTime;
                if(shiftZoom[1] <= 0f){
                    GenerateMapTexture();
                }
            }

            if(mapLoad[0] < mapLoad[1] && mapEnable) {
                for (int push = Mathf.Clamp(mapLoad[2], 0, mapLoad[1]-mapLoad[0]); push > 0; push--) MapRender();
                MapTexture.Apply();
                Map.transform.localScale = Vector3.zero;
            } else if (shiftZoom[0] == BlocksPerPixel) {
                Map.transform.localScale = Vector3.one;
            }
        } else {
            MapAnchor.transform.localScale = Vector3.zero;
        }
    }
    // Map stuff

}
