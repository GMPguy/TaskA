using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static getStatic.WorldManager;

public abstract class DrawBase : MonoBehaviour {
    abstract public void initializeSystem();
    abstract public void beginLoad(Vector3 There);
    abstract public void load(int LoadID, int lastCall);
}

