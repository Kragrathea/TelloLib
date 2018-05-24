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

namespace FFMpeg.Xamarin
{
    public class FFMpegSource
    {
        public static string FFMPEGVersion { get; } = "3.0.1";

        public FFMpegSource(string arch, Func<string, bool> isArch, string hash)
        {
            this.Arch = arch;
            this.IsArch = isArch;
            this.Hash = hash;
        }

        public static FFMpegSource[] Sources = new FFMpegSource[] {
            new FFMpegSource("arm", x=> !x.EndsWith("86"), "yRVoeaZATQdZIR/lZxMsIa/io9U="),
            new FFMpegSource("x86", x=> x.EndsWith("86"), "mU4QKhrLEO0aROb9N7JOCJ/rVTA==")
        };

        public string Arch { get; }

        public string Hash { get; }
        
        //https://cdn.rawgit.com/neurospeech/xamarin-android-ffmpeg/master/binary/3.0.1.1/arm/ffmpeg
        //https://raw.githubusercontent.com/neurospeech/xamarin-android-ffmpeg/master/binary/3.0.1.1/arm/ffmpeg
        //public string Url => $"https://{FFMpegLibrary.Instance.CDNHost}/neurospeech/xamarin-android-ffmpeg/v1.0.7/binary/{FFMPEGVersion}/{Arch}/ffmpeg";

        public Func<string, bool> IsArch { get; }

        public static FFMpegSource Get()
        {
            string osArchitecture = Java.Lang.JavaSystem.GetProperty("os.arch");

            foreach (var source in Sources)
            {
                if (source.IsArch(osArchitecture))
                    return source;
            }

            throw new NotImplementedException();
        }

        public bool IsHashMatch(byte[] data)
        {
            var sha = System.Security.Cryptography.SHA1.Create();
            string h = Convert.ToBase64String(sha.ComputeHash(data));
            return true;// h == Hash;
        }
    }
}