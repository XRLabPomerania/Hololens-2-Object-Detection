# Hololens 2 Object Detection with Unity

A template for Microsoft Hololens 2 Object Recognition with Unity. This project is part of the Interreg collaboration.

## Description

Our goal is to create a Microsoft Hololens 2 template which uses the Locatable Camera to do Image Classification and Object Recognition by using ONNX models and the Barracuda plugin. No Microsoft Azure needed.

## Getting Started

### Dependencies

* Unity 2021.3.1f1
* Barracuda 3.0.0 
* MRT Feature Tool (Mixed Reality Toolkit Foundation 2.8.0, Mixed Reality Toolkit Standard Assets 2.8.0, Mixed Reality OpenXR Plugin 1.4.0)

### Installing

* This project is built using Unity Version 2021.3.1f1. We may switch or add a 2020 Version, which will probably be uploaded as a new repository.
* To add Barracuda 3.0.0 (not 2.0.0) to your project, follow the steps of A__'s answer here: https://stackoverflow.com/questions/68460374/cant-find-barracuda-package-in-unity-registry
* Get the MRTK feature Tool here: https://www.microsoft.com/en-us/download/details.aspx?id=102778
* When opening Unity, go into the hierarchy in the top left, under MixedRealityPlayspace > Main Camera
  - in the Inspector on the right, scroll to the script Custom Vision Analyzer and add the "efficientnet-lite4-11 (NN Model)", "labels_map" from the Assets/Models folder and the Cube Object from the hierarchy into the respective boxes in the Inspector panel.
