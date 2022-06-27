using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEngine.XR.WSA.Input;
using UnityEngine.Windows.WebCam;
using UnityEngine.Windows;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine.Windows.Speech;
using Unity.Barracuda;
using UnityEngine.Rendering;
using UnityEngine.Events;
using UnityEngine.UI;


/// <summary>
/// Takes the image and handles tap gestures
/// IMPORTANT: when the cursor is green, the camera is available to take an image, when its red the camera is busy
/// </summary>
/// 
public class ImageCapture : MonoBehaviour
{
    public static ImageCapture Instance;
    internal bool captureIsActive;
    internal string filePath = string.Empty;
    private KeywordRecognizer keywordRecognizer;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Clean up LocalState folder of this app from all photos stored
        DirectoryInfo info = new DirectoryInfo(Application.persistentDataPath);
        var fileInfo = info.GetFiles();
        foreach (var file in fileInfo)
        {
            try
            {
                file.Delete();
            }
            catch
            {
                Debug.LogFormat("Cannot delete file: ", file.Name);
            }
        }

    }


    public void StartTheCapturingProcess()
    {
        SceneOrganiser.instance.cursor.GetComponent<Renderer>().material.color = UnityEngine.Color.red;
        Invoke("ExecuteImageCaptureAnalysis", 0);
    }




}
