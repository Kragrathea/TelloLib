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
using System.IO;

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
            static private Surface surface=null;//todo.

            static string path = "aTello/video/";
            static string videoFilePath ;


            static private void drawBuffer(ByteBuffer buffer, byte[] data)
            {
                if(videoFilePath==null)
                {
                    System.IO.Directory.CreateDirectory(Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path));
                    videoFilePath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.Path, path + DateTime.Now.ToString("processed") + ".bin");
                }
                using (var stream = new FileStream(videoFilePath, FileMode.Append))
                {
                    //var bytes = new byte[buffer.Remaining()];
                   // buffer.Get(bytes, 0, bytes.Length);

                    stream.Write(data, 0, data.Length);//
                }
            }

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
                            drawBuffer(byteBuffer2,buffer);
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
                return;// ret;
            }

            static public void stop()
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

            static public void config(Surface surface,int width, int height, byte[] sps, byte[] pps)
            {
                if (sps == null || pps == null)//not ready.
                    return;

                if (bConfigured)
                {
                    return;
                }
                if (codec != null)
                {
                    stop();
                }
                Decoder.width = width;
                Decoder.height = height;
                Decoder.sps = sps;
                Decoder.pps = pps;
                MediaFormat videoFormat = MediaFormat.CreateVideoFormat("video/avc", width, height);
                videoFormat.SetByteBuffer("csd-0",ByteBuffer.Wrap(sps));
                videoFormat.SetByteBuffer("csd-1", ByteBuffer.Wrap(pps));
                videoFormat.SetInteger("color-format", 19);

                string str = videoFormat.GetString("mime");
                try
                {
                    codec = MediaCodec.CreateDecoderByType(str);
                    codec.Configure(videoFormat, surface, (MediaCrypto)null, 0);
                    codec.Start();
                    bConfigured = true;
                }
                catch (Exception ex)
                {
                    var xstr = ex.Message.ToString();
                }
            }

        }

    }
}

