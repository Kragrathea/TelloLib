
aTello for Android
======

## About aTello
aTello is an alternative to the stock Tello android app.

## Homepage
https://tellopilots.com/threads/atello-android-development-thread.530/

## Installing the APK.
https://www.wikihow.tech/Install-APK-Files-on-Android

## Instructions

<img src="https://github.com/Kragrathea/TelloLib/blob/master/Media/ScreenDiagram1.jpg" width="900" >

1. Network status area. This bar will be RED if not connected to the Tello network. Tap to connect in that case.
2. Takeoff/Land button
3. Throw takeoff button
4. Take picture button. Hold to switch to aspect ratio to 16x9. (not fully working yet)
5. Show pictures. Not fully working.
6. Settings. Joystick config and Jpeg quality.
7. Joystick axis status (for debugging)
8. Aircraft status area.

Joystick is currently supported for generic Android Bluetooth/OTG controllers and PS3 style controllers. Joystick type is selected in the settings page. If your joystick doesn't work First check to make sure it works on other Android apps. If it does then post an issue including your controller brand.

These are the button mappings on the controller for takeoff, land, take picture and speed mode. 

<img src="https://github.com/Kragrathea/TelloLib/blob/master/Media/PS3_diagram.png" width="600" >

**Start** (Hold) = Takeoff/Land.  
**Select** (Hold) = Land.  
**R1** = Take Picture.  
**R2** = Speed mode.  
**L1** = Record.  
**L2+DPad** = Flip

## Settings screen

<img src="https://github.com/Kragrathea/TelloLib/blob/master/Media/aTelloSettings.png" width="900" >

1. Convert H264 to MP4.
2. Joystick type Generic for Android compatible bluetooth or OTG. PS3 or PS4 also supported.
3. Photo quality. Jpeg photo quality.
4. Exposure.
5. Cache video. If on video is always being saved to the /cache folder when it is not being recorded.
6. Video bit rate. 
7. Video iFrame rate. How often keyframes are sent. Adjusting this **may** change video quality.


Pictures are stored in <storage>/aTello/pics. Video is stored in <storage>/aTello/video.

Video is handled a bit differently. Currently the video stream is **always** being "recorded" as in saved to storage. This can be disabled in the options. I like it this way because I am always forgetting to press record or I don't decide until afterward that I wanted somethig recorded. The "Record" button just toggles where the file is saved. When not recording the files are stored in aTello/videos/cache. When recording the file is saved in aTello/videos. This allows you to explicitly have video "recorded" into a dicreet piece.

Currently the video is saved as the raw H264 that is coming from the drone and must be converted to mp4 for playback. There are two buttons on the settings screen that will handle the conversion to .mp4. For the most part use the Convert All Videos button. 

## On Video quality
The Tello has a very good camera and takes good pictures, but the video feed is a letdown. The compression is high and the WIFI signal is weak leading to video breakup and lost frames. aTello uses the same video feed as the official app. And so all things being equal the video quality should be the same. That said here are a few things that are not equal. 

**Edit. Video quality is not as good as stock app. I am looking into why.**

Given the inherent low quality video my approach to the video feed is to maximize the user experience while flying the drone as opposed to make the recorded video look as possible. To that end:

-By default aTello uses a 4:3 aspect ratio and a video resolution of 960x720. The official app records video only at 16x9 1280x720. You can switch to 16x9 mode in aTello by holding the onscreen camera button down for a moment. This will result in the video being cropped the same as the official app and will apparently increase the compression quality since less there is less of the frame to compress.

-The official app chooses to drop frames when it encounters an error resulting in a black or frozen frame. aTello will draw the frame even if there are errors. This means you will get a frame with some amount of garbage instead of black. In some cases this is unnoticeable and in others it results in a mess. I think it is a desirable trade-off.

-You can potentially improve the video quality by adjusting the Video bit rate and iFrame interval in the settings. A lower bitrate should help with lost frames or corrupted frames at the expense of quality. A higher iFrame rate will allow it to recover from bad frames faster but will again cost some quality.


