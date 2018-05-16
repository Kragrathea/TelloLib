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
            
            var items = new List<int>() { 0,1,2,3,4,5,6 };
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

            var buttonitems = new List<int>() { 0, 1, 2, 3, 4, 5, 6,7,8,9,10,11,12,13,14,15,16 };
            var buttonAdapter = new ArrayAdapter<int>(this, Android.Resource.Layout.SimpleSpinnerItem, buttonitems);
            spinner = FindViewById<Spinner>(Resource.Id.takeoffButttonSpinner);
            spinner.Adapter = buttonAdapter;
            spinner.SetSelection(Preferences.takeoffButtonIndex);
            spinner.ItemSelected += (sender, args) =>
            {
                Preferences.takeoffButtonIndex = args.Position;
                Preferences.save();
            };

            spinner = FindViewById<Spinner>(Resource.Id.landButtonSpinner);
            spinner.Adapter = buttonAdapter;
            spinner.SetSelection(Preferences.landButtonIndex);
            spinner.ItemSelected += (sender, args) =>
            {
                Preferences.landButtonIndex = args.Position;
                Preferences.save();
            };

            spinner = FindViewById<Spinner>(Resource.Id.speedButtonSpinner);
            spinner.Adapter = buttonAdapter;
            spinner.SetSelection(Preferences.speedButtonIndex);
            spinner.ItemSelected += (sender, args) =>
            {
                Preferences.speedButtonIndex = args.Position;
                Preferences.save();
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