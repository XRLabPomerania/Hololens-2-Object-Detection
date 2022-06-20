using UnityEngine;
using Unity.Barracuda;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System;

public class CustomVisionAnalyzer : MonoBehaviour
{
    const int IMAGE_SIZE = 224;
    const string INPUT_NAME = "images";
    const string OUTPUT_NAME = "Softmax";
    bool previousRoundEnded = true;

    WebCamTexture webcamTexture;
    cast_ray castRay;

    [Header("Model Stuff")]
    public NNModel modelFile;
    public TextAsset labelAsset;

    [Header("Scene Stuff")]
    public Preprocess preprocess;
    //public Text uiText;

    string[] labels;
    IWorker worker;

    private void Awake()
    {
    }

    void Start()
	{
        castRay = SceneOrganiser.instance.castRay;
        InitWebCam();
        var model = ModelLoader.Load(modelFile);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
        LoadLabels();
        
    }

    void InitWebCam()
    {
        string camName = WebCamTexture.devices[0].name;
        webcamTexture = new WebCamTexture(camName, Screen.width, Screen.height, 30);
        //rawImage.texture = webcamTexture;
        webcamTexture.Play();
    }

    void LoadLabels()
    {
        var stringArray = labelAsset.text.Split('"').Where((item, index) => index % 2 != 0);
        labels = stringArray.Where((x, i) => i % 2 != 0).ToArray();
    }

    void Update()
	{
        if (previousRoundEnded)
        {
            /*
            Texture2D tex = new Texture2D(IMAGE_SIZE, IMAGE_SIZE, TextureFormat.RGB24, false);
            ImagePlane.Instance.GetComponent<Renderer>().material.mainTexture = tex;
            tex.Apply();
            */
            StartAnalysis();
            previousRoundEnded = false;
        }
        
    }

    public void StartAnalysis()
    {
        //if (webCamTexture.didUpdateThisFrame && webCamTexture.width > 100
        preprocess.ScaleAndCropImage(webcamTexture, IMAGE_SIZE, RunModel);
    }

    void RunModel(byte[] pixels)
    {
        StartCoroutine(RunModelRoutine(pixels));
    }

    IEnumerator RunModelRoutine(byte[] pixels)
    {        
        Texture2D tex = new Texture2D(IMAGE_SIZE, IMAGE_SIZE, TextureFormat.RGB24, false);
        tex.LoadRawTextureData(pixels);
        tex.Apply();
        Debug.Log(tex.width + " " + tex.height);
        ImagePlane.Instance.GetComponent<Renderer>().material.mainTexture = tex;

        Tensor tensor = TransformInput(pixels);

        var inputs = new Dictionary<string, Tensor> {
            { INPUT_NAME, tensor }
        };
        
        worker.Execute(inputs);
        Tensor outputTensor = worker.PeekOutput(OUTPUT_NAME);

        //get largest output
        List<float> temp = outputTensor.ToReadOnlyArray().ToList();
        float max = temp.Max();
        int index = temp.IndexOf(max);

        //uiText.text = labels[index];
        Debug.Log(labels[index]);
        //castRay.CastToPixel(924, 512);
        //castRay.ChangeLastLabel(labels[index]);
        castRay.CastToPixel(labels[index]);
        tensor.Dispose();
        outputTensor.Dispose();
        previousRoundEnded = true;
        yield return null;
    }

    Tensor TransformInput(byte[] pixels)
    {
        float[] transformedPixels = new float[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            transformedPixels[i] = (pixels[i] - 127f) / 128f;
        }
        return new Tensor(1, IMAGE_SIZE, IMAGE_SIZE, 3, transformedPixels);
    }

}



