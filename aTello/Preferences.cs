using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace aTello
{
    public static class Preferences
    {
        public static int lxAxis = 0, lyAxis = 1,rxAxis = 2, ryAxis = 3;

        public static Keycode flipButtonCode = Keycode.ButtonL2;
        public static Keycode speedButtonCode = Keycode.ButtonR2;
        public static Keycode landButtonCode = Keycode.ButtonSelect;
        public static Keycode takeoffButtonCode = Keycode.ButtonStart;
        public static Keycode pictureButtonCode = Keycode.ButtonR1;
        public static Keycode recButtonCode = Keycode.ButtonL1;
        public static Keycode homeButtonCode = Keycode.ButtonMode;

        public static int jpgQuality = 1;
        public static int exposure = 9;
        public static int videoBitRate = 0;
        public static int joyType = 0;
        public static bool onScreenJoy = false;
       
        public static int iFrameRate = TelloLib.Tello.iFrameRate;//5 = 4x second.
        public static bool cacheVideo = true;


        static Preferences()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            lxAxis = prefs.GetInt("lxAxis", lxAxis);
            lyAxis = prefs.GetInt("lyAxis", lyAxis);
            rxAxis = prefs.GetInt("rxAxis", rxAxis);
            ryAxis = prefs.GetInt("ryAxis", ryAxis);


            var jtype = prefs.GetInt("joyType", joyType);
            setJoyType(jtype);

            jpgQuality = prefs.GetInt("jpgQuality", jpgQuality);

            onScreenJoy = prefs.GetBoolean("onScreenJoy", onScreenJoy);

            exposure = prefs.GetInt("exposure", exposure);
            videoBitRate = prefs.GetInt("videoBitRate", videoBitRate);
            if (videoBitRate < 0 || videoBitRate > 5)
                videoBitRate = 0;

            cacheVideo = prefs.GetBoolean("cacheVideo", cacheVideo);

            iFrameRate = prefs.GetInt("iFrameRate", iFrameRate);
        }

        public static void setJoyType(int type)
        {
            switch(type)
            {
                case 0://generic
                    flipButtonCode = Keycode.ButtonL2;
                    speedButtonCode = Keycode.ButtonR2;
                    landButtonCode =  Keycode.ButtonSelect;
                    takeoffButtonCode = Keycode.ButtonStart;
                    pictureButtonCode = Keycode.ButtonR1;
                    recButtonCode = Keycode.ButtonL1;
                    homeButtonCode = Keycode.ButtonA;
                    joyType = type;
                    break;
                case 1://ps3
                    flipButtonCode = Keycode.ButtonL1;
                    speedButtonCode = Keycode.ButtonR1;
                    landButtonCode = Keycode.ButtonL2;
                    takeoffButtonCode = Keycode.ButtonR2;
                    pictureButtonCode = Keycode.ButtonZ;
                    recButtonCode = Keycode.ButtonY;
                    homeButtonCode = Keycode.ButtonMode;
                    joyType = type;
                    break;
            }
        }
        public static void save()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            ISharedPreferencesEditor editor = prefs.Edit();

            editor.PutInt("lxAxis", lxAxis);
            editor.PutInt("lyAxis", lyAxis);
            editor.PutInt("rxAxis", rxAxis);
            editor.PutInt("ryAxis", ryAxis);
            editor.PutInt("joyType", joyType);
            editor.PutBoolean("onScreenJoy", onScreenJoy);
            editor.PutInt("jpgQuality", jpgQuality);
            editor.PutInt("exposure", exposure);
            editor.PutInt("videoBitRate", videoBitRate);
            editor.PutBoolean("cacheVideo", cacheVideo);
            editor.PutInt("iFrameRate", iFrameRate);

            editor.Apply();        
        }
    }
}