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
        public static int rxAxis = 0, ryAxis = 1, lxAxis = 2, lyAxis = 3;
        public static int speedButtonIndex = 5;
        public static int landButtonIndex = 6;
        public static int takeoffButtonIndex = 7;

        static Preferences()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            lxAxis = prefs.GetInt("lxAxis", lxAxis);
            lyAxis = prefs.GetInt("lyAxis", lyAxis);
            rxAxis = prefs.GetInt("rxAxis", rxAxis);
            ryAxis = prefs.GetInt("ryAxis", ryAxis);

            takeoffButtonIndex = prefs.GetInt("takeoffButtonIndex", takeoffButtonIndex);
            landButtonIndex = prefs.GetInt("landButtonIndex", landButtonIndex);
            speedButtonIndex = prefs.GetInt("speedButtonIndex", speedButtonIndex);
        }
        public static void save()
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            ISharedPreferencesEditor editor = prefs.Edit();

            editor.PutInt("lxAxis", lxAxis);
            editor.PutInt("lyAxis", lyAxis);
            editor.PutInt("rxAxis", rxAxis);
            editor.PutInt("ryAxis", ryAxis);

            editor.PutInt("takeoffButtonIndex", takeoffButtonIndex);
            editor.PutInt("landButtonIndex", landButtonIndex);
            editor.PutInt("speedButtonIndex", speedButtonIndex);

            editor.Apply();        // applies changes asynchronously on newer APIs
        }
    }
}