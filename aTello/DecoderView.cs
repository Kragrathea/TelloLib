using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Nio;

namespace aTello
{
    public class DecoderView : SurfaceView
    {
        byte[] buffer;
        private MediaCodec codec;

        private bool bConfigured;
        //pic mode sps
        private byte[] sps = new byte[] { 0, 0, 0, 1, 103, 77, 64, 40, 149, 160, 60, 5, 185 };
        private bool bWaitForKeyframe = true;

        //vid mode sps
        private byte[] vidSps = new byte[] { 0, 0, 0, 1, 103, 77, 64, 40, 149, 160, 20, 1, 110, 64 };

        private byte[] pps = new byte[] { 0, 0, 0, 1, 104, 238, 56, 128 };
        private int decoderWidth = 960;
        private int decoderHeight = 720;

        /*
         * 
         -            Video.Decoder.surface = FindViewById<SurfaceView>(Resource.Id.surfaceView).Holder.Surface;
-
-/*
-            var textureView = FindViewById<TextureView>(Resource.Id.textureView);
-            mSurfaceTextureListener = new SurfaceTextureListener(this);
-            textureView.SurfaceTextureListener = mSurfaceTextureListener;
-
-            //var st = new Surface(textureView.SurfaceTexture);
-            //Video.Decoder.surface = st;
-
-*/
            
         //       mSurfaceTextureListener = new SurfaceTextureListener(Activity);

    //TextureView.ISurfaceTextureListener mSurfaceTextureListener;
    class SurfaceTextureListener : Java.Lang.Object, TextureView.ISurfaceTextureListener
        {
            DecoderView view { get; set; }

            public SurfaceTextureListener(DecoderView view)
            {
                this.view = view;
                //Activity = activity;
            }

            public void OnSurfaceTextureAvailable(Android.Graphics.SurfaceTexture surfaceTexture, int width, int height)
            {
                //var st = new Surface(surfaceTexture);
                //view.surface = st;

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


        private void Init()
        {
            if (sps.Length == 14)
                decoderWidth = 1280;
            else
                decoderWidth = 960;

            MediaFormat videoFormat = MediaFormat.CreateVideoFormat("video/avc", decoderWidth, decoderHeight);
            videoFormat.SetByteBuffer("csd-0", ByteBuffer.Wrap(sps));
            videoFormat.SetByteBuffer("csd-1", ByteBuffer.Wrap(pps));

            string str = videoFormat.GetString("mime");
            try
            {
                var cdx = MediaCodec.CreateDecoderByType(str);
                cdx.Configure(videoFormat, Holder.Surface, (MediaCrypto)null, 0);
                cdx.SetVideoScalingMode(VideoScalingMode.ScaleToFit);
                cdx.Start();

                codec = cdx;
                bWaitForKeyframe = true;
                bConfigured = true;
            }
            catch (Exception ex)
            {
                //handle
                MainActivity.getActivity().notifyUser("VideoDecoder Init exception " + ex.Message,false);
            }
 return;
            //set surface aspect ratio
            MainActivity.getActivity().RunOnUiThread(() =>
            {
                float videoProportion = (float)decoderWidth / (float)decoderHeight;

                var size = new Android.Graphics.Point();
                MainActivity.getActivity().WindowManager.DefaultDisplay.GetSize(size);
                int screenWidth = size.X;
                int screenHeight = size.Y;
                float screenProportion = (float)screenWidth / (float)screenHeight;

                var lp = this.LayoutParameters;
                if (videoProportion > screenProportion)
                {
                    lp.Width = screenWidth;
                    lp.Height = (int)((float)screenWidth / videoProportion);
                }
                else
                {
                    lp.Width = (int)(videoProportion * (float)screenHeight);
                    lp.Height = screenHeight;
                }

                this.LayoutParameters = lp;
            });

        }

        public void decode(byte[] array)
        {
            if (bConfigured == false)
            {
                Init();
            }

            var nalType = array[4] & 0x1f;
//Console.WriteLine("nal:" + nalType);
            if (nalType == 7)
            {
                //sps = array.ToArray();
                if (array.Length != sps.Length)
                {
                    stop();
                    sps = array.ToArray();
                    Init();
                }
                return;
            }
            if (nalType == 8)
            {
                //pps = array.ToArray();
                return;
            }
            if (bConfigured == false)
            {
                return;
            }

            //Make sure keyframe is first. 
            if (nalType == 5)
            {
                bWaitForKeyframe = false;
                //pps = array.ToArray();
                //return;
            }
            if (bWaitForKeyframe)
                return;

            if (bConfigured)
            {
                try
                {
                    ByteBuffer[] inputBuffers = codec.GetInputBuffers();
                    ByteBuffer[] outputBuffers = codec.GetOutputBuffers();
                    int dequeueInputBuffer = codec.DequeueInputBuffer(-1L);
                    if (dequeueInputBuffer >= 0)
                    {
                        //Send data to decoder. 
                        ByteBuffer byteBuffer = inputBuffers[dequeueInputBuffer];
                        byteBuffer.Clear();
                        byteBuffer.Put(array);
                        codec.QueueInputBuffer(dequeueInputBuffer, 0, array.Length, 0L, 0);
                    }

                    //Show decoded frame
                    MediaCodec.BufferInfo BufferInfo = new MediaCodec.BufferInfo();
                    int i = codec.DequeueOutputBuffer(BufferInfo, 0L);
                    while (i >= 0)
                    {
                        /*if (picSurface == null)//Only if not using display surface. 
                        {
                            ByteBuffer byteBuffer2 = outputBuffers[i];
                            if (buffer == null || buffer.Length != BufferInfo.Size)
                            {
                                buffer = new byte[BufferInfo.Size];
                            }
                            byteBuffer2.Get(buffer);
                            //do something with raw frame in buffer. 
                        }*/

                        codec.ReleaseOutputBuffer(i, true);
                        codec.SetVideoScalingMode(VideoScalingMode.ScaleToFit);

                        i = codec.DequeueOutputBuffer(BufferInfo, 0L);
                    }
                }
                catch (Exception ex)
                {
                    MainActivity.getActivity().notifyUser("VideoDecoder decode exception " + ex.Message, false);

                    //attempt to recover.
                    //codec.Release();
                    //codec = null;
                    //bConfigured = false;

                    stop();
                }
            }
            return;// ret;
        }

        public void stop()
        {
            bConfigured = false;
            if (codec != null)
            {
                try
                {
                    //codec.Stop();
                    codec.Release();
                }
                catch (Exception ex)
                {
                    MainActivity.getActivity().notifyUser("VideoDecoder stop exception " + ex.Message, false);
                }
            }
            codec = null;
        }



        public DecoderView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public DecoderView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        private void Initialize()
        {
        }
    }
}