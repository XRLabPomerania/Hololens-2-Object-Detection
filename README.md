# Hololens 2 Object Detection with Unity

A template for Microsoft Hololens 2 Object Recognition with Unity. This project is part of the Interreg collaboration.

## Description

Our goal is to create a Microsoft Hololens 2 template which uses the Locatable Camera to do Image Classification and Object Recognition by using ONNX models and the Barracuda plugin. No Microsoft Azure needed.

## Getting Started

### Dependencies

* Unity 2021.3.1f1
* Burst 1.6.6 (preinstalled)
* Barracuda 3.0.0 (preinstalled)
* MRT Feature Tool (Mixed Reality Toolkit Foundation 2.8.0, Mixed Reality Toolkit Standard Assets 2.8.0, Mixed Reality OpenXR Plugin 1.4.0) (preinstalled)

### Installation

When opening the project, the MRTK Toolkit Configurator should open automatically. If not, go to Mixed Reality > Toolkit > Utilities > Configure Project for MRTK. Make sure that Universal Windows Platform is selected and MRTK and OpenXR are checkmarked.
Then go to File > Build Settings and select Architecture ARM64-bit, D3D Project and click Build. Save the Build where you prefer. If your compilation process does not succeed, its probably because Burst depends on some workloads which are not installed in Visual Studio. Check the Error in Unity to see if you have to install Visual Studio 2022 or if you can add the workloads to your Visual Studio version. The Workloads and individual components you probably need to add are: 
* Universal Windows Platform Development Workload
* Universal Windows Platform Support for vNNN build tools (ARM64)
* MSVC vNNN - VS NNNN C++ ARM build tools (latest)
* MSVC vNNN - VS NNNN C++ ARM64 build tools (latest)

### More information about dependencies & versions

If you have any trouble with this project, here are some information and resources which might help you:
* This project is built using Unity Version 2021.3.1f1. We may switch or add a 2020 Version, which will probably be uploaded as a new repository.
* To add Barracuda 3.0.0 (not 2.0.0) to your project, follow the steps of A__'s answer here: https://stackoverflow.com/questions/68460374/cant-find-barracuda-package-in-unity-registry
* Get the MRTK feature Tool here: https://www.microsoft.com/en-us/download/details.aspx?id=102778
* When opening Unity, you may need to go into the hierarchy in the top left, under MixedRealityPlayspace > Main Camera
  - in the Inspector on the right, scroll to the script Custom Vision Analyzer and add the "efficientnet-lite4-11 (NN Model)", "labels_map" and the BlazeFace model from the Assets/Models folder and the Cube Object from the hierarchy into the respective boxes in the Inspector panel.
  
### Preview (BlazeFace model used)


![ezgif com-gif-maker-3](https://user-images.githubusercontent.com/62561593/179522693-71c68e32-08a7-48c5-b4cd-14b12fd181c0.gif)
