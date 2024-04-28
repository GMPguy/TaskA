using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour {

    public Transform MainCamera;
    public Transform Capsule;
    Camera mcCamera;
    Vector3 cameraOffset;
    float POVscroll = 5f;
    public float playerSpeed = 10f;
    public float stun = 0f;

    void Start() {
        mcCamera = MainCamera.GetComponent<Camera>();
    }

    void Update() {

        // Movement
        if(stun > 0f){
            stun = Mathf.Clamp(stun -= Time.deltaTime, 0f, Mathf.Infinity);
        } else {
            this.transform.position += (MainCamera.right * Input.GetAxis("Horizontal") + MainCamera.up * Input.GetAxis("Vertical")) * playerSpeed * Time.deltaTime;
            if(Input.GetMouseButton(1)) cameraOffset -= new Vector3(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"), 0f);
            if(Input.GetMouseButton(2)) MainCamera.Rotate(Vector3.forward * Input.GetAxis("Mouse X") * 4f);
            if(Input.mouseScrollDelta.y != 0f) POVscroll = Mathf.Clamp(POVscroll - Input.mouseScrollDelta.y, 5f, 100f);
        }

        // Camera control
        Vector3 setCamPos = Vector3.Lerp(MainCamera.transform.position, this.transform.position + cameraOffset, Time.deltaTime * 20f);
        setCamPos.z = -10f;
        MainCamera.transform.position = setCamPos;
        mcCamera.orthographicSize = Mathf.Lerp(mcCamera.orthographicSize, POVscroll, Time.deltaTime * 10f);

        // Test capsule rot
        float Rot = MainCamera.eulerAngles.z;
        Capsule.eulerAngles = MainCamera.eulerAngles;
        Capsule.localPosition = MainCamera.up/2f;
        
    }
}
