using System.Collections;
using System.Collections.Generic;
using getStatic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class DrawBase : MonoBehaviour {
    abstract public void initializeSystem();
    abstract public void beginLoad(Vector3 There);
    abstract public void load(int LoadID, int lastCall);
    abstract public void updateSettings(WorldManager.renderSetting currSet);
}

