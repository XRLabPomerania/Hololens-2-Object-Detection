using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Barracuda;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.Networking;
//using OpenCvSharp;
using UnityEngine.Windows.WebCam;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine.UI;

/// <summary>
/// Setting up the camera + calculate position of detected object in the real world + place a tag label into the world 
/// </summary>

public class SceneOrganiser : MonoBehaviour
{
    public static SceneOrganiser instance;
    cast_ray ray;
    internal GameObject cursor;
    public cast_ray castRay;
    internal GameObject imagePlane;
    CustomVisionAnalyzer analyzer;
    internal float probabilityThreshold = 0.99f;

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
        analyzer = gameObject.AddComponent<CustomVisionAnalyzer>();
        gameObject.AddComponent<CustomVisionObjects>();
        //gameObject.AddComponent<cast_ray>();
        //preprocess = gameObject.AddComponent<Preprocess>();
    }
    /*
        string camName = WebCamTexture.devices[0].name;
        WebCamTexture webcamTexture = new WebCamTexture(camName, Screen.width, Screen.height, 30);
        webcamTexture.Play();
        globalTex = webcamTexture;
     */
    void Start()
    {
        Debug.Log("CASI IS " + castRay);

    }


    private void Update()
    {
        
    }


    public void FinaliseLabel(AnalysisRootObject analysisObject)
    {
        if (analysisObject != null)
        {
            List<Prediction> sortedPredictions = new List<Prediction>();
            sortedPredictions = analysisObject.predictions.OrderByDescending(p => p.probability).ToList();
            sortedPredictions = analysisObject.predictions;
            foreach (Prediction prediction in sortedPredictions)
            {
                int ind = sortedPredictions.IndexOf(prediction);
            }
            Prediction bestPrediction = new Prediction();
            double best = sortedPredictions.Max(e => e.probability);
            bestPrediction = sortedPredictions[0];

            if (bestPrediction.probability > probabilityThreshold)
            {
                ray = SceneOrganiser.instance.GetComponent<cast_ray>();
                //ray.ChangeLastLabel(bestPrediction.tagName);
            }
        }
        cursor.GetComponent<Renderer>().material.color = Color.green;

    }
}
