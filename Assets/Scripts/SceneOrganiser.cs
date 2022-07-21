using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Barracuda;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.Windows.WebCam;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.UI;

/// <summary>
/// Setting up the camera + calculate position of detected object in the real world + place a tag label into the world 
/// </summary>

public class SceneOrganiser : MonoBehaviour
{
    public static SceneOrganiser instance;
    RayCastHelper ray;
    internal GameObject cursor;
    public RayCastHelper castRay;
    internal GameObject imagePlane;
    public CustomVisionAnalyzer analyzer;
    internal float probabilityThreshold = 0.99f;
    public bool stillWorking = false;

    //internal GameObject videoCube;

    public GameObject prefab;
    public int pix_x = 960;
    public int pix_y = 540;

    // Barracuda Stuff
    public IWorker worker;
    public string[] labels;

    WebCamTexture globalTex;
    int counter = 0;

    private void Awake()
    {
        instance = this;
       // gameObject.AddComponent<CustomVisionObjects>();
        gameObject.AddComponent<Preprocess>();
       
    }

    void Start()
    {
        castRay = gameObject.GetComponent<RayCastHelper>();

    }


    private void Update()
    {
        
    }
}
