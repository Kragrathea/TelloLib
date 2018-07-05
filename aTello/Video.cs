using System;
using System.Collections.Generic;
using System.Linq;

using Android.Views;
using Android.Media;
using Java.Nio;
//Not used anymore. Left for a bit for reference. 
namespace aTello
{
    public class Video
    {
        public class Decoder
        {
            static byte[] buffer;
            static private MediaCodec codec;
            static private MediaCodec vidCodec;
            static private MediaCodec picCodec;

            static private bool bConfigured;
            //pic mode sps
            static private byte[] sps = new byte[] { 0, 0, 0, 1, 103, 77, 64, 40, 149, 160, 60, 5, 185 };// null;

            //vid mode sps
            static private byte[] vidSps = new byte[] { 0, 0, 0, 1, 103, 77, 64, 40, 149, 160, 20, 1, 110, 64 };// null;

            static private byte[] pps = new byte[] { 0, 0, 0, 1, 104, 238, 56, 128 };// null;
            static private int width=960;
            static private int height=720;
            static private Surface picSurface=null;
            static private Surface vidSurface = null;

            public static void Config(Surface picSur, Surface vidSur)
            {
                picSurface = picSur;
                vidSurface = vidSur;
            }
            private static void Init()
            { 
                MediaFormat videoFormat = MediaFormat.CreateVideoFormat("video/avc", width, height);
                videoFormat.SetByteBuffer("csd-0", ByteBuffer.Wrap(sps));
                videoFormat.SetByteBuffer("csd-1", ByteBuffer.Wrap(pps));

                string str = videoFormat.GetString("mime");
                try
                {
                    var cdx = MediaCodec.CreateDecoderByType(str);
                    cdx.Configure(videoFormat, picSurface, (MediaCrypto)null, 0);
                    cdx.SetVideoScalingMode(VideoScalingMode.ScaleToFit);
                    cdx.Start();

                    picCodec = cdx;
                    //codec = picCodec;
                }
                catch (Exception ex)
                {
                }


                videoFormat = MediaFormat.CreateVideoFormat("video/avc", 1280, 720);
                videoFormat.SetByteBuffer("csd-0", ByteBuffer.Wrap(vidSps));
                videoFormat.SetByteBuffer("csd-1", ByteBuffer.Wrap(pps));

                try
                {
                    var cdx = MediaCodec.CreateDecoderByType(videoFormat.GetString("mime"));
                    cdx.Configure(videoFormat, vidSurface, (MediaCrypto)null, 0);
                    cdx.SetVideoScalingMode(VideoScalingMode.ScaleToFit);
                    cdx.Start();

                    vidCodec = cdx;
                }
                catch (Exception ex)
                {
                }

                bConfigured = true;
            }

            static public void decode(byte[] array)
            {
                if (bConfigured == false)
                {
                    Init();
                }

                var nalType = array[4] & 0x1f;
                if(nalType==7)
                {
                    //sps = array.ToArray();
                    if (array.Length == 14)
                        codec = vidCodec;
                    if (array.Length == 13)
                        codec = picCodec;
                    return;
                }
                if (nalType == 8)
                {
                    //pps = array.ToArray();
                    return;
                }
                int ret;
                if (bConfigured == false || codec == null)
                {
                    return;
                }

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
                            if (picSurface == null)//Only if not using display surface. 
                            {
                                ByteBuffer byteBuffer2 = outputBuffers[i];
                                if (buffer == null || buffer.Length != BufferInfo.Size)
                                {
                                    buffer = new byte[BufferInfo.Size];
                                }
                                byteBuffer2.Get(buffer);
                                //do something with raw frame in buffer. 
                            }
                            codec.ReleaseOutputBuffer(i, true);
                            codec.SetVideoScalingMode(VideoScalingMode.ScaleToFit);

                            i = codec.DequeueOutputBuffer(BufferInfo, 0L);
                        }
                    }
                    catch (Exception ex)
                    {
                        //codec.Reset();
                        //attempt to recover.
                        stop();
                    }
                    ret = 0;
                }
                return;// ret;
            }

            static public void stop()
            {
                bConfigured = false;
                codec = null;
                if (vidCodec != null)
                {
                    try
                    {
                        vidCodec.Stop();
                        vidCodec.Release();
                    }
                    catch { }
                }

                if (picCodec != null)
                {
                    try
                    {
                        picCodec.Stop();
                        picCodec.Release();
                    }
                    catch { }
                }

            }
  
        }

    }
}

