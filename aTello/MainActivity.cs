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
using System.Drawing;

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

        private bool forceSpeedMode = false;

        ImageButton takeoffButton;
        ImageButton throwTakeoffButton;
        ImageButton rthButton;
        private int rthButtonClickCount = 0;
        ImageButton lookAtButton;
        private int lookAtButtonClickCount = 0;
        string videoFilePath;//file to save raw h264 to. 
        private long totalVideoBytesReceived = 0;//used to calc video bit rate display.

        private int picMode = 0;
        Plugin.SimpleAudioPlayer.ISimpleAudioPlayer cameraShutterSound = Plugin.SimpleAudioPlayer.CrossSimpleAudioPlayer.Current;
        private bool toggleRecording = false;
        private bool isRecording = false;
        private DateTime startRecordingTime;

        private bool doStateLogging = false;

        public bool isPaused = false;

        private static MainActivity _mainActivity;
        public static MainActivity getActivity()
        {
            return _mainActivity;
        }

        public override View OnCreateView(String name, Context context, Android.Util.IAttributeSet attrs)
        {
            var result = base.OnCreateView(name,context,attrs);

            return result;
        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            _mainActivity = this;

            //force max brightness on screen.
            Window.Attributes.ScreenBrightness = 1f;

            //Full screen and hide nav bar.
            View decorView = Window.DecorView;
            var uiOptions = (int)decorView.SystemUiVisibility;
            var newUiOptions = (int)uiOptions;
            newUiOptions |= (int)SystemUiFlags.LowProfile;
            newUiOptions |= (int)SystemUiFlags.Fullscreen;
            newUiOptions |= (int)SystemUiFlags.HideNavigation;
            newUiOptions |= (int)SystemUiFlags.Immersive;
            // This option will make bars disappear by themselves
            newUiOptions |= (int)SystemUiFlags.ImmersiveSticky;
            decorView.SystemUiVisibility = (StatusBarVisibility)newUiOptions;

            //Keep screen from dimming.
            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);


            onScreenJoyL = FindViewById<JoystickView>(Resource.Id.joystickViewL);
            onScreenJoyR = FindViewById<JoystickView>(Resource.Id.joystickViewR);

            takeoffButton = FindViewById<ImageButton>(Resource.Id.takeoffButton);
            throwTakeoffButton = FindViewById<ImageButton>(Resource.Id.throwTakeoffButton);

            rthButton = FindViewById<ImageButton>(Resource.Id.rthButton);
            lookAtButton = FindViewById<ImageButton>(Resource.Id.lookAtButton);

            //subscribe to Tello connection events
            Tello.onConnection += (Tello.ConnectionState newState) =>
            {
                //Update state on screen
                Button cbutton = FindViewById<Button>(Resource.Id.connectButton);

                //If not connected check to see if connected to tello network.
                if (newState != Tello.ConnectionState.Connected 
                    && newState != Tello.ConnectionState.Paused)
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
                if (newState == Tello.ConnectionState.Paused)
                {
                }
                if (newState == Tello.ConnectionState.UnPausing)
                {
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

                    notifyUser("Connected");

                    Tello.setPicVidMode(picMode);//0=picture(960x720)

                    Tello.setEV(Preferences.exposure);

                    Tello.setVideoBitRate(Preferences.videoBitRate);
                    Tello.setVideoDynRate(1);

                    if (forceSpeedMode)
                        Tello.controllerState.setSpeedMode(1);
                    else
                        Tello.controllerState.setSpeedMode(0);

                }
                if (newState == Tello.ConnectionState.Disconnected)
                {
                    //if was connected then warn.
                    if(Tello.connectionState== Tello.ConnectionState.Connected)
                        notifyUser("Disconnected");
                }
                //update connection state button.
                RunOnUiThread(() => {

                    if (newState == Tello.ConnectionState.UnPausing)//Fix. Don't show "unpausing" string.
                        cbutton.Text = Tello.ConnectionState.Connected.ToString();
                    else
                        cbutton.Text = newState.ToString();

                    if (newState == Tello.ConnectionState.Connected || newState == Tello.ConnectionState.UnPausing)
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

            //Long click vert speed to force fast mode. 
            hSpeedTextView.LongClick += delegate {
                forceSpeedMode = !forceSpeedMode;
                if (forceSpeedMode)
                    Tello.controllerState.setSpeedMode(1);
                else
                    Tello.controllerState.setSpeedMode(0);
            };
            
            cameraShutterSound.Load("cameraShutterClick.mp3");
            //subscribe to Tello update events
            Tello.onUpdate += (int cmdId) =>
            {
                if (doStateLogging)
                {
                    //write update to log.
                    var elapsed = DateTime.Now - logStartTime;
                    File.AppendAllText(logFilePath, elapsed.ToString(@"mm\:ss\:ff\,") + Tello.state.getLogLine());
                }

                RunOnUiThread(() => {
                    if (cmdId == 86)//ac status update. 
                    {
                        //Update state on screen
                        modeTextView.Text = "FM:" + Tello.state.flyMode;
                        hSpeedTextView.Text = string.Format("HS:{0: 0.0;-0.0}m/s", (float)Tello.state.flySpeed / 10);
                        vSpeedTextView.Text = string.Format("VS:{0: 0.0;-0.0}m/s", -(float)Tello.state.verticalSpeed / 10);//Note invert so negative means moving down. 
                        heiTextView.Text = string.Format("Hei:{0: 0.0;-0.0}m", (float)Tello.state.height / 10);

                        if (Tello.controllerState.speed > 0)
                            hSpeedTextView.SetBackgroundColor(Android.Graphics.Color.IndianRed);
                        else
                            hSpeedTextView.SetBackgroundColor(Android.Graphics.Color.DarkGreen);

                        batTextView.Text = "Bat:" + Tello.state.batteryPercentage;
                        wifiTextView.Text = "Wifi:" + Tello.state.wifiStrength;

                        /*if (bAutopilot)
                            rthButton.SetBackgroundColor(Android.Graphics.Color.DarkGreen);
                        else
                            rthButton.SetBackgroundColor(Android.Graphics.Color.White);
                            
                         */

                        //Autopilot debugging.
                        if (/*!bAutopilot &&*/ Tello.state.flying)
                        {
                            var eular = Tello.state.toEuler();
                            var yaw = eular[2];

                            var deltaPosX = autopilotTarget.X - Tello.state.posX;
                            var deltaPosY = autopilotTarget.Y - Tello.state.posY;
                            var dist = Math.Sqrt(deltaPosX * deltaPosX + deltaPosY * deltaPosY);
                            var normalizedX = deltaPosX / dist;
                            var normalizedY = deltaPosY / dist;

                            var ldeltaPosX = lookAtTarget.X - Tello.state.posX;
                            var ldeltaPosY = lookAtTarget.Y - Tello.state.posY;
                            var ldist = Math.Sqrt(ldeltaPosX * ldeltaPosX + ldeltaPosY * ldeltaPosY);
                            var lnormalizedX = ldeltaPosX / ldist;
                            var lnormalizedY = ldeltaPosY / ldist;

                            var targetYaw = Math.Atan2(lnormalizedY, lnormalizedX);

                            double deltaYaw = 0.0;
                            if (Math.Abs(targetYaw - yaw) < Math.PI)
                                deltaYaw= targetYaw - yaw;
                            else if (targetYaw > yaw)
                                deltaYaw = targetYaw - yaw - Math.PI * 2.0f;
                            else
                                deltaYaw = targetYaw - yaw + Math.PI * 2.0f;

                            var str = string.Format("x {0:0.00; -0.00} y {1:0.00; -0.00} yaw {2:0.00; -0.00} Unc:{3:0.00; -0.00} tDist {4:0.00; -0.00} On:{5} targetYaw {6:0.00; -0.00} ",
                                    Tello.state.posX, Tello.state.posY,
                                    (((yaw * (180.0 / Math.PI)) ) ),
//                                    (((yaw * (180.0 / Math.PI)) + 360.0) % 360.0),
                                    Tello.state.posUncertainty
                                    ,dist,
                                    bAutopilot.ToString()
                                    , (((deltaYaw * (180.0 / Math.PI)) ) )
//                                    , (((targetYaw * (180.0 / Math.PI)) + 360.0) % 360.0)
                                    );

                            TextView joystat = FindViewById<TextView>(Resource.Id.joystick_state);
                            joystat.Text = str;
                        }

                        if (!Tello.state.flying)//debug joystick
                        {
                            TextView joystat = FindViewById<TextView>(Resource.Id.joystick_state);

                            //var dataStr = string.Join(" ", buttons);
                            joystat.Text = string.Format("JOY lx:{0: 0.00;-0.00} ly:{1: 0.00;-0.00} rx:{2: 0.00;-0.00} ry:{3: 0.00;-0.00}  ",
                                Tello.controllerState.lx,
                                Tello.controllerState.ly,
                                Tello.controllerState.rx,
                                Tello.controllerState.ry);
                        }

                        //acstat.Text = str;
                        if (Tello.state.flying)
                            takeoffButton.SetImageResource(Resource.Drawable.land);
                        else if (!Tello.state.flying)
                            takeoffButton.SetImageResource(Resource.Drawable.takeoff);
                    }
                    if (cmdId == 48)//ack picture start. 
                    {
                        cameraShutterSound.Play();
                    }
                    if (cmdId == 98)//start picture download. 
                    {
                    }
                    if (cmdId == 100)//picture piece downloaded. 
                    {
                        if(Tello.picDownloading==false)//if done downloading.
                        {
                            if (remainingExposures >= 0)
                            {
                                var exposureSet = new int[]{0,-2,8};
                                Tello.setEV(Preferences.exposure + exposureSet[remainingExposures]);
                                remainingExposures--;
                                Tello.takePicture();
                            }
                            if(remainingExposures==-1)//restore exposure. 
                                Tello.setEV(Preferences.exposure);
                        }
                    }
                });

                //Do autopilot input.
                handleAutopilot();

            };


            var videoFrame = new byte[100 * 1024];
            var videoOffset = 0;

            var path = "aTello/video/";
            System.IO.Directory.CreateDirectory(Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path+"cache/"));
            videoFilePath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path +"cache/"+ DateTime.Now.ToString("MMMM dd yyyy HH-mm-ss") + ".h264");

            FileStream videoStream = null;

            startUIUpdateThread();

            //subscribe to Tello video data
            var vidCount = 0;
            Tello.onVideoData += (byte[] data) =>
            {
                totalVideoBytesReceived += data.Length;
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
                                    //                                    Tello.setVideoRecord(vidCount++);
                                    notifyUser("Recording");
                                    updateUI();
                                }
                                else
                                {
                                    videoFilePath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path + "cache/" + DateTime.Now.ToString("MMMM dd yyyy HH-mm-ss") + ".h264");
                                    notifyUser("Recording stopped");
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
                            if (!isPaused)//surfaces are lost when paused.
                            {
                                //aTello.Video.Decoder.decode(videoFrame.Take(videoOffset).ToArray());

                                //todo. get rid of buffer copy.
                                var decoderView = FindViewById<DecoderView>(Resource.Id.DecoderView);
                                decoderView.decode(videoFrame.Take(videoOffset).ToArray());
                            }
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

            rthButton.LongClick += delegate {
                if (bAutopilot)
                    cancelAutopilot();
                else if(bHomepointSet)
                {
                    bAutopilot = true;
                    notifyUser("Autopilot engaged");
                }
            };

            rthButton.Click += delegate {
                cancelAutopilot();//Stop if going.
                if (rthButtonClickCount == 0)
                {
                    notifyUser("Press again to set home point. Long press to fly to home.", false);
                }

                if (rthButtonClickCount == 1)
                {//force set of new home point. 
                    bHomepointSet = false;
                }
                rthButtonClickCount++;
                Handler h = new Handler();
                Action myAction = () =>
                {
                    rthButtonClickCount = 0;
                };

                h.PostDelayed(myAction, 750);//750=3/4 of a second
            };
            lookAtButton.LongClick += delegate {
                if (bLookAt)
                    cancelLookAt();
                else if (bLookAtTargetSet)
                {
                    bLookAt = true;
                    notifyUser("Look at engaged");
                }
            };

            lookAtButton.Click += delegate {
                cancelLookAt();//Stop if going.
                if (lookAtButtonClickCount == 0)
                {
                    notifyUser("Press again to set look target. Long press to lock on target.", false);
                }

                if (lookAtButtonClickCount == 1)
                {//force set of new home point. 
                    bLookAtTargetSet = false;
                }
                lookAtButtonClickCount++;
                Handler h = new Handler();
                Action myAction = () =>
                {
                    lookAtButtonClickCount = 0;
                };

                h.PostDelayed(myAction, 750);//750=3/4 of a second
            };
            takeoffButton.LongClick += delegate {
                if (Tello.connected && !Tello.state.flying)
                {
                    Tello.takeOff();
                }
                else if (Tello.connected && Tello.state.flying)
                {
                    Tello.land();
                }
            };
            throwTakeoffButton.LongClick += delegate {
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

            pictureButton.Click += delegate
            {
                remainingExposures = -1;
                Tello.takePicture();
            };
            /*
            * Multiple exposure. Not working yet.
            pictureButton.LongClick += delegate
            {
                remainingExposures = 2;
                Tello.takePicture();
            };
            */

            var recordButton = FindViewById<ImageButton>(Resource.Id.recordButton);
            recordButton.Click += delegate
            {
                toggleRecording = true;
            };

            recordButton.LongClick += delegate
            {
                //Toggle
                picMode = picMode == 1 ? 0 : 1;
                Tello.setPicVidMode(picMode);
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
                StartActivityForResult(Intent.CreateChooser(intent,"Select Picture"),1);
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

        public void notifyUser(string message,bool bSpeak=true)
        {
            if(bSpeak)
                CrossTextToSpeech.Current.Speak(message);

            RunOnUiThread(async () =>
            {
                Toast.MakeText(Application.Context, message, ToastLength.Long).Show();
            });
        }

        private bool bAutopilot=false;
        private PointF autopilotTarget=new PointF(0,0);
        //private PointF autopilotLookTarget = null;
        private bool bHomepointSet = false;

        private bool bLookAt = false;
        private PointF lookAtTarget = new PointF(0, 0);
        //private PointF autopilotLookTarget = null;
        private bool bLookAtTargetSet = false;


        public void setAutopilotTarget(PointF target)
        {
            if (Tello.state.flying)
            {
                autopilotTarget = target;
            }
        }
        public void cancelAutopilot()
        {
            if(bAutopilot)
                notifyUser("Autopilot disengaged");
            Tello.autoPilotControllerState.setAxis(0, 0, 0, 0);
            Tello.sendControllerUpdate();
            bAutopilot = false;

        }

        public void setLookAtTarget(PointF target)
        {
            if (Tello.state.flying)
            {
                lookAtTarget = target;
            }
        }
        public void cancelLookAt()
        {
            if (bLookAt)
                notifyUser("Look at disengaged");
            Tello.autoPilotControllerState.setAxis(0, 0, 0, 0);
            Tello.sendControllerUpdate();
            bLookAt = false;

        }
  
        private void handleAutopilot()
        {
            if(!Tello.state.flying)
            {
                bHomepointSet = false;
                bLookAtTargetSet = false;
                return;
            }

            if (!bHomepointSet)
            {
                if(Tello.state.posUncertainty>0.03)
                {
                    //set new home point
                    setAutopilotTarget(new PointF(Tello.state.posX, Tello.state.posY));
                    notifyUser("Homepoint set");
                    bHomepointSet = true;
                }
            }

            if (!bLookAtTargetSet)
            {
                if(Tello.state.posUncertainty>0.03)
                {
                    //set new home point
                    setLookAtTarget(new PointF(Tello.state.posX, Tello.state.posY));
                    notifyUser("Look at set");
                    bLookAtTargetSet = true;
                }
            }

            double lx = 0, ly = 0, rx = 0, ry = 0;
            bool updated = false;
            if (bLookAt && bLookAtTargetSet)
            {
                var eular = Tello.state.toEuler();
                var yaw = eular[2];

                var deltaPosX = lookAtTarget.X - Tello.state.posX;
                var deltaPosY = lookAtTarget.Y - Tello.state.posY;
                var dist = Math.Sqrt(deltaPosX * deltaPosX + deltaPosY * deltaPosY);
                var normalizedX = deltaPosX / dist;
                var normalizedY = deltaPosY / dist;

                var targetYaw = Math.Atan2(normalizedY, normalizedX);

                double deltaYaw = 0.0;
                if (Math.Abs(targetYaw - yaw) < Math.PI)
                    deltaYaw = targetYaw - yaw;
                else if (targetYaw > yaw)
                    deltaYaw = targetYaw - yaw - Math.PI * 2.0f;
                else
                    deltaYaw = targetYaw - yaw + Math.PI * 2.0f;


                var minYaw = 0.1;//Radians
                if (Math.Abs(deltaYaw) > minYaw)
                {
                    lx = Math.Min(1.0, deltaYaw * 1.0);
                    updated = true;
                }
                else if (deltaYaw < -minYaw)
                {
                    lx = -Math.Min(1.0, deltaYaw * 1.0);
                    updated = true;
                }
            }
            if (bAutopilot && bHomepointSet)
            {
                var eular = Tello.state.toEuler();
                var yaw = eular[2];

                var deltaPosX = autopilotTarget.X - Tello.state.posX;
                var deltaPosY = autopilotTarget.Y - Tello.state.posY;
                var dist = Math.Sqrt(deltaPosX * deltaPosX + deltaPosY * deltaPosY);
                var normalizedX = deltaPosX / dist;
                var normalizedY = deltaPosY / dist;

                var targetYaw = Math.Atan2(normalizedY, normalizedX);
                var deltaYaw = targetYaw - yaw;

                var minDist = 0.25;//Meters (I think)

                if (dist > minDist)
                {
                    var speed = Math.Min(0.45, dist*2);//0.2 limits max throttle for safety.
                    rx = speed * Math.Sin(deltaYaw);
                    ry = speed * Math.Cos(deltaYaw);
                    updated = true;
                }
                else
                {
                    cancelAutopilot();//arrived
                    updated = true;
                }
            }
            if (updated)
            {
                Tello.autoPilotControllerState.setAxis((float)lx, (float)ly, (float)rx, (float)ry);
                Tello.sendControllerUpdate();
            }
        }

        private void startUIUpdateThread()
        {
            Task.Factory.StartNew(async () =>
            {
                var recLight = FindViewById<RadioButton>(Resource.Id.recLightButton);
                var throwButton = FindViewById<ImageButton>(Resource.Id.throwTakeoffButton);
                var galleryButton = FindViewById<ImageButton>(Resource.Id.galleryButton);
                var vbrTextView = FindViewById<TextView>(Resource.Id.vbrTextView);
                int tick = 0;
                long videoBytesReceivedLastSecond = 0;
                while (true)
                {
                    try
                    {
                        var bFlying = Tello.state.flying;
                        RunOnUiThread(() =>
                        {
                            if (isRecording)
                            {
                                recLight.Visibility = ViewStates.Visible;
                                recLight.Text = "REC " + (DateTime.Now - startRecordingTime).ToString(@"mm\:ss");
                            }
                            else
                                recLight.Visibility = ViewStates.Gone;

                            if (bFlying)
                            {
                                throwButton.Visibility = ViewStates.Gone;
                                //galleryButton.Visibility = ViewStates.Gone;
                            }
                            else
                            {
                                throwButton.Visibility = ViewStates.Visible;
                                //galleryButton.Visibility = ViewStates.Visible;
                            }
                            if((tick%4)==0)//Every second.
                            {
                                if (totalVideoBytesReceived > 0 && videoBytesReceivedLastSecond > 0)
                                {
                                    var perSec = totalVideoBytesReceived - videoBytesReceivedLastSecond;
                                    vbrTextView.Text =string.Format("Vbr:{0}k i:{1}",(perSec / 1024),Tello.iFrameRate);
                                }
                                videoBytesReceivedLastSecond = totalVideoBytesReceived;

                                updateOnScreenJoyVisibility();
                            }
                        });
                        Thread.Sleep(250);//Often enough?
                    }
                    catch (Exception ex)
                    {
                    }
                    tick++;
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


        public void OnTouchJoystickMoved(JoystickView joystickView )
        {
            //Right stick movement cancels autopilot.
            if (bAutopilot && (Math.Abs(onScreenJoyR.normalizedX) > 0.1 || Math.Abs(onScreenJoyR.normalizedY) > 0.1))
                cancelAutopilot();

            //Left stick movement cancels autopilot.
            if (bLookAt && (Math.Abs(onScreenJoyL.normalizedX) > 0.1 || Math.Abs(onScreenJoyL.normalizedY) > 0.1))
                cancelLookAt();

            if (isPaused)//Zero out any movement when paused.
                Tello.controllerState.setAxis(0, 0, 0, 0);
            else
            {
                var deadBand = 0.15f;
                var rx = Math.Abs(onScreenJoyR.normalizedX) < deadBand ? 0.0f : onScreenJoyR.normalizedX;
                var ry = Math.Abs(onScreenJoyR.normalizedY) < deadBand ? 0.0f : onScreenJoyR.normalizedY;
                var lx = Math.Abs(onScreenJoyL.normalizedX) < deadBand ? 0.0f : onScreenJoyL.normalizedX;
                var ly = Math.Abs(onScreenJoyL.normalizedY) < deadBand ? 0.0f : onScreenJoyL.normalizedY;

                Tello.controllerState.setAxis(lx, -ly, rx, -ry);
            }
            Tello.sendControllerUpdate();
        }
        public float hatAxisX, hatAxisY;
        //Handle joystick axis events.
        private DateTime lastFlip;
        private int remainingExposures;

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

                    //Right stick movement cancels autopilot.
                    if (bAutopilot && (Math.Abs(rx) > 0.1 || Math.Abs(ry) > 0.1))
                        cancelAutopilot();

                    //Left stick movement cancels autopilot.
                    if (bLookAt && (Math.Abs(lx) > 0.1 || Math.Abs(ly) > 0.1))
                        cancelLookAt();

                    var deadBand = 0.15f;
                    rx = Math.Abs(rx) < deadBand ? 0.0f : rx;
                    ry = Math.Abs(ry) < deadBand ? 0.0f : ry;
                    lx = Math.Abs(lx) < deadBand ? 0.0f : lx;
                    ly = Math.Abs(ly) < deadBand ? 0.0f : ly;

                    if (isPaused)//Zero out any movement when paused.
                        Tello.controllerState.setAxis(0, 0, 0, 0);
                    else
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
                    if(forceSpeedMode)
                        Tello.controllerState.setSpeedMode(1);
                    else
                        Tello.controllerState.setSpeedMode(0);
                    Tello.sendControllerUpdate();
                    return true;
                }
                if (keyCode == Preferences.homeButtonCode)
                {
                    if (rthPressCount < 7)
                    {
                        cancelAutopilot();
                        if (rthDoublePress)
                        {
                            bHomepointSet = false;
                            rthDoublePress = false;
                        }
                        else
                        {
                            rthDoublePress = true;
                            //notifyUser("Press again to set home point. Long press to fly to home.", false);
                            Handler h = new Handler();
                            Action myAction = () =>
                            {
                                rthDoublePress = false;
                            };

                            h.PostDelayed(myAction, 750);
                        }
                    }
                    return true;
                }

            }
            return base.OnKeyUp(keyCode, e);
        }

        private int rthPressCount = 0;
        private bool rthDoublePress = false;
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
                    if (keyCode == Preferences.homeButtonCode)
                    {
                        rthPressCount = e.RepeatCount;
                        if (e.RepeatCount == 7)
                        {
                            if (bAutopilot)
                                cancelAutopilot();
                            else if (bHomepointSet)
                            {
                                bAutopilot = true;
                                notifyUser("Autopilot engaged");
                            }
                        }
                        return true;
                    }
                    if (keyCode == Preferences.pictureButtonCode && e.RepeatCount == 0)
                    {
                        Tello.takePicture();
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

            isPaused = false;
            input_manager.RegisterInputDeviceListener(this, null);
            updateOnScreenJoyVisibility();
            //fix if joy was moved when paused.
            onScreenJoyL.returnHandleToCenter();
            onScreenJoyR.returnHandleToCenter();

            Tello.connectionSetPause(false);//reanable connections if paused. 
        }

        protected override void OnPause()
        {
            //fix if joy was moved when paused.
            onScreenJoyL.returnHandleToCenter();
            onScreenJoyR.returnHandleToCenter();

            //Zero out Joy input so we don't keep flying.
            Tello.controllerState.setAxis(0, 0, 0, 0);
            Tello.sendControllerUpdate();

            cancelAutopilot();

            isPaused = true;

            Tello.connectionSetPause(true);//pause connections (if connected). 

            var decoderView = FindViewById<DecoderView>(Resource.Id.DecoderView);
            decoderView.stop();

            base.OnPause();
            input_manager.UnregisterInputDeviceListener(this);
        }

        bool doubleBackToExitPressedOnce = false;
        public override void OnBackPressed()
        {
            if (doubleBackToExitPressedOnce)
            {
                base.OnBackPressed();
                return;
            }

            this.doubleBackToExitPressedOnce = true;
            Toast.MakeText(this, "Click BACK again to exit", ToastLength.Short).Show();

            Handler h = new Handler();
            Action myAction = () =>
            {
                doubleBackToExitPressedOnce = false;
            };

            h.PostDelayed(myAction, 2000);
        }

        public void updateOnScreenJoyVisibility()
        {
            if (current_device_id > -1 && !Preferences.onScreenJoy)
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
            try
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
            }catch(Exception ex)
            {//trying to figure out why this might crash video decoder.
                notifyUser("Joystick exception OnInputDeviceAdded " + ex.Message, false);
            }
        }

        public void OnInputDeviceRemoved(int deviceId)
        {
            try
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
            catch (Exception ex)
            {//trying to figure out why this might crash video decoder.
                notifyUser("Joystick exception OnInputDeviceRemoved " + ex.Message, false);
            }

        }

        public void OnInputDeviceChanged(int deviceId)
        {
            try
            {
                //Log.Debug(TAG, "OnInputDeviceChanged: " + deviceId);
                //controller_view.Invalidate();
                updateOnScreenJoyVisibility();
            }
            catch (Exception ex)
            {//trying to figure out why this might crash video decoder.
                notifyUser("Joystick exception OnInputDeviceChanged " + ex.Message, false);
            }
        }


    }
}

