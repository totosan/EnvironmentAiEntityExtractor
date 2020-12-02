# EntityExtractor - grab photo content and sort #

## Overview ##
This project has it's needs in having a huge collection of images of the  surrounding of my house. These are made with my surveillance cam (a old Huawei Smart Phone with IP Cam connected to ISpy). 
With a motion detection this setup tracks all situation, where something happens to my property. Either, if a person like a postman, a bike, an animal or a car is arriving, it took little snapshots and saved them to my OneDrive.

## What to expect ##
There are arrount 70k images for about 3 years. Now I just want for an AI project, these pictures arranged by content and idealy sorting the detected objects in the images to target folder for further usage.
It should something like ``<source-folder>`` with all images sorted to ``<postcar>``, ``<childbuggy>``, ``<truck>`` and others.

## side note: AI project ##
The project I am driving, that needs those images is for tuning that surveillance cam setup. Currently it's dumb, because it cannot tell me anything about, what happend. So, I decided, to make it smart. 
Together with Azure Custom Vision API, I created a ML Model, that can detect for specialities like 'The postman arrived' or 'The neighbour drove away'.
And, maybe in little future, the system will notify me, when a truck is trying to turn on my ground (it has indeed enough space to do so 😊 ) 
Interested in contributing this project? Take a look [here in Github (IoTEdgeObjectTracking)](https://github.com/totosan/IoTEdgeObjectTracking)

## Solution ##
There are two folder for arranging images. These applicate two steps:   
- **YoloAPICall**:  
scannes all images via TinyYoloV3 for general objects and sort detected results into different folder.
- **OnnxObjDet-cropped**:  
scannes all images in ``<sourcefolder>`` for objects pre-trained at Custom Vision API with the result images of first step.

Those results are cropped images - the object itself. So, with this approach I can build in a third step a classifier model.

At the end I have two models. One, that can do specialised object detection (knowing my sourroundings), and second, using a hirarchical tree analysis of an object detected by a general model with Yolo and specialized analysis with a classification of the object.

- **ML-Container**  
This is a folder containing the docker files for running the specialized models as container. One container is able to host a Rest API for object detection and the other can host a Rest API for Classification. This prepares the modules for use with IoT devices (Azure IoT Edge and Jetson Nano - in my case).