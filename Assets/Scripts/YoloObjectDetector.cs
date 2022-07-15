using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;


public class YoloObjectDetector : MonoBehaviour
{

    public YoloObjectDetector(ResourceSet resources)
      => AllocateObjects(resources);


    public void Dispose()
      => DeallocateObjects();

    public void ProcessImage(Texture sourceTexture, float threshold)
      => RunModel(sourceTexture, threshold);

    public IEnumerable<Detection> Detections
      => _readCache.Cached;

    public ComputeBuffer DetectionBuffer
      => _buffers.post2;

    public ComputeBuffer DetectionCountBuffer
      => _buffers.countRead;

    public void SetIndirectDrawCount(ComputeBuffer drawArgs)
      => ComputeBuffer.CopyCount(_buffers.post2, drawArgs, sizeof(uint));

    ResourceSet _resources;
    Config _config;
    IWorker _worker;

    (ComputeBuffer preprocess,
     RenderTexture feature1,
     RenderTexture feature2,
     ComputeBuffer post1,
     ComputeBuffer post2,
     ComputeBuffer counter,
     ComputeBuffer countRead) _buffers;

    DetectionCache _readCache;

    void AllocateObjects(ResourceSet resources)
    {
        // NN model loading
        var model = ModelLoader.Load(resources.model);

        // Private object initialization
        _resources = resources;
        _config = new Config(resources, model);
        _worker = model.CreateWorker();

        // Buffer allocation
        _buffers.preprocess = new ComputeBuffer
          (_config.InputFootprint, sizeof(float));

        _buffers.feature1 = RTUtil.NewFloat
          (_config.FeatureDataSize, _config.FeatureMap1Footprint);

        _buffers.feature2 = RTUtil.NewFloat
          (_config.FeatureDataSize, _config.FeatureMap2Footprint);

        _buffers.post1 = new ComputeBuffer
          (Config.MaxDetection, Detection.Size);

        _buffers.post2 = new ComputeBuffer
          (Config.MaxDetection, Detection.Size, ComputeBufferType.Append);

        _buffers.counter = new ComputeBuffer
          (1, sizeof(uint), ComputeBufferType.Counter);

        _buffers.countRead = new ComputeBuffer
          (1, sizeof(uint), ComputeBufferType.Raw);

        // Detection data read cache initialization
        _readCache = new DetectionCache(_buffers.post2, _buffers.countRead);
    }

    void DeallocateObjects()
    {
        _worker?.Dispose();
        _worker = null;

        _buffers.preprocess?.Dispose();
        _buffers.preprocess = null;

        ObjectUtil.Destroy(_buffers.feature1);
        _buffers.feature1 = null;

        ObjectUtil.Destroy(_buffers.feature2);
        _buffers.feature2 = null;

        _buffers.post1?.Dispose();
        _buffers.post1 = null;

        _buffers.post2?.Dispose();
        _buffers.post2 = null;

        _buffers.counter?.Dispose();
        _buffers.counter = null;

        _buffers.countRead?.Dispose();
        _buffers.countRead = null;
    }

    void RunModel(Texture source, float threshold)
    {
        Debug.Log("YAAZ");
        // Preprocessing
        var pre = _resources.preprocess;
        pre.SetInt("Size", _config.InputWidth);
        pre.SetTexture(0, "Image", source);
        pre.SetBuffer(0, "Tensor", _buffers.preprocess);
        pre.DispatchThreads(0, _config.InputWidth, _config.InputWidth, 1);

        // NN worker invocation
        using (var t = new Tensor(_config.InputShape, _buffers.preprocess))
            _worker.Execute(t);

        // NN output retrieval
        //_worker.CopyOutput("Identity", _buffers.feature1);
        //_worker.CopyOutput("Identity_1", _buffers.feature2);

        // Counter buffer reset
        _buffers.post2.SetCounterValue(0);
        _buffers.counter.SetCounterValue(0);

        // First stage postprocessing: detection data aggregation
        var post1 = _resources.postprocess1;
        post1.SetInt("ClassCount", _config.ClassCount);
        post1.SetFloat("Threshold", threshold);
        post1.SetBuffer(0, "Output", _buffers.post1);
        post1.SetBuffer(0, "OutputCount", _buffers.counter);

        // (feature map 1)
        var width1 = _config.FeatureMap1Width;
        post1.SetTexture(0, "Input", _buffers.feature1);
        post1.SetInt("InputSize", width1);
        post1.SetFloats("Anchors", _config.AnchorArray1);
        post1.DispatchThreads(0, width1, width1, 1);

        // (feature map 2)
        var width2 = _config.FeatureMap2Width;
        post1.SetTexture(0, "Input", _buffers.feature2);
        post1.SetInt("InputSize", width2);
        post1.SetFloats("Anchors", _config.AnchorArray2);
        post1.DispatchThreads(0, width2, width2, 1);

        // Second stage postprocessing: overlap removal
        var post2 = _resources.postprocess2;
        post2.SetFloat("Threshold", 0.5f);
        post2.SetBuffer(0, "Input", _buffers.post1);
        post2.SetBuffer(0, "InputCount", _buffers.counter);
        post2.SetBuffer(0, "Output", _buffers.post2);
        post2.Dispatch(0, 1, 1, 1);

        // Bounding box count after removal
        ComputeBuffer.CopyCount(_buffers.post2, _buffers.countRead, 0);

        // Cache data invalidation
        _readCache.Invalidate();
    }

    public sealed class ResourceSet : ScriptableObject
    {
        // To change: load model 
        public NNModel model;
        public float[] anchors = new float[12];
        public ComputeShader preprocess;
        public ComputeShader postprocess1;
        public ComputeShader postprocess2;
    }

    public readonly struct Detection
    {
        public readonly float x, y, w, h;
        public readonly uint classIndex;
        public readonly float score;

        // sizeof(Detection)
        public static int Size = 6 * sizeof(int);

        // String formatting
        public override string ToString()
          => $"({x},{y})-({w}x{h}):{classIndex}({score})";
    };

    struct Config
    {
        #region Compile-time constants

        // These values must be matched with the ones defined in Common.hlsl.
        public const int MaxDetection = 512;
        public const int AnchorCount = 3;

        #endregion

        #region Variables from tensor shapes

        public int InputWidth { get; private set; }
        public int ClassCount { get; private set; }
        public int FeatureMap1Width { get; private set; }
        public int FeatureMap2Width { get; private set; }

        #endregion

        #region Data size calculation properties

        public int FeatureDataSize => (5 + ClassCount) * AnchorCount;
        public int InputFootprint => InputWidth * InputWidth * 3;
        public int FeatureMap1Footprint => FeatureMap1Width * FeatureMap1Width;
        public int FeatureMap2Footprint => FeatureMap2Width * FeatureMap2Width;

        #endregion

        #region Tensor shape utilities

        public TensorShape InputShape
          => new TensorShape(1, InputWidth, InputWidth, 3);

        public TensorShape FlattenFeatureMap1
          => new TensorShape(1, FeatureMap1Footprint, FeatureDataSize, 1);

        public TensorShape FlattenFeatureMap2
          => new TensorShape(1, FeatureMap2Footprint, FeatureDataSize, 1);

        #endregion

        #region Anchor arrays (16 byte aligned for compute shader use)

        public float[] AnchorArray1 { get; private set; }
        public float[] AnchorArray2 { get; private set; }

        static float[] MakeAnchorArray
          (float[] anchors, int i1, int i2, int i3, float scale)
          => new float[]
              { anchors[i1 * 2 + 0] * scale, anchors[i1 * 2 + 1] * scale, 0, 0,
            anchors[i2 * 2 + 0] * scale, anchors[i2 * 2 + 1] * scale, 0, 0,
            anchors[i3 * 2 + 0] * scale, anchors[i3 * 2 + 1] * scale, 0, 0 };

        #endregion

        #region Constructor

        public Config(ResourceSet resources, Model model)
        {
            var inShape = model.inputs[0].shape;
            var out1Shape = model.GetShapeByName(model.outputs[0]).Value;
            var out2Shape = model.GetShapeByName(model.outputs[1]).Value;

            InputWidth = inShape[6]; // 6: width
            ClassCount = out1Shape.channels / AnchorCount - 5;
            FeatureMap1Width = out1Shape.width;
            FeatureMap2Width = out2Shape.width;

            var scale = 1.0f / InputWidth;
            AnchorArray1 = MakeAnchorArray(resources.anchors, 3, 4, 5, scale);
            AnchorArray2 = MakeAnchorArray(resources.anchors, 1, 2, 3, scale);
        }

        #endregion
    }

    #region GPU to CPU readback helpers

    sealed class DetectionCache
    {
        ComputeBuffer _dataBuffer;
        ComputeBuffer _countBuffer;

        Detection[] _cached;
        int[] _countRead = new int[1];

        public DetectionCache(ComputeBuffer data, ComputeBuffer count)
          => (_dataBuffer, _countBuffer) = (data, count);

        public Detection[] Cached => _cached ?? UpdateCache();

        public void Invalidate() => _cached = null;

        public Detection[] UpdateCache()
        {
            _countBuffer.GetData(_countRead, 0, 0, 1);
            var count = _countRead[0];

            _cached = new Detection[count];
            _dataBuffer.GetData(_cached, 0, 0, count);

            return _cached;
        }
    }

    #endregion

}

static class ComputeShaderExtensions
{
    public static void DispatchThreads
      (this ComputeShader compute, int kernel, int x, int y, int z)
    {
        uint xc, yc, zc;
        compute.GetKernelThreadGroupSizes(kernel, out xc, out yc, out zc);

        x = (x + (int)xc - 1) / (int)xc;
        y = (y + (int)yc - 1) / (int)yc;
        z = (z + (int)zc - 1) / (int)zc;

        compute.Dispatch(kernel, x, y, z);
    }
}

//static class IWorkerExtensions
//{
//    public static void CopyOutput
//      (this IWorker worker, string tensorName, RenderTexture rt)
//    {
//        var output = worker.PeekOutput(tensorName);
//        var shape = new TensorShape(1, rt.height, rt.width, 1);
//        using var tensor = output.Reshape(shape);
//        tensor.ToRenderTexture(rt);
//    }
//}

static class RTUtil
{
    public static RenderTexture NewFloat(int w, int h)
      => new RenderTexture(w, h, 0, RenderTextureFormat.RFloat);
}

static class ObjectUtil
{
    public static void Destroy(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying)
            Object.Destroy(o);
        else
            Object.DestroyImmediate(o);
    }
}
