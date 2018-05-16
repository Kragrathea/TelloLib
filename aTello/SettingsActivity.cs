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
using TelloLib;
using static Android.Views.ViewGroup;

namespace aTello
{
    [Activity(Label = "Settings", Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen")]
    public class SettingsActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Settings);

            //var np = (NumberPicker)FindViewById(Resource.Id.numberPicker1);
            //np.MaxValue = 6;


            /*           Button myButton = new Button(this);
                       myButton.Text = "Push Me";

                       LinearLayout ll = (LinearLayout)FindViewById(Resource.Id.joyLayout);
                       LayoutParams lp = new LayoutParams(LayoutParams.MatchParent, LayoutParams.WrapContent);
                       ll.AddView(myButton, lp);
           */

            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
            var lxAxis = prefs.GetInt("lxAxis", 0);
            var lyAxis = prefs.GetInt("lyAxis", 1);
            var rxAxis = prefs.GetInt("rxAxis", 2);
            var ryAxis = prefs.GetInt("ryAxis", 3);

            var takeoffButtonIndex = prefs.GetInt("takeoffButtonIndex", 7);
            var landButtonIndex = prefs.GetInt("landButtonIndex", 6);
            var speedButtonIndex = prefs.GetInt("speedButtonIndex", 5);



            ISharedPreferencesEditor editor = prefs.Edit();
            //editor.PutBoolean("key_for_my_bool_value", true);
            //editor.Apply();        // applies changes asynchronously on newer APIs



            var items = new List<int>() { 0,1,2,3,4,5,6 };
            var axisAdapter = new ArrayAdapter<int>(this, Android.Resource.Layout.SimpleSpinnerItem, items);
            var spinner = FindViewById<Spinner>(Resource.Id.lxSpinner);
            spinner.Adapter = axisAdapter;
            spinner.SetSelection(lxAxis);
            spinner.ItemSelected += (sender, args) =>
            {
                editor.PutInt("lxAxis", args.Position);
                editor.Apply();        // applies changes asynchronously on newer APIs
            };

            spinner = FindViewById<Spinner>(Resource.Id.lySpinner);
            spinner.Adapter = axisAdapter;
            spinner.SetSelection(lyAxis);
            spinner.ItemSelected += (sender, args) =>
            {
                editor.PutInt("lyAxis", args.Position);
                editor.Apply();        // applies changes asynchronously on newer APIs
            };

            spinner = FindViewById<Spinner>(Resource.Id.rxSpinner);
            spinner.Adapter = axisAdapter;
            spinner.SetSelection(rxAxis);
            spinner.ItemSelected += (sender, args) =>
            {
                editor.PutInt("rxAxis", args.Position);
                editor.Apply();        // applies changes asynchronously on newer APIs
            };

            spinner = FindViewById<Spinner>(Resource.Id.rySpinner);
            spinner.Adapter = axisAdapter;
            spinner.SetSelection(ryAxis);
            spinner.ItemSelected += (sender, args) =>
            {
                editor.PutInt("ryAxis", args.Position);
                editor.Apply();        // applies changes asynchronously on newer APIs
            };

            var buttonitems = new List<int>() { 0, 1, 2, 3, 4, 5, 6,7,8,9,10,11,12,13,14,15,16 };
            var buttonAdapter = new ArrayAdapter<int>(this, Android.Resource.Layout.SimpleSpinnerItem, buttonitems);
            spinner = FindViewById<Spinner>(Resource.Id.takeoffButttonSpinner);
            spinner.Adapter = buttonAdapter;
            spinner.SetSelection(takeoffButtonIndex);
            spinner.ItemSelected += (sender, args) =>
            {
                editor.PutInt("takeoffButtonIndex", args.Position);
                editor.Apply();        // applies changes asynchronously on newer APIs
            };

            spinner = FindViewById<Spinner>(Resource.Id.landButtonSpinner);
            spinner.Adapter = buttonAdapter;
            spinner.SetSelection(landButtonIndex);
            spinner.ItemSelected += (sender, args) =>
            {
                editor.PutInt("landButtonIndex", args.Position);
                editor.Apply();        // applies changes asynchronously on newer APIs
            };

            spinner = FindViewById<Spinner>(Resource.Id.speedButtonSpinner);
            spinner.Adapter = buttonAdapter;
            spinner.SetSelection(speedButtonIndex);
            spinner.ItemSelected += (sender, args) =>
            {
                editor.PutInt("speedButtonIndex", args.Position);
                editor.Apply();        // applies changes asynchronously on newer APIs
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
    }
}