# Yolo Object Detector
This project contains a .NET library and application to detect object in an RTSP video stream using a Yolo model, ML.NET and OpenCV.
This is a work in progress. 

- The Yolov4 implementation works 
- The YoloV2 implementation in incomplete and does not yet work

![image](https://user-images.githubusercontent.com/14876765/177539687-e8275690-a8a9-4bdc-bb9f-b33aed863cc4.png)

**NOTE**
Due to file size restrictions here in Github I have not included the ONNX models in the repo.

## Build and run
- Using VS 2022
- Clone the repo
- Download the Yolov4.onnx model
- Build the solution
- Copy the model to the build output folder of the application
- Set the rtsp url including logon credentials as run argument
- Run the application

# Tasks
[ ] Application
 [X] Initial app connect to rtsp
 [ ] Propper parameter parsing
 [ ] Add ui
[ ] Library
 [ ] Common
  [ ] Unify code for algoritms
 [ ] Yolov4 support
  [X] Initial prototype
 [ ] TinyYolov2 support
  [ ] Initial prototype
