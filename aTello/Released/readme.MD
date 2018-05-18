Be very careful when flying! Especially the first time. Make sure the controls are what you expect and not inverted.

Installing the APK.
https://www.wikihow.tech/Install-APK-Files-on-Android

Instructions

<img src="https://github.com/Kragrathea/TelloLib/blob/master/Media/ScreenDiagram1.jpg" width="900" >

1. Network status area. This bar will be RED if not connected to the Tello network. Tap to connect in that case.
2. Takeoff/Land button
3. Throw takeoff button
4. Take picture button. Hold to switch to aspect ratio to 16x9. (not fully working yet)
5. Show pictures. Not fully working.
6. Settings. Joystick config and Jpeg quality.
7. Joystick axis status (for debugging)
8. Aircraft status area.

Joystick is currently supported for a Generic Android controllers and PS3 style controllers. This is selected in the settings page. If your joystick doesn't work First check to make sure it works on other Android apps. If it does then post an issue with including your controller brand.

There are button mappings on the controllers for takeoff, land, take picture and speed mode. 

<img src="https://github.com/Kragrathea/TelloLib/blob/master/Media/PS3_diagram.png" width="600" >

**Start** (Hold) = Takeoff.  
**Select** (Hold) = Land.  
**R1** = Take Picture.  
**R2** = Speed mode.  

Pictures are stored in <storage>/aTello/pics. Video is stored in <storage>/aTello/video.

Currently there is no "Record" button for video because video is **always** being recorded. The video is saved as the raw H264 that is coming from the drone and must be converted to mp4 for playback. I plan on a built in conversion but right now you need to use an external ffmpeg app to convert. Like this:

ffmpeg -i <input.h264> -vcodec copy output.mp4
 
Ffmpeg is avail for all PCs and on Android in the play store.  
https://www.ffmpeg.org/  
https://play.google.com/store/apps/details?id=com.silentlexx.ffmpeggui&hl=en_US  

