using UnityEngine;
using UnityEngine.UI;

public class ImagePlane : MonoBehaviour
{

    public static ImagePlane Instance;
    public RawImage rawImage;
    //AspectRatioFitter fitter;
    WebCamTexture webcamTexture;
    bool ratioSet;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SceneOrganiser.instance.imagePlane = gameObject;
    }

    void Update()
    {
    }

    void SetAspectRatio()
    {
    }

}
