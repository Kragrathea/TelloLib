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

namespace aTello
{
    [Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize, Label = "aTello",
    MainLauncher = true, Icon = "@mipmap/icon", Theme = "@android:style/Theme.NoTitleBar.Fullscreen", ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : Activity, InputManager.IInputDeviceListener
    {
    
        //joystick stuff
        public int[] buttons = new int[ButtonMapping.size];
        public float[] axes = new float[AxesMapping.size];
        private InputManager input_manager;
        private List<int> connected_devices = new List<int>();
        private int current_device_id = -1;

        Button takeoffButton;
        string videoFilePath;//file to save raw h264 to. 

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);

            takeoffButton = FindViewById<Button>(Resource.Id.takeoffButton);

            var path = "aTello/video/";
            System.IO.Directory.CreateDirectory(Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path));
            videoFilePath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".h264");

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
                        //Not connected to network.
                        RunOnUiThread(() => {
                            cbutton.Text = "Not Connected. Touch Here.";
                            cbutton.SetBackgroundColor(Android.Graphics.Color.DarkSalmon);
                        });
                        return;
                    }
                }
                if (newState == Tello.ConnectionState.Connected)
                {
                    //Override max hei on connect.
                    Tello.setMaxHeight(30);//meters

                    //Set new video file name based on date. 
                    //var path = "aTello/video/";
                    //System.IO.Directory.CreateDirectory(Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path));
                    //videoFilePath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".h264");
                }
                //update connection state button.
                RunOnUiThread(() => {
                    cbutton.Text = newState.ToString();
                    if (newState == Tello.ConnectionState.Connected)
                        cbutton.SetBackgroundColor(Android.Graphics.Color.LightGreen);
                    else
                        cbutton.SetBackgroundColor(Android.Graphics.Color.Yellow);

                });


            };
            //subscribe to Tello update events
            Tello.onUpdate += (Tello.FlyData newState) =>
            {
                var str = newState.ToString();//ToString() = Formated state

                RunOnUiThread(() => {
                    //Update state on screen
                    var acstat = FindViewById<TextView>(Resource.Id.ac_state);

                    acstat.Text = str;
                    if (Tello.state.flying && takeoffButton.Text != "Land")
                        takeoffButton.Text = "Land";
                    else if (!Tello.state.flying && takeoffButton.Text != "Takeoff")
                        takeoffButton.Text = "Takeoff";
                });

            };

            var videoFrame = new byte[100 * 1024];
            var videoOffset = 0;

            //subscribe to Tello video data
            Tello.onVideoData += (byte[] data) =>
            {

                if (videoFilePath != null)
                {
                    //Save raw data minus sequence.
                    using (var stream = new FileStream(videoFilePath, FileMode.Append))
                    {
                        stream.Write(data, 2, data.Length-2);//Note remove 2 byte seq when saving. 
                    }
                }
                if (false)//video decoder tests.
                {
                    if (data[2] == 0 && data[3] == 0 && data[4] == 0 && data[5] == 1)//if nal
                    {
                        var nalType = data[6] & 0x1f;
                        if (nalType == 7|| nalType == 8)
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
                    Array.Copy(data, 2, videoFrame, videoOffset, data.Length - 2);
                    videoOffset += (data.Length - 2);
                }
            };

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

            //Settings button
            Button settingsButton = FindViewById<Button>(Resource.Id.button1);
            settingsButton.Click += delegate {
                StartActivity(typeof(SettingsActivity));
            };

            //Init joysticks.
            input_manager = (InputManager)GetSystemService(Context.InputService);
            CheckGameControllers();
        }
        //Handle joystick axis events.
        public override bool OnGenericMotionEvent(MotionEvent e)
        {
            InputDevice device = e.Device;
            if (device != null && device.Id == current_device_id)
            {
                if (IsGamepad(device))
                {
                    for (int i = 0; i < AxesMapping.size; i++)
                    {
                        axes[i] = GetCenteredAxis(e, device, AxesMapping.OrdinalValueAxis(i));
                    }
                    axes[4] = buttons[5];
                    Tello.setAxis(axes);
                    TextView joystat = FindViewById<TextView>(Resource.Id.joystick_state);
                    
                    var dataStr = string.Join(" ", buttons);
                    joystat.Text = string.Format(dataStr+"\nJOY {0: 0.00;-0.00} {1: 0.00;-0.00} {2: 0.00;-0.00} {3: 0.00;-0.00} {4: 0.00;-0.00}", axes[0], axes[1], axes[2], axes[3], axes[4]);

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
                int index = ButtonMapping.OrdinalValue(keyCode);
                if (index >= 0)
                {
                    buttons[index] = 0;
                    if (index == 7)
                        Tello.takeOff();
                    if (index == 6)
                        Tello.land();
                    axes[4] = buttons[5];
                    Tello.setAxis(axes);

                    //controller_view.Invalidate();
                }
                return true;
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
                    int index = ButtonMapping.OrdinalValue(keyCode);
                    if (index >= 0)
                    {
                        buttons[index] = 1;
                        //controller_view.Invalidate();
                        axes[4] = buttons[5];
                        Tello.setAxis(axes);
                    }
                    return true;
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
        }

        protected override void OnPause()
        {
            base.OnPause();
            input_manager.UnregisterInputDeviceListener(this);
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

        }

        public void OnInputDeviceChanged(int deviceId)
        {
            //Log.Debug(TAG, "OnInputDeviceChanged: " + deviceId);
            //controller_view.Invalidate();
        }


    }
}

