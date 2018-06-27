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

namespace aTello
{
 //       mSurfaceTextureListener = new SurfaceTextureListener(Activity);

    //TextureView.ISurfaceTextureListener mSurfaceTextureListener;
    class SurfaceTextureListener : Java.Lang.Object, TextureView.ISurfaceTextureListener
    {
        Activity Activity { get; set; }

        public SurfaceTextureListener(Activity activity)
        {
            Activity = activity;
        }

        public void OnSurfaceTextureAvailable(Android.Graphics.SurfaceTexture surfaceTexture, int width, int height)
        {
            var st = new Surface(surfaceTexture);
            Video.Decoder.surface = st;

        }

        public bool OnSurfaceTextureDestroyed(Android.Graphics.SurfaceTexture surface)
        {
            return true;
        }

        public void OnSurfaceTextureSizeChanged(Android.Graphics.SurfaceTexture surface, int width, int height)
        {
        }

        public void OnSurfaceTextureUpdated(Android.Graphics.SurfaceTexture surface)
        {
        }
    }
}