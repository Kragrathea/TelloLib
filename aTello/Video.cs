using System;
using System.Collections.Generic;
using System.Linq;

using Android.Views;
using Android.Media;
using Java.Nio;

namespace aTello
{
    public class Video
    {
        public class Decoder
        {
            static byte[] buffer;
            static private MediaCodec codec;
            static private bool bConfigured;
            static private byte[] sps=null;
            static private byte[] pps = null;
            static private int width=960;
            static private int height=720;
            static public Surface surface=null;

            static public void decode(byte[] array)
            {
                var nalType = array[4] & 0x1f;
                if(nalType==7)
                {
                    sps = array.ToArray();
                    return;
                }
                if (nalType == 8)
                {
                    pps = array.ToArray();
                    return;
                }
                int ret;
                if (codec == null)
                {
                    config(surface,width, height, sps, pps);
                    ret = -1;
                }
                else
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
                            if (surface == null)//Only if not using display surface. 
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
                        //attempt to recover.
                        stop();
                        config(surface, width, height, sps, pps);
                    }
                    ret = 0;
                }
                return;// ret;
            }

            static public void stop()
            {
                if (codec == null)
                    return;
                bConfigured = false;

                try
                {
                    codec.Stop();
                    codec.Release();
                }
                catch (Exception ex)
                {
                }
                codec = null;
            }
            static public void reconfig()
            {
                stop();
                bConfigured = false;
                config(surface, width, height, sps, pps);

                //sps = null;
                //pps = null;
            }
            static public void config(Surface surface,int width, int height, byte[] sps, byte[] pps)
            {
                if (sps == null || pps == null)//not ready.
                    return;

                if (bConfigured)
                    return;

                if (codec != null)
                    stop();

                Decoder.width = width;
                Decoder.height = height;
                Decoder.sps = sps;
                Decoder.pps = pps;
                MediaFormat videoFormat = MediaFormat.CreateVideoFormat("video/avc", width, height);
                videoFormat.SetByteBuffer("csd-0",ByteBuffer.Wrap(sps));
                videoFormat.SetByteBuffer("csd-1", ByteBuffer.Wrap(pps));
                //videoFormat.SetInteger("color-format", 19);

                string str = videoFormat.GetString("mime");
                try
                {
                    codec = MediaCodec.CreateDecoderByType(str);
                    codec.Configure(videoFormat, surface, (MediaCrypto)null, 0);
                    codec.SetVideoScalingMode(VideoScalingMode.ScaleToFit);
                    codec.Start();
                    bConfigured = true;
                }
                catch (Exception ex)
                {
                    var errstr = ex.Message.ToString();
                }
            }

        }

    }
}

