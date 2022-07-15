using UnityEngine;
using UI = UnityEngine.UI;
using System.Collections.Generic;
using Unity.Barracuda;
using System.Runtime.InteropServices;

namespace MediaPipe.BlazeFace
{

    public sealed class RunInferenceBlazeFace : MonoBehaviour
    {
        private string _deviceName = "";
        [SerializeField] Vector2Int _resolution = new Vector2Int(1080, 1080);

        WebCamTexture _webcam;
        RenderTexture _buffer;

        private Texture2D _image = null;
        [SerializeField, Range(0, 1)] float _threshold = 0.75f;
        [SerializeField] UI.RawImage _previewUI = null;
        [SerializeField] Marker _markerPrefab = null;

        Marker[] _markers = new Marker[16];

        // Maximum number of detections. This value must be matched with
        // MAX_DETECTION in Common.hlsl.
        const int MaxDetection = 64;

        public NNModel _model;
        public ComputeShader _preprocess;
        public ComputeShader _postprocess1;
        public ComputeShader _postprocess2;
        ComputeBuffer _preBuffer;
        ComputeBuffer _post1Buffer;
        ComputeBuffer _post2Buffer;
        ComputeBuffer _countBuffer;
        IWorker _worker;
        int _size;

        public GameObject webglWarning;
        public UI.Dropdown _cameraDropdown;
        public UI.Dropdown _backendDropdown;

        //
        // Detection structure. The layout of this structure must be matched with
        // the one defined in Common.hlsl.
        //
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Detection
        {
            // Bounding box
            public readonly Vector2 center;
            public readonly Vector2 extent;

            // Key points
            public readonly Vector2 leftEye;
            public readonly Vector2 rightEye;
            public readonly Vector2 nose;
            public readonly Vector2 mouth;
            public readonly Vector2 leftEar;
            public readonly Vector2 rightEar;

            // Confidence score [0, 1]
            public readonly float score;

            // Padding
            public readonly float pad1, pad2, pad3;

            // sizeof(Detection)
            public const int Size = 20 * sizeof(float);
        };

        void Start()
        {

#if UNITY_WEBGL
            webglWarning.SetActive(true);
#endif

            _webcam = new WebCamTexture(_deviceName, _resolution.x, _resolution.y);
            _buffer = new RenderTexture(_resolution.x, _resolution.y, 0);
            _webcam.requestedFPS = 30;
            _webcam.Play();

            Screen.orientation = ScreenOrientation.LandscapeLeft;

            AddCameraOptions();
            AddBackendOptions();

            //initialization
            AllocateObjects();

            // Marker population
            for (var i = 0; i < _markers.Length; i++)
                _markers[i] = Instantiate(_markerPrefab, _previewUI.transform);

            // Static image test: Run the detector once.
            if (_image != null) runInference(_image);
        }

        void Update()
        {
            // Format video inpout
            if (!_webcam.didUpdateThisFrame) return;

            var aspect1 = (float)_webcam.width / _webcam.height;
            var aspect2 = (float)_resolution.x / _resolution.y;
            var gap = aspect2 / aspect1;

            var vflip = _webcam.videoVerticallyMirrored;
            var scale = new Vector2(gap, vflip ? -1 : 1);
            var offset = new Vector2((1 - gap) / 2, vflip ? 1 : 0);

            Graphics.Blit(_webcam, _buffer, scale, offset);
        }

        void LateUpdate()
        {
            // Webcam test: Run the detector every frame.
            runInference(_buffer);
        }

        public void AddCameraOptions()
        {
            List<string> options = new List<string>();
            foreach (var option in WebCamTexture.devices)
            {
                options.Add(option.name);
            }
            _cameraDropdown.ClearOptions();
            _cameraDropdown.AddOptions(options);
        }

        public void AddBackendOptions()
        {
            List<string> options = new List<string>();
#if !UNITY_WEBGL
            options.Add("ComputePrecompiled");
#endif
            //options.Add("PixelShader"); //not supported with this demo
            //options.Add("CSharpBurst"); //not supported with this demo
            _backendDropdown.ClearOptions();
            _backendDropdown.AddOptions(options);
        }

        public void SwapCamera()
        {
            _webcam.Stop();
            _webcam.deviceName = _cameraDropdown.options[_cameraDropdown.value].text;
            _webcam.Play();
        }

        public void ProcessImage(Texture image, float threshold = 0.75f)
          => ExecuteML(image, threshold);

        public void AllocateObjects()
        {
            var model = ModelLoader.Load(_model);
            _size = model.inputs[0].shape[6]; // Input tensor width

            _preBuffer = new ComputeBuffer(_size * _size * 3, sizeof(float));

            _post1Buffer = new ComputeBuffer
              (MaxDetection, Detection.Size, ComputeBufferType.Append);

            _post2Buffer = new ComputeBuffer
              (MaxDetection, Detection.Size, ComputeBufferType.Append);

            _countBuffer = new ComputeBuffer
              (1, sizeof(uint), ComputeBufferType.Raw);

            if (_backendDropdown.options[_backendDropdown.value].text == "ComputePrecompiled")
            {
                _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
            }
            else if (_backendDropdown.options[_backendDropdown.value].text == "PixelShader")
            {
                _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.PixelShader, model);
            }
            else if (_backendDropdown.options[_backendDropdown.value].text == "CSharpBurst")
            {
                _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model);
            }
        }

        void ExecuteML(Texture source, float threshold)
        {
            // Reset the compute buffer counters.
            _post1Buffer.SetCounterValue(0);
            _post2Buffer.SetCounterValue(0);

            // Preprocessing
            var pre = _preprocess;
            pre.SetInt("_ImageSize", _size);
            pre.SetTexture(0, "_Texture", source);
            pre.SetBuffer(0, "_Tensor", _preBuffer);
            pre.Dispatch(0, _size / 8, _size / 8, 1);

            // Run the BlazeFace model.
            using (var tensor = new Tensor(1, _size, _size, 3, _preBuffer))
                _worker.Execute(tensor);

            // Output tensors -> Temporary render textures
            var scores1RT = _worker.CopyOutputToTempRT("Identity", 1, 512);
            var scores2RT = _worker.CopyOutputToTempRT("Identity_1", 1, 384);
            var boxes1RT = _worker.CopyOutputToTempRT("Identity_2", 16, 512);
            var boxes2RT = _worker.CopyOutputToTempRT("Identity_3", 16, 384);

            // 1st postprocess (bounding box aggregation)
            var post1 = _postprocess1;
            post1.SetFloat("_ImageSize", _size);
            post1.SetFloat("_Threshold", threshold);

            post1.SetTexture(0, "_Scores", scores1RT);
            post1.SetTexture(0, "_Boxes", boxes1RT);
            post1.SetBuffer(0, "_Output", _post1Buffer);
            post1.Dispatch(0, 1, 1, 1);

            post1.SetTexture(1, "_Scores", scores2RT);
            post1.SetTexture(1, "_Boxes", boxes2RT);
            post1.SetBuffer(1, "_Output", _post1Buffer);
            post1.Dispatch(1, 1, 1, 1);

            // Release the temporary render textures.
            RenderTexture.ReleaseTemporary(scores1RT);
            RenderTexture.ReleaseTemporary(scores2RT);
            RenderTexture.ReleaseTemporary(boxes1RT);
            RenderTexture.ReleaseTemporary(boxes2RT);

            // Retrieve the bounding box count.
            ComputeBuffer.CopyCount(_post1Buffer, _countBuffer, 0);

            // 2nd postprocess (overlap removal)
            var post2 = _postprocess2;
            post2.SetBuffer(0, "_Input", _post1Buffer);
            post2.SetBuffer(0, "_Count", _countBuffer);
            post2.SetBuffer(0, "_Output", _post2Buffer);
            post2.Dispatch(0, 1, 1, 1);

            // Retrieve the bounding box count after removal.
            ComputeBuffer.CopyCount(_post2Buffer, _countBuffer, 0);

            // Read cache invalidation
            _post2ReadCache = null;
        }

        void runInference(Texture input)
        {
            // Face detection
            ProcessImage(input, _threshold);

            // Marker update
            var i = 0;

            foreach (var detection in Detections)
            {
                if (i == _markers.Length) break;
                var marker = _markers[i++];
                //marker.detection = detection;
                marker.gameObject.SetActive(true);
            }

            for (; i < _markers.Length; i++)
                _markers[i].gameObject.SetActive(false);

            // UI update
            _previewUI.texture = input;
        }

        public IEnumerable<Detection> Detections
            => _post2ReadCache ?? UpdatePost2ReadCache();

        Detection[] _post2ReadCache;
        int[] _countReadCache = new int[1];

        Detection[] UpdatePost2ReadCache()
        {
            _countBuffer.GetData(_countReadCache, 0, 0, 1);
            var count = _countReadCache[0];

            _post2ReadCache = new Detection[count];
            _post2Buffer.GetData(_post2ReadCache, 0, 0, count);

            return _post2ReadCache;
        }

        void OnDestroy()
        {
            Destroy(_webcam);
            Destroy(_buffer);
            Dispose();
        }

        public void Dispose()
            => DeallocateObjects();

        void DeallocateObjects()
        {
            _preBuffer?.Dispose();
            _preBuffer = null;

            _post1Buffer?.Dispose();
            _post1Buffer = null;

            _post2Buffer?.Dispose();
            _post2Buffer = null;

            _countBuffer?.Dispose();
            _countBuffer = null;

            _worker?.Dispose();
            _worker = null;
        }

    }

    static class IWorkerExtensions
    {
        //
        // Retrieves an output tensor from a NN worker and returns it as a
        // temporary render texture. The caller must release it using
        // RenderTexture.ReleaseTemporary.
        //
        public static RenderTexture
            CopyOutputToTempRT(this IWorker worker, string name, int w, int h)
        {
            var fmt = RenderTextureFormat.RFloat;
            var shape = new TensorShape(1, h, w, 1);
            var rt = RenderTexture.GetTemporary(w, h, 0, fmt);
            using (var tensor = worker.PeekOutput(name).Reshape(shape))
                tensor.ToRenderTexture(rt);
            return rt;
        }
    }

    //public sealed class Marker : MonoBehaviour
    //{
    //    public RunInferenceBlazeFace.Detection detection { get; set; }

    //    [SerializeField] RectTransform[] _keyPoints;

    //    Marker _marker;
    //    RectTransform _xform;
    //    RectTransform _parent;
    //    UI.Text _label;

    //    //void SetKeyPoint(RectTransform xform, Vector2 point)
    //    //  => xform.anchoredPosition =
    //    //       point * _parent.rect.size - _xform.anchoredPosition;

    //    //void Start()
    //    //{
    //    //    _marker = GetComponent<Marker>();
    //    //    _xform = GetComponent<RectTransform>();
    //    //    _parent = (RectTransform)_xform.parent;
    //    //    _label = GetComponentInChildren<UI.Text>();
    //    //}

    //    //void LateUpdate()
    //    //{
    //    //    var detection = _marker.detection;

    //    //    // Bounding box center
    //    //    _xform.anchoredPosition = detection.center * _parent.rect.size;

    //    //    // Bounding box size
    //    //    var size = detection.extent * _parent.rect.size;
    //    //    _xform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
    //    //    _xform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);

    //    //    // Key points
    //    //    SetKeyPoint(_keyPoints[0], detection.leftEye);
    //    //    SetKeyPoint(_keyPoints[1], detection.rightEye);
    //    //    SetKeyPoint(_keyPoints[2], detection.nose);
    //    //    SetKeyPoint(_keyPoints[3], detection.mouth);
    //    //    SetKeyPoint(_keyPoints[4], detection.leftEar);
    //    //    SetKeyPoint(_keyPoints[5], detection.rightEar);

    //    //    // Label
    //    //    _label.text = $"{(int)(detection.score * 100)}?";
    //    //}
    //}

} // namespace MediaPipe.BlazeFace
