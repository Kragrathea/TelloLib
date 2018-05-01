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
    }
}
/*
//package com.ryzerobotics.tello.gcs.engine.video.a.a;

//import android.media.MediaCrypto;
//import android.media.MediaFormat;
//import android.media.MediaCodec$BufferInfo;
//import com.ryzerobotics.tello.common.utils.t;
//import com.ryzerobotics.tello.gcs.engine.video.a.d;
//import android.media.MediaCodec;

public class Decoder
{
    public AtomicBoolean ab;
    byte[] buffer;
    private MediaCodec codec;
    private Surface surface;
    private bool bConfigured;
    private ByteBuffer csd0;
    private ByteBuffer csd1;
    private int width;
    private int height;
    private Surface jsurface;

    public Decoder(Surface d)
    {
        surface = d;
        ab = new AtomicBoolean();
    }

    public int decode(byte[] array)//, d d)
    {
        //synchronized(this)
            {
            //t.a("ZOMediaCodec decodeH264", "ENTER->");
            int n;
            if (codec == null)
            {
                //t.a("ZOMediaCodec decodeH264", "mediaCodec= NULL ");
                config(jsurface, width, height, csd0, csd1);
                n = -1;
            }
            else
            {
                //t.a("ZOMediaCodec decodeH264", "decodeH264 START->");
                try
                {
                     ByteBuffer[] inputBuffers = codec.GetInputBuffers();
                     ByteBuffer[] outputBuffers = codec.GetOutputBuffers();
                     int dequeueInputBuffer = codec.DequeueInputBuffer(-1L);
                    //t.a("ZOMediaCodec decodeH264", "inputBufferIndex " + dequeueInputBuffer);
                    if (dequeueInputBuffer >= 0)
                    {
                         ByteBuffer byteBuffer = inputBuffers[dequeueInputBuffer];
                        byteBuffer.Clear();
                        byteBuffer.Put(array);
                        codec.QueueInputBuffer(dequeueInputBuffer, 0, array.Length, 0L, 0);
                    }
                     MediaCodec.BufferInfo BufferInfo = new MediaCodec.BufferInfo();
                    int i = codec.DequeueOutputBuffer(BufferInfo, 0L);
                    //t.a("ZOMediaCodec decodeH264", "outputBufferIndex " + i);
                    while (i >= 0)
                    {
                        if (jsurface == null)
                        {
                             ByteBuffer byteBuffer2 = outputBuffers[i];
                            if (buffer == null || buffer.Length != BufferInfo.Size) {
                                buffer = new byte[BufferInfo.Size];
                            }
                            byteBuffer2.Get(buffer);
                            //d.a(BufferInfo.Flags, false, buffer);
                            drawBuffer(buffer);
                        }
                        codec.ReleaseOutputBuffer(i, true);
                        i = codec.DequeueOutputBuffer(BufferInfo, 0L);
                    }
                }
                catch (Exception ex)
                {
                    //CrashReport.postCatchedException(ex);
                    //a(j, h, i, f, g);
                    //ex.printStackTrace();
                }
                n = 0;
            }
            return n;
        }
    }

    public void stop( int n)
    {
        //synchronized(this) 
            {
            if (codec == null)
            {
                return;
            }
            try
            {
                if (!ab.Get())
                {
                    //t.c("ZOMediaCodec ", "__stop" + n);
                    codec.Stop();
                    codec.Release();
                    codec = null;
                    ab.Set(true);
                }
            }
            catch (Exception ex)
            {
                //t.c("ZOMediaCodec ", "__stop Exception" + ex.getMessage());
                //ex.printStackTrace();
            }
        }
    }

    //@TargetApi(23)
    public void config( Surface j,  int width,  int height,  ByteBuffer csd0,  ByteBuffer csd1)
    {
        //t.a("ZOMediaCodec configure", "mConfigured " + e);
        if (bConfigured)
        {
            return;
        }
        jsurface = j;
        if (codec != null)
        {
            stop(2);
        }
        width = width;
        height = height;
        csd0 = csd0;
        csd1 = csd1;
        //t.a("ZOMediaCodec ", "configure");
         MediaFormat videoFormat = MediaFormat.CreateVideoFormat("video/avc", width, height);
        videoFormat.SetByteBuffer("csd-0", csd0);
        videoFormat.SetByteBuffer("csd-1", csd1);
        if (j == null)
        {
            videoFormat.SetInteger("color-format", 19);
        }
         string str = videoFormat.GetString("mime");
        try
        {
            (codec = MediaCodec.CreateDecoderByType(str)).Configure(videoFormat, j, (MediaCrypto)null, 0);
            codec.Start();
            ab.Set(false);
            bConfigured = true;
        }
        catch (Exception ex)
        {
            //t.a("ZOMediaCodec configure", " Failed to create codec = " + ex);
        }
    }

    //@TargetApi(21)
    public void A( bool e)
    {
        //synchronized(this) 
        {
            bConfigured = e;
        }
    }
}
*/