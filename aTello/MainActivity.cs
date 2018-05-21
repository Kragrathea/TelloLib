using Android.App;
using Android.Widget;
using Android.OS;
using Android.Hardware.Input;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using static aTello.GameController;
using Android.Content.PM;
using System;
using Android.Net.Wifi;
using Android.Text.Format;
using System.IO;
using System.Linq;
using TelloLib;
using Plugin.TextToSpeech;
using Android.Preferences;
using System.Threading.Tasks;
using System.Threading;
using Plugin.FilePicker.Abstractions;
using Plugin.FilePicker;

namespace aTello
{
    [Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize, Label = "aTello",
    MainLauncher = true, Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen", ScreenOrientation = ScreenOrientation.SensorLandscape)]
    public class MainActivity : Activity, InputManager.IInputDeviceListener
    {
        //joystick stuff
        private InputManager input_manager;
        private List<int> connected_devices = new List<int>();
        private int current_device_id = -1;

        JoystickView onScreenJoyL;
        JoystickView onScreenJoyR;

        ImageButton takeoffButton;
        ImageButton throwTakeoffButton;
        string videoFilePath;//file to save raw h264 to. 

        private int picMode = 0;
        Plugin.SimpleAudioPlayer.ISimpleAudioPlayer cameraShutterSound = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.Current;
        private bool toggleRecording = false;
        private bool isRecording = false;
        private DateTime startRecordingTime;

        private bool doStateLogging = false;

        private void startUIUpdate()
        { }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            //force max brightness on screen.
            Window.Attributes.ScreenBrightness = 1f;

            onScreenJoyL = FindViewById<JoystickView>(Resource.Id.joystickViewL);
            onScreenJoyR = FindViewById<JoystickView>(Resource.Id.joystickViewR);

            takeoffButton = FindViewById<ImageButton>(Resource.Id.takeoffButton);
            throwTakeoffButton = FindViewById<ImageButton>(Resource.Id.throwTakeoffButton);
           
            //subscribe to Tello connection events
            Tello.onConnection += (Tello.ConnectionState newState) =>
            {
                //Update state on screen
                Button cbutton = FindViewById<Button>(Resource.Id.connectButton);

                //If not connected check to see if connected to tello network.
                if (newState != Tello.ConnectionState.Connected)
                {
                    WifiManager wifiManager = (WifiManager)Application.Context.GetSystemService(Context.WifiService);
                    string ip = Formatter.FormatIpAddress(wifiManager.ConnectionInfo.IpAddress);
                    if (!ip.StartsWith("192.168.10."))
                    {
                        //CrossTextToSpeech.Current.Speak("No network found.");
                        //Not connected to network.
                        RunOnUiThread(() => {
                            cbutton.Text = "Not Connected. Touch Here.";
                            cbutton.SetBackgroundColor(Android.Graphics.Color.ParseColor("#55ff3333"));
                        });
                        return;
                    }
                }
                if (newState == Tello.ConnectionState.Connected)
                {
                    //Tello.queryMaxHeight();
                    //Override max hei on connect.
                    Tello.setMaxHeight(30);//meters
                    Tello.queryMaxHeight();

                    //Tello.queryAttAngle();
                    Tello.setAttAngle(25);
                    //Tello.queryAttAngle();

                    Tello.setJpgQuality(Preferences.jpgQuality);

                    CrossTextToSpeech.Current.Speak("Connected");
                    
                    Tello.setPicVidMode(picMode);//0=picture(960x720)

                    Tello.setEV(Preferences.exposure);

                }
                if (newState == Tello.ConnectionState.Disconnected)
                {
                    //if was connected then warn.
                    if(Tello.connectionState== Tello.ConnectionState.Connected)
                        CrossTextToSpeech.Current.Speak("Disconnected");
                }
                //update connection state button.
                RunOnUiThread(() => {
                    cbutton.Text = newState.ToString();
                    if (newState == Tello.ConnectionState.Connected)
                        cbutton.SetBackgroundColor(Android.Graphics.Color.ParseColor("#6090ee90"));//transparent light green.
                    else
                        cbutton.SetBackgroundColor(Android.Graphics.Color.ParseColor("#ffff00"));//yellow
                });


            };
            var modeTextView = FindViewById<TextView>(Resource.Id.modeTextView);
            var hSpeedTextView =FindViewById<TextView>(Resource.Id.hSpeedTextView);
            var vSpeedTextView = FindViewById<TextView>(Resource.Id.vSpeedTextView);
            var heiTextView = FindViewById<TextView>(Resource.Id.heiTextView);
            var batTextView = FindViewById<TextView>(Resource.Id.batTextView);
            var wifiTextView = FindViewById<TextView>(Resource.Id.wifiTextView);

            //Log file setup.
            var logPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, "aTello/logs/"); ;
            var logStartTime = DateTime.Now;
            var logFilePath = logPath + logStartTime.ToString("yyyy-dd-M--HH-mm-ss") + ".csv";

            if (doStateLogging)
            {
                //write header for cols in log.
                System.IO.Directory.CreateDirectory(logPath);
                File.WriteAllText(logFilePath, "time," + Tello.state.getLogHeader());
            }

            //subscribe to Tello update events
            Tello.onUpdate += (Tello.FlyData newState) =>
            {
                if (doStateLogging)
                {
                    //write update to log.
                    var elapsed = DateTime.Now - logStartTime;
                    File.AppendAllText(logFilePath, elapsed.ToString(@"mm\:ss\:ff\,") + newState.getLogLine());
                }

                RunOnUiThread(() => {
                    //Update state on screen

                    modeTextView.Text = "FM:" + newState.flyMode;
                    hSpeedTextView.Text = string.Format("HS:{0: 0.0;-0.0}m/s", (float)newState.flySpeed / 10);
                    vSpeedTextView.Text = string.Format("VS:{0: 0.0;-0.0}m/s", (float)newState.verticalSpeed / 10);
                    heiTextView.Text = string.Format("Hei:{0: 0.0;-0.0}m", (float)newState.height / 10);

                    if (Tello.controllerState.speed > 0)
                        hSpeedTextView.SetBackgroundColor(Android.Graphics.Color.IndianRed);
                    else
                        hSpeedTextView.SetBackgroundColor(Android.Graphics.Color.Transparent);

                    batTextView.Text = "Bat:" + newState.batteryPercentage;
                    wifiTextView.Text = "Wifi:" + newState.wifiStrength;

                    //acstat.Text = str;
                    if (Tello.state.flying)
                        takeoffButton.SetImageResource(Resource.Drawable.land);
                    else if (!Tello.state.flying)
                        takeoffButton.SetImageResource(Resource.Drawable.takeoff_white);
                });

            };

            var videoFrame = new byte[100 * 1024];
            var videoOffset = 0;
            Video.Decoder.surface = FindViewById<SurfaceView>(Resource.Id.surfaceView).Holder.Surface;

            var path = "aTello/video/";
            System.IO.Directory.CreateDirectory(Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path+"cache/"));
            videoFilePath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path +"cache/"+ DateTime.Now.ToString("MMMM dd yyyy HH-mm-ss") + ".h264");

            FileStream videoStream = null;

            startUIUpdateThread();
            //updateUI();//hide record light etc. 

            //subscribe to Tello video data
            Tello.onVideoData += (byte[] data) =>
            {
                //Handle recording.
                if (true)//videoFilePath != null)
                {
                    if (data[2] == 0 && data[3] == 0 && data[4] == 0 && data[5] == 1)//if nal
                    {
                        var nalType = data[6] & 0x1f;
                        //                       if (nalType == 7 || nalType == 8)
                        {
                            if (toggleRecording)
                            {
                                if (videoStream != null)
                                    videoStream.Close();
                                videoStream = null;

                                isRecording = !isRecording;
                                toggleRecording = false;
                                if (isRecording)
                                {
                                    videoFilePath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path + DateTime.Now.ToString("MMMM dd yyyy HH-mm-ss") + ".h264");
                                    startRecordingTime = DateTime.Now;
                                    CrossTextToSpeech.Current.Speak("Recording");
                                    updateUI();
                                }
                                else
                                {
                                    videoFilePath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path + "cache/" + DateTime.Now.ToString("MMMM dd yyyy HH-mm-ss") + ".h264");
                                    CrossTextToSpeech.Current.Speak("Recording stopped");
                                    updateUI();
                                }
                            }
                        }
                    }

                    if ((isRecording || Preferences.cacheVideo))
                    {
                        if (videoStream == null)
                            videoStream = new FileStream(videoFilePath, FileMode.Append);

                        if (videoStream != null)
                        {
                            //Save raw data minus sequence.
                            videoStream.Write(data, 2, data.Length - 2);//Note remove 2 byte seq when saving. 
                        }
                    }
                }

                //Handle video display.
                if (true)//video decoder tests.
                {
                    //Console.WriteLine("1");

                    if (data[2] == 0 && data[3] == 0 && data[4] == 0 && data[5] == 1)//if nal
                    {
                        var nalType = data[6] & 0x1f;
                        if (nalType == 7 || nalType == 8)
                        {

                        }
                        if (videoOffset > 0)
                        {
                            aTello.Video.Decoder.decode(videoFrame.Take(videoOffset).ToArray());
                            videoOffset = 0;
                        }
                        //var nal = (received.bytes[6] & 0x1f);
                        //if (nal != 0x01 && nal != 0x07 && nal != 0x08 && nal != 0x05)
                        //    Console.WriteLine("NAL type:" + nal);
                    }
                    //todo. resquence frames.
                    Array.Copy(data, 2, videoFrame, videoOffset, data.Length - 2);
                    videoOffset += (data.Length - 2);
                }
            };

            onScreenJoyL.onUpdate += OnTouchJoystickMoved;
            onScreenJoyR.onUpdate += OnTouchJoystickMoved;



            Tello.startConnecting();//Start trying to connect.

            //Clicking on network state button will show wifi connection page. 
            Button button = FindViewById<Button>(Resource.Id.connectButton);
            button.Click += delegate {
                WifiManager wifiManager = (WifiManager)Application.Context.GetSystemService(Context.WifiService);
                string ip = Formatter.FormatIpAddress(wifiManager.ConnectionInfo.IpAddress);
                if(!ip.StartsWith("192.168.10."))//Already connected to network?
                    StartActivity(new Intent(Android.Net.Wifi.WifiManager.ActionPickWifiNetwork));

            };

            
            takeoffButton.Click += delegate {
                if (Tello.connected && !Tello.state.flying)
                {
                    Tello.takeOff();
                }
                else if (Tello.connected && Tello.state.flying)
                {
                    Tello.land();
                }
            };
            throwTakeoffButton.Click += delegate {
                if (Tello.connected && !Tello.state.flying)
                {
                    Tello.throwTakeOff();
                }
                else if (Tello.connected && Tello.state.flying)
                {
                    //Tello.land();
                }
            };
            var pictureButton = FindViewById<ImageButton>(Resource.Id.pictureButton);
            Tello.picPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, "aTello/pics/");
            System.IO.Directory.CreateDirectory(Tello.picPath);



            cameraShutterSound.Load("cameraShutterClick.mp3");
            pictureButton.Click += delegate
            {
                Tello.takePicture();
                cameraShutterSound.Play();
            };
            pictureButton.LongClick += delegate
            {
                //Toggle
                picMode= picMode == 1?0:1;
                Tello.setPicVidMode(picMode);
                aTello.Video.Decoder.reconfig();
            };

            var galleryButton = FindViewById<ImageButton>(Resource.Id.galleryButton);
            galleryButton.Click += async delegate
            {


                //var uri = Android.Net.Uri.FromFile(new Java.IO.File(Tello.picPath));
                //shareImage(uri);
                //return;
                Intent intent = new Intent();
                intent.PutExtra(Intent.ActionView, Tello.picPath);
                intent.SetType("image/*");
                intent.SetAction(Intent.ActionGetContent);
                StartActivityForResult(Intent.CreateChooser(intent,"Select Picture"), 1);
            };
            //Settings button
            ImageButton settingsButton = FindViewById<ImageButton>(Resource.Id.settingsButton);
            settingsButton.Click += delegate
            {
                StartActivity(typeof(SettingsActivity));
            };


            //Init joysticks.
            input_manager = (InputManager)GetSystemService(Context.InputService);
            CheckGameControllers();
        }

        private void startUIUpdateThread()
        {
            Task.Factory.StartNew(async () =>
            {
                var recLight = FindViewById<RadioButton>(Resource.Id.recLightButton);
                int tick = 0;
                while (true)
                {
                    try
                    {
                        RunOnUiThread(() =>
                        {
                            if (isRecording)
                            {
                                recLight.Visibility = ViewStates.Visible;
                                recLight.Text = "REC " + (DateTime.Now - startRecordingTime).ToString(@"mm\:ss");
                            }
                            else
                                recLight.Visibility = ViewStates.Gone;
                        });
                        Thread.Sleep(250);//Often enough?
                    }
                    catch (Exception ex)
                    {
                    }
                }
            });
        }

        private void updateUI()
        {
            var recLight = FindViewById<RadioButton>(Resource.Id.recLightButton);
            RunOnUiThread(() =>
            {
                if(isRecording)
                    recLight.Visibility = ViewStates.Visible;
                else
                    recLight.Visibility = ViewStates.Gone;
            });
        }
        // Share image
        private void shareImage(Android.Net.Uri imagePath)
        {
            Intent sharingIntent = new Intent(Intent.ActionSend);
            sharingIntent.AddFlags(ActivityFlags.ClearWhenTaskReset);
            sharingIntent.SetType("image/*");
            sharingIntent.PutExtra(Intent.ExtraStream, imagePath);
            StartActivity(Intent.CreateChooser(sharingIntent, "Share Image Using"));
        }

        public void OnTouchJoystickMoved(JoystickView joystickView )
        {
            Tello.controllerState.setAxis(onScreenJoyL.normalizedX, -onScreenJoyL.normalizedY, onScreenJoyR.normalizedX, -onScreenJoyR.normalizedY );
            Tello.sendControllerUpdate();
        }
        public float hatAxisX, hatAxisY;
        //Handle joystick axis events.
        private DateTime lastFlip;
        public override bool OnGenericMotionEvent(MotionEvent e)
        {
            InputDevice device = e.Device;
            if (device != null && device.Id == current_device_id)
            {
                if (IsGamepad(device))
                {
                    var lx = GetCenteredAxis(e, device, AxesMapping.OrdinalValueAxis(Preferences.lxAxis));//axes[0];
                    var ly = -GetCenteredAxis(e, device, AxesMapping.OrdinalValueAxis(Preferences.lyAxis));//-axes[1];
                    var rx = GetCenteredAxis(e, device, AxesMapping.OrdinalValueAxis(Preferences.rxAxis));// axes[2];
                    var ry = -GetCenteredAxis(e, device, AxesMapping.OrdinalValueAxis(Preferences.ryAxis));//-axes[3];


                    Tello.controllerState.setAxis(lx, ly, rx, ry);
                    Tello.sendControllerUpdate();

                    updateOnScreenJoyVisibility();

                    hatAxisX = GetCenteredAxis(e, device, AxesMapping.AXIS_HAT_X);
                    hatAxisY = GetCenteredAxis(e, device, AxesMapping.AXIS_HAT_Y);

                    //do flips only in speed mode.
                    if (Tello.controllerState.speed > 0)
                    {
                        if (hatAxisY > 0.9f && (DateTime.Now - lastFlip).TotalMilliseconds > 600)
                        {
                            lastFlip = DateTime.Now;
                            Tello.doFlip(2);
                        }
                        if (hatAxisY < -0.9f && (DateTime.Now - lastFlip).TotalMilliseconds > 600)
                        {
                            lastFlip = DateTime.Now;
                            Tello.doFlip(0);
                        }
                        if (hatAxisX > 0.9f && (DateTime.Now - lastFlip).TotalMilliseconds > 600)
                        {
                            lastFlip = DateTime.Now;
                            Tello.doFlip(3);
                        }
                        if (hatAxisX < -0.9f && (DateTime.Now - lastFlip).TotalMilliseconds > 600)
                        {
                            lastFlip = DateTime.Now;
                            Tello.doFlip(1);
                        }
                    }


                    RunOnUiThread(() =>
                    {
                        TextView joystat = FindViewById<TextView>(Resource.Id.joystick_state);

                        //var dataStr = string.Join(" ", buttons);
                        joystat.Text = string.Format("JOY 0:{0: 0.00;-0.00} 1:{1: 0.00;-0.00} 2:{2: 0.00;-0.00} 3:{3: 0.00;-0.00} 4:{4: 0.00;-0.00} ",// BTN: " + dataStr,
                            GetCenteredAxis(e, device, AxesMapping.OrdinalValueAxis(0)),
                            GetCenteredAxis(e, device, AxesMapping.OrdinalValueAxis(1)),
                            GetCenteredAxis(e, device, AxesMapping.OrdinalValueAxis(2)),
                            GetCenteredAxis(e, device, AxesMapping.OrdinalValueAxis(3)),
                            GetCenteredAxis(e, device, AxesMapping.OrdinalValueAxis(4))
                            );
                    });
 

                    //controller_view.Invalidate();
                    return true;
                }
            }
            return base.OnGenericMotionEvent(e);
        }

        public override bool OnKeyUp(Keycode keyCode, KeyEvent e)
        {
            InputDevice device = e.Device;
            if (device != null && device.Id == current_device_id)
            {
                if (keyCode == Preferences.speedButtonCode)
                {
                    Tello.controllerState.setSpeedMode(0);
                    Tello.sendControllerUpdate();
                    return true;
                }

            }
            return base.OnKeyUp(keyCode, e);
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            InputDevice device = e.Device;
            if (device != null && device.Id == current_device_id)
            {
                if (IsGamepad(device))
                {
                    if (keyCode == Preferences.takeoffButtonCode && e.RepeatCount == 7)
                    {
                        if (Tello.connected && !Tello.state.flying)
                        {
                            Tello.takeOff();
                        }
                        else if (Tello.connected && Tello.state.flying)
                        {
                            Tello.land();
                        }
                        return true;
                    }
                    if (keyCode == Preferences.landButtonCode && e.RepeatCount == 7)
                    {
                        Tello.land();
                        return true;
                    }
                    if (keyCode == Preferences.pictureButtonCode && e.RepeatCount == 0)
                    {
                        Tello.takePicture();
                        cameraShutterSound.Play();
                        return true;
                    };
                    if (keyCode == Preferences.recButtonCode && e.RepeatCount == 0)
                    {
                        toggleRecording = true;
                        return true;
                    };
                    //controller_view.Invalidate();
                    if (keyCode == Preferences.speedButtonCode)
                    {
                        Tello.controllerState.setSpeedMode(1);
                        Tello.sendControllerUpdate();
                        return true;
                    }

                    //if joy button return handled. 
                    if(keyCode>= Keycode.ButtonA && keyCode <=Keycode.ButtonMode)
                        return true;
;
                }
            }
            return base.OnKeyDown(keyCode, e);
        }

        //Check for any connected game controllers
        private void CheckGameControllers()
        {
            int[] deviceIds = input_manager.GetInputDeviceIds();
            foreach (int deviceId in deviceIds)
            {
                Android.Views.InputDevice dev = InputDevice.GetDevice(deviceId);
                int sources = (int)dev.Sources;

                if (((sources & (int)InputSourceType.Gamepad) == (int)InputSourceType.Gamepad) ||
                    ((sources & (int)InputSourceType.Joystick) == (int)InputSourceType.Joystick))
                {
                    if (!connected_devices.Contains(deviceId))
                    {
                        connected_devices.Add(deviceId);
                        if (current_device_id == -1)
                        {
                            current_device_id = deviceId;
                        }
                    }
                }
            }
        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);
        }

        protected override void OnResume()
        {
            base.OnResume();
            input_manager.RegisterInputDeviceListener(this, null);
            updateOnScreenJoyVisibility();
        }

        protected override void OnPause()
        {
            base.OnPause();
            input_manager.UnregisterInputDeviceListener(this);
        }


        private void updateOnScreenJoyVisibility()
        {
            if (current_device_id > -1)
            {
                RunOnUiThread(() =>
                {
                    onScreenJoyL.Visibility = ViewStates.Invisible;
                    onScreenJoyR.Visibility = ViewStates.Invisible;
                });
            }
            else
            {
                RunOnUiThread(() =>
                {
                    onScreenJoyL.Visibility = ViewStates.Visible;
                    onScreenJoyR.Visibility = ViewStates.Visible;
                });
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            return base.OnTouchEvent(e);
        }


        //Get the centered position for the joystick axis
        private float GetCenteredAxis(MotionEvent e, InputDevice device, Axis axis)
        {
            InputDevice.MotionRange range = device.GetMotionRange(axis, e.Source);
            if (range != null)
            {
                float flat = range.Flat;
                float value = e.GetAxisValue(axis);
                if (System.Math.Abs(value) > flat)
                    return value;
            }

            return 0;

        }

        private bool IsGamepad(InputDevice device)
        {
            if ((device.Sources & InputSourceType.Gamepad) == InputSourceType.Gamepad ||
               (device.Sources & InputSourceType.ClassJoystick) == InputSourceType.Joystick)
            {
                return true;
            }
            return false;
        }

        public void OnInputDeviceAdded(int deviceId)
        {
            //Log.Debug(TAG, "OnInputDeviceAdded: " + deviceId);
            if (!connected_devices.Contains(deviceId))
            {
                connected_devices.Add(deviceId);
            }
            if (current_device_id == -1)
            {
                current_device_id = deviceId;
                InputDevice dev = InputDevice.GetDevice(current_device_id);
                if (dev != null)
                {
                    //controller_view.SetCurrentControllerNumber(dev.ControllerNumber);
                    //controller_view.Invalidate();
                }
            }
            updateOnScreenJoyVisibility();
        }

        public void OnInputDeviceRemoved(int deviceId)
        {
            //Log.Debug(TAG, "OnInputDeviceRemoved: ", deviceId);
            connected_devices.Remove(deviceId);
            if (current_device_id == deviceId)
                current_device_id = -1;

            if (connected_devices.Count == 0)
            {
                //controller_view.SetCurrentControllerNumber(-1);
                //controller_view.Invalidate();
            }
            else
            {
                current_device_id = connected_devices[0];
                InputDevice dev = InputDevice.GetDevice(current_device_id);
                if (dev != null)
                {
                    //controller_view.SetCurrentControllerNumber(dev.ControllerNumber);
                    //controller_view.Invalidate();
                }
            }
            updateOnScreenJoyVisibility();
        }

        public void OnInputDeviceChanged(int deviceId)
        {
            //Log.Debug(TAG, "OnInputDeviceChanged: " + deviceId);
            //controller_view.Invalidate();
            updateOnScreenJoyVisibility();
        }


    }
}

