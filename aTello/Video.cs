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
using Android.Media;
using Java.Nio;
using Java.Util.Concurrent.Atomic;

namespace aTello
{
    class Video
    {
        public class Decoder
        {
            byte[] buffer;
            private MediaCodec codec;
            private bool bConfigured;
            private ByteBuffer csd0;
            private ByteBuffer csd1;
            private int width;
            private int height;
            private Surface surface;//todo.

            public int decode(byte[] array)
            {
                int ret;
                if (codec == null)
                {
                    config(surface,width, height, csd0, csd1);
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
                            ByteBuffer byteBuffer = inputBuffers[dequeueInputBuffer];
                            byteBuffer.Clear();
                            byteBuffer.Put(array);
                            codec.QueueInputBuffer(dequeueInputBuffer, 0, array.Length, 0L, 0);
                        }
                        MediaCodec.BufferInfo BufferInfo = new MediaCodec.BufferInfo();
                        int i = codec.DequeueOutputBuffer(BufferInfo, 0L);
                        while (i >= 0)
                        {
                            ByteBuffer byteBuffer2 = outputBuffers[i];
                            if (buffer == null || buffer.Length != BufferInfo.Size)
                            {
                                buffer = new byte[BufferInfo.Size];
                            }
                            byteBuffer2.Get(buffer);
                            //d.a(BufferInfo.Flags, false, buffer);
                            //                           drawBuffer(buffer);
                            codec.ReleaseOutputBuffer(i, true);
                            i = codec.DequeueOutputBuffer(BufferInfo, 0L);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    ret = 0;
                }
                return ret;
            }

            public void stop()
            {
                if (codec == null)
                {
                    return;
                }
                try
                {
                    codec.Stop();
                    codec.Release();
                    codec = null;
                }
                catch (Exception ex)
                {
                }
            }

            public void config(Surface surface,int width, int height, ByteBuffer csd0, ByteBuffer csd1)
            {
                if (bConfigured)
                {
                    return;
                }
                if (codec != null)
                {
                    stop();
                }
                width = width;
                height = height;
                csd0 = csd0;
                csd1 = csd1;
                MediaFormat videoFormat = MediaFormat.CreateVideoFormat("video/avc", width, height);
                videoFormat.SetByteBuffer("csd-0", csd0);
                videoFormat.SetByteBuffer("csd-1", csd1);
                videoFormat.SetInteger("color-format", 19);
                string str = videoFormat.GetString("mime");
                try
                {
                    (codec = MediaCodec.CreateDecoderByType(str)).Configure(videoFormat, surface, (MediaCrypto)null, 0);
                    codec.Start();
                    bConfigured = true;
                }
                catch (Exception ex)
                {
                }
            }

        }

    }
}

