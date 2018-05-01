using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using TelloLib;

namespace aTello
{
    [Activity(Label = "Settings")]
    public class SettingsActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Settings);

            EditText text = FindViewById<EditText>(Resource.Id.maxHeightText);
            text.AfterTextChanged += delegate {
                Tello.setMaxHeight(int.Parse(text.Text));
            };
            text = FindViewById<EditText>(Resource.Id.exposureText);
            text.AfterTextChanged += delegate {
                Tello.setEV(int.Parse(text.Text));
            };
            text = FindViewById<EditText>(Resource.Id.attAngleText);
            text.AfterTextChanged += delegate {
                Tello.setAttAngle(int.Parse(text.Text));
            };
            text = FindViewById<EditText>(Resource.Id.eisText);
            text.AfterTextChanged += delegate {
                Tello.setEIS(int.Parse(text.Text));
            };
        }
    }
}