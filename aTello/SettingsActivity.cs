using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using TelloLib;
using static Android.Views.ViewGroup;

namespace aTello
{
    [Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize, Label = "Settings",
    Theme = "@android:style/Theme.DeviceDefault", ScreenOrientation = ScreenOrientation.SensorLandscape)]

    public class SettingsActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            this.RequestWindowFeature(WindowFeatures.NoTitle);
            this.Window.AddFlags(WindowManagerFlags.Fullscreen | WindowManagerFlags.TurnScreenOn);
            SetContentView(Resource.Layout.Settings);

            var items = new List<int>() { 0, 1, 2, 3, 4, 5, 6 };
            var axisAdapter = new ArrayAdapter<int>(this, Android.Resource.Layout.SimpleSpinnerItem, items);
            var spinner = FindViewById<Spinner>(Resource.Id.lxSpinner);
            spinner.Adapter = axisAdapter;
            spinner.SetSelection(Preferences.lxAxis);
            spinner.ItemSelected += (sender, args) =>
            {
                Preferences.lxAxis = args.Position;
                Preferences.save();
            };

            spinner = FindViewById<Spinner>(Resource.Id.lySpinner);
            spinner.Adapter = axisAdapter;
            spinner.SetSelection(Preferences.lyAxis);
            spinner.ItemSelected += (sender, args) =>
            {
                Preferences.lyAxis = args.Position;
                Preferences.save();
            };

            spinner = FindViewById<Spinner>(Resource.Id.rxSpinner);
            spinner.Adapter = axisAdapter;
            spinner.SetSelection(Preferences.rxAxis);
            spinner.ItemSelected += (sender, args) =>
            {
                Preferences.rxAxis = args.Position;
                Preferences.save();
            };

            spinner = FindViewById<Spinner>(Resource.Id.rySpinner);
            spinner.Adapter = axisAdapter;
            spinner.SetSelection(Preferences.ryAxis);
            spinner.ItemSelected += (sender, args) =>
            {
                Preferences.ryAxis = args.Position;
                Preferences.save();
            };


            var joyItems = new List<string>() { "Generic", "PS3/PS4" };
            var joyAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, joyItems);
            var joyTypeSpinner = FindViewById<Spinner>(Resource.Id.joystickTypeSpinner);
            joyTypeSpinner.Adapter = joyAdapter;
            joyTypeSpinner.SetSelection(Preferences.joyType);
            joyTypeSpinner.ItemSelected += (sender, args) =>
            {
                Preferences.setJoyType(args.Position);
                Preferences.save();
            };

            var onScreenJoySwitch = FindViewById<Switch>(Resource.Id.onScreenJoySwitch);
            onScreenJoySwitch.Checked = Preferences.onScreenJoy;
            onScreenJoySwitch.CheckedChange += (sender, args) =>
            {
                Preferences.onScreenJoy = args.IsChecked;
                Preferences.save();
            };
            

            var evItems = new List<double>() { -3.0, -2.7, -2.3, -2.0, -1.7, -1.3, -1.0, -0.7, -0.3, 0, 0.3, 0.7, 1.0, 1.3, 1.7, 2.0, 2.3, 2.7, 3.0 };
            var evAdapter = new ArrayAdapter<double>(this, Android.Resource.Layout.SimpleSpinnerItem, evItems);
            var evSpinner = FindViewById<Spinner>(Resource.Id.exposureSpinner);
            evSpinner.Adapter = evAdapter;
            evSpinner.SetSelection(Preferences.exposure);
            evSpinner.ItemSelected += (sender, args) =>
            {
                Preferences.exposure = args.Position;
                Tello.setEV(Preferences.exposure);

                Preferences.save();
            };

            var vbrItems = new List<string>() { "Auto", "1M", "1.5M", "2M", "3M", "4M"};
            var vbrAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, vbrItems);
            var vbrSpinner = FindViewById<Spinner>(Resource.Id.vbrSpinner);
            vbrSpinner.Adapter = vbrAdapter;
            vbrSpinner.SetSelection(Preferences.videoBitRate);
            vbrSpinner.ItemSelected += (sender, args) =>
            {
                Preferences.videoBitRate = args.Position;
                Tello.setVideoBitRate(Preferences.videoBitRate);

                Preferences.save();
            };
            //2,5,10,20,40
            var iframeRateItems = new List<string>() { "10/s", "4/s", "2/s", "1/s", "0.5/s" };
            var iframeRateAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, iframeRateItems);
            var iframeRateSpinner = FindViewById<Spinner>(Resource.Id.iframeRateSpinner);
            iframeRateSpinner.Adapter = iframeRateAdapter;
            switch (Preferences.iFrameRate)
            {
                case 2:
                    iframeRateSpinner.SetSelection(0);
                    break;
                case 5:
                    iframeRateSpinner.SetSelection(1);
                    break;
                case 10:
                    iframeRateSpinner.SetSelection(2);
                    break;
                case 20:
                    iframeRateSpinner.SetSelection(3);
                    break;
                case 40:
                    iframeRateSpinner.SetSelection(4);
                    break;
            }
            iframeRateSpinner.ItemSelected += (sender, args) =>
            {
                switch(args.Position)
                {
                    case 0:
                        Preferences.iFrameRate = 2;
                        break;
                    case 1:
                        Preferences.iFrameRate = 5;
                        break;
                    case 2:
                        Preferences.iFrameRate = 10;
                        break;
                    case 3:
                        Preferences.iFrameRate = 20;
                        break;
                    case 4:
                        Preferences.iFrameRate = 40;
                        break;
                }
                Tello.iFrameRate = Preferences.iFrameRate;
                Preferences.save();
            };
            

            var cacheVideoSwitch = FindViewById<Switch>(Resource.Id.cacheVideoSwitch);
            cacheVideoSwitch.Checked = Preferences.cacheVideo;
            cacheVideoSwitch.CheckedChange += (sender, args) =>
            {
                Preferences.cacheVideo = args.IsChecked;
                Preferences.save();
            };


            var photoQualitySwitch = FindViewById<Switch>(Resource.Id.photoQualitySwitch);
            photoQualitySwitch.Checked = Preferences.jpgQuality > 0;
            photoQualitySwitch.CheckedChange += (sender, args) =>
            {
                Preferences.jpgQuality = args.IsChecked ? 1 : 0;
                Preferences.save();
                Tello.setJpgQuality(Preferences.jpgQuality);
            };

            //Settings button
            Button convertVideoButton = FindViewById<Button>(Resource.Id.convertVideoButton);
            convertVideoButton.Click += async delegate
            {
                if (Tello.connected && Tello.state.flying)
                    return;//Don't allow convert when flying. 

                if (true)
                {
                    try
                    {
                        FileData fileData = await CrossFilePicker.Current.PickFile();
                        if (fileData == null)
                            return; // user canceled file picking
                        string fileName = fileData.FileName;
                        //string contents = System.Text.Encoding.UTF8.GetString(fileData.DataArray);
                        Console.WriteLine(fileData.FilePath);

                        System.Console.WriteLine("File name chosen: " + fileName);
                        //System.Console.WriteLine("File data: " + contents);


                        RunOnUiThread(async () => {
                            try
                            {
                                if (!fileName.ToLower().EndsWith(".h264"))
                                {
                                    Toast.MakeText(Application.Context, "Error. Can only convert .h264 files", ToastLength.Long).Show();
                                    return;
                                }

                                var videoConverter = new aTello.VideoConverter();
                                var result = await videoConverter.ConvertFileAsync(this, new Java.IO.File(fileData.FilePath));
                                Toast.MakeText(Application.Context, "Video Conversion. Result:" + result, ToastLength.Long).Show();
                            }catch(Exception ex)
                            {
                                Toast.MakeText(Application.Context, "Video Conversion. FAIL:" + ex.Message, ToastLength.Long).Show();
                            }
                        });

                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine("Exception choosing file: " + ex.ToString());
                    }
                    return;
                }

            };


            Button convertAllVideoButton = FindViewById<Button>(Resource.Id.convertAllVideoButton);
            convertAllVideoButton.Click += async delegate
            {
                if (Tello.connected && Tello.state.flying)
                    return;//Don't allow convert when flying. 

                var path = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, "aTello/video/");
                Java.IO.File f = new Java.IO.File(path);
                var files = f.ListFiles().ToList();

                //append cache files to list.
                path = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, "aTello/video/cache");
                f = new Java.IO.File(path);
                files.AddRange(f.ListFiles());

                foreach (Java.IO.File inFile in files)
                {
                    if (!inFile.IsDirectory && inFile.Name.EndsWith(".h264"))
                    {
                        RunOnUiThread(async () =>
                        {
                            var videoConverter = new aTello.VideoConverter();
                            var inF = new Java.IO.File(inFile.Path);
                            var result = await videoConverter.ConvertFileAsync(this, inF);
                            Toast.MakeText(Application.Context, "Video Converted. Result:" + result, ToastLength.Long).Show();
                            if(result.StartsWith("Success"))
                            {
                                inF.Delete();
                            }
                        });
                    }
                }
            };

            Button sharePhotoButton = FindViewById<Button>(Resource.Id.sharePhotoButton);
            sharePhotoButton.Click += async delegate
            {
                if (Tello.connected && Tello.state.flying)
                    return;//Don't allow convert when flying. 

                var uri = Android.Net.Uri.FromFile(new Java.IO.File(Tello.picPath));
                shareImage(uri);
                return;

            };
            //EditText text = FindViewById<EditText>(Resource.Id.maxHeightText);
            //text.AfterTextChanged += delegate {
            //    Tello.setMaxHeight(int.Parse(text.Text));
            //};
            //text = FindViewById<EditText>(Resource.Id.exposureText);
            //text.AfterTextChanged += delegate {
            //    Tello.setEV(int.Parse(text.Text));
            //};
            //text = FindViewById<EditText>(Resource.Id.attAngleText);
            //text.AfterTextChanged += delegate {
            //    Tello.setAttAngle(int.Parse(text.Text));
            //};
            //text = FindViewById<EditText>(Resource.Id.eisText);
            //text.AfterTextChanged += delegate {
            //    Tello.setEIS(int.Parse(text.Text));
            //};
        }
        // Share image
        private void shareImage(Android.Net.Uri imagePath)
        {
            Intent intent = new Intent();
            intent.PutExtra(Intent.ActionView, Tello.picPath);
            intent.SetType("image/*");
            intent.SetAction(Intent.ActionGetContent);
            StartActivityForResult(Intent.CreateChooser(intent, "Select Picture"), 0);
        }
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            switch (requestCode)
            {
                case 0:
                    if (resultCode == Result.Ok)
                    {
                        var selectedImage = data.Data;
                        Intent sharingIntent = new Intent(Intent.ActionSend);
                        sharingIntent.AddFlags(ActivityFlags.ClearWhenTaskReset);
                        sharingIntent.SetType("image/*");
                        sharingIntent.PutExtra(Intent.ExtraStream, selectedImage);
                        StartActivity(Intent.CreateChooser(sharingIntent, "Share Image Using"));
                        //imageview.setImageUri(selectedImage);
                    }
                    break;
                case 1:
                    if (resultCode == Result.Ok)
                    {
                        var selectedImage = data.Data;
                        //imageview.setImageUri(selectedImage);
                    }
                    break;
            }
        }
    }
}