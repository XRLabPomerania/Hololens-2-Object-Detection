using UnityEngine;
using Unity.Barracuda;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Explicit)]
public struct Color32Array
{
    [FieldOffset(0)]
    public byte[] byteArray;

    [FieldOffset(0)]
    public Color32[] colors;
}

public class CustomVisionAnalyzer : MonoBehaviour
{
    const int IMAGE_SIZE = 224;
    const string INPUT_NAME = "images";
    const string OUTPUT_NAME = "Softmax";

    private Preprocess preprocess;

    WebCamTexture webcamTexture;
    cast_ray castRay;
    Model model;

    [Header("Model Stuff")]
    public NNModel modelFile;
    public TextAsset labelAsset;

    string[] labels;
    IWorker worker;

    private void Awake()
    {
    }

    void Start()
	{
        SceneOrganiser.instance.analyzer = this;
        preprocess = SceneOrganiser.instance.GetComponent<Preprocess>();
        castRay = SceneOrganiser.instance.castRay;
        model = ModelLoader.Load(modelFile);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);

        LoadLabels();
        
    }


    void LoadLabels()
    {
        var stringArray = labelAsset.text.Split('"').Where((item, index) => index % 2 != 0);
        labels = stringArray.Where((x, i) => i % 2 != 0).ToArray();
    }

    void Update()
	{
    }

    public void StartAnalysis(WebCamTexture wtex)
    {
        preprocess.ScaleAndCropImage(wtex, IMAGE_SIZE, RunModel);
    }


    void RunModel(byte[] pixels)
    {
        StartCoroutine(RunModelRoutine(pixels));
    }

    IEnumerator RunModelRoutine(byte[] pixels)
    {
        Tensor tensor = TransformInput(pixels);
        var inputs = new Dictionary<string, Tensor> {
            { INPUT_NAME, tensor }
        };
        
        worker.Execute(inputs);
        Tensor outputTensor = worker.PeekOutput(OUTPUT_NAME);

        List<float> temp = outputTensor.ToReadOnlyArray().ToList();
        float max = temp.Max();
        int index = temp.IndexOf(max);
        Debug.Log(labels[index]);
        castRay.CastToPixel(labels[index]);
        tensor.Dispose();
        outputTensor.Dispose();
        SceneOrganiser.instance.stillWorking = false;
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





