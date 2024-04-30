using System;
using System.Collections;
using System.Collections.Generic;
using getStatic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static getStatic.WorldManager;

public class CanvasScript : MonoBehaviour {

    // References
    public Transform MainPlayer;
    public Transform LoadingScreen;
    PlayerScript mp;
    public WorldManager WM;
    Canvas scaler;
    
    void Start(){
       GenerateMapTexture();
       mcdText = MapCoorData.GetChild(0).GetComponent<Text>();
       mp = MainPlayer.GetComponent<PlayerScript>();
       scaler = this.GetComponent<Canvas>();
    }

    void Update(){
        Options(!Initialized);
        if(Initialized) MapUpdate();
    }

    // Options
    public Transform OptionsWindow;
    public TextMeshProUGUI[] OptionsTextes;

    void Options(bool show){
        if(show){
            for(int st = 0; st < OptionsTextes.Length; st++){
                string setT = "";
                switch(OptionsTextes[st].name){
                    case "RenderDistance": 
                        setT = "Render distance: " + WM.renderSettings[WM.currRenderSetting].settingName; 
                        OptionsTextes[st].color = WM.renderSettings[WM.currRenderSetting].warningColor;
                        break;
                    case "InfiniteWorld":
                        if(worldSize[2] > 0.5f) setT = "Infinite world: true";
                        else setT = "Infinite world: false";
                        break;
                }
                OptionsTextes[st].text = setT;
            }

            OptionsWindow.localScale = Vector3.one;
            mp.stun = 1f;
        } else {
            OptionsWindow.localScale = Vector3.zero;
        }
    }

    public void optionButton(string buttType){
        switch(buttType){
            case "RenderDistance":
                WM.currRenderSetting = (WM.currRenderSetting+1)%5;
                break;
            case "Spawn":
                WM.Initialize();
                mapEnable = true;
                break;
            case "InfiniteWorld":
                worldSize[2] = (worldSize[2]+1f)%2f;
                break;
        }
    }

    public void changeX(string Input){
        try{ worldSize[0] = float.Parse(Input); } 
        catch (Exception e){ worldSize[0] = 20000f; print(e); }
    }

    public void changeY(string Input){
        try{ worldSize[1] = float.Parse(Input); } 
        catch (Exception e){ worldSize[1] = 20000f; print(e); }
    }

    public void changeSeed(string Input){
        if(Input.Length > 9) WM.Seed = int.Parse(Input[..9]);
        else WM.Seed = int.Parse(Input);
    }

    public void changeBiome(string Input){
        biomeSize[0] = float.Parse(Input);
    }

    public void changeRiverDensity(string Input){
        riverDensity = float.Parse(Input);
    }

    public void changeContinents(string Input){
        continentMargin[2] = float.Parse(Input);
    }

    public void changeRiverSize(Slider slider){
        riverMargin[0] = Mathf.Lerp(0.5f, 0.3f, slider.value);
        riverMargin[2] = 0.5f - riverMargin[0];
    }
    // Options

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
        mapLoad = new[]{0, TextureSize[0]*TextureSize[1], 40000};
    }

    void MapRender(){
        float[] offsetedBlock = {(mapLoad[0]%TextureSize[0]) - TextureSize[0]/2f, (mapLoad[0]/TextureSize[0]) - TextureSize[1]/2f};
        Color biomeColor = Color.blue;
        Vector2 e = Offset[0] + new Vector2(offsetedBlock[0] * BlocksPerPixel, offsetedBlock[1] * BlocksPerPixel);
        if(getWater(e) > 0f) {
            if(showBiomes) biomeColor = loadedBiomes[(int)getBiomeQuick(e)].biomeColor;
            else biomeColor = loadedTiles[(int)getBiomeQuick(e)].tileColor;
        }
        MapTexture.SetPixel(mapLoad[0]%TextureSize[0], mapLoad[0]/TextureSize[0], biomeColor);
        mapLoad[0]++;
    }

    string mcdString(Vector2 tile){
        return "Coordinates: (x" + tile.x + ", y" + tile.y + ")\n\nLMB - move map\nScroll - zoom in/out map\nRMB - teleport to this point";
    }

    void MapUpdate(){
        if(Input.GetKeyDown(KeyCode.M)) mapEnable = !mapEnable;
        if(mapEnable){
            MapAnchor.transform.localScale = Vector3.one;
            mp.stun = Time.deltaTime*4f;

            float shiftPower = 25f;
            Vector2 mousePos = (Input.mousePosition - new Vector3(Screen.width/2f, Screen.height/2f)) / scaler.scaleFactor;
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
                LoadingScreen.localScale = Vector3.zero;
            }

            if(Input.mouseScrollDelta.y != 0f)  shiftZoom = new[]{Mathf.Clamp(shiftZoom[0] - Input.mouseScrollDelta.y, 1f, Mathf.Infinity), 1f};
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
