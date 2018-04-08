using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public Transform MapContainer;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


}

public abstract class AMapGenerator : MonoBehaviour
{
    public abstract void GenerateMap();
}
