using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoCube : MonoBehaviour
{
    private WebCamTexture webcamTexture;
    // Start is called before the first frame update
    void Start()
    {
        SceneOrganiser.instance.videoCube = this.gameObject;
        string camName = WebCamTexture.devices[0].name;
        webcamTexture = new WebCamTexture(camName, 1080, 1080, 30);
        //rawImage.texture = webcamTexture
        webcamTexture.Play();
        gameObject.GetComponent<Renderer>().material.mainTexture = webcamTexture;
        Debug.Log("XXX!");
        SceneOrganiser.instance.analyzer._webcam = webcamTexture;
        SceneOrganiser.instance.analyzer.InitializeBlazeFace();
    }

    // Update is called once per frame
    void Update()
    {
        bool busy = SceneOrganiser.instance.stillWorking;
        if (busy == false)
        {
            
            //SceneOrganiser.instance.analyzer.StartAnalysis(webcamTexture);
        }
    }
}
