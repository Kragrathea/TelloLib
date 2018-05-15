# xamarin-android-ffmpeg
Xamarin Android FFMpeg binding

# For Android 6.0 onwards, use Xamarin.Android.MP4Transcoder
Android 6.0 onwards, `text relocations` are strictly prohibited, many source files used in `ffmpeg` use `text relocations` so `ffmpeg` will never run on future android builds unless they rewrite large library and replace them with alternative of `text relocations`. For this, only alternative is to use Android's native Mp4 transcoder.

MP4Transcoder internally uses https://github.com/ypresto/android-transcoder , please read license before using Xamarin.Android.MP4Transcoder

        Install-Package Xamarin.Android.MP4Transcoder


             if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat) {

                await Xamarin.MP4Transcoder.Transcoder.For720pFormat().ConvertAsync(inputFile, ouputFile, f => {
                    onProgress?.Invoke((int)(f * (double)100), 100);
                });
                return ouputFile;

            }


# Big Thanks
https://github.com/WritingMinds/ffmpeg-android-java

# Licensing
This code is licensed under MIT, however, you must use this library by accepting and following licensing terms mentioned in the source project at https://github.com/WritingMinds/ffmpeg-android-java

# Nuget Package
You can download Xamarin.Android.FFmpeg package from Nuget Package manager or run following command in Nuget Package Console.

        Install-Package Xamarin.Android.FFmpeg

# Usage

        public class VideoConverter 
        {

            public VideoConverter()
            {

            }

		/**
		* This method must be called from UI thread.
		***/
            public Task<File> ConvertFileAsync(Context context,
                File inputFile, 
                Action<string> logger = null, 
                Action<int,int> onProgress = null)
            {
                File ouputFile = new File(inputFile.CanonicalPath + ".mpg");

                ouputFile.DeleteOnExit();

                List<string> cmd = new List<string>();
                cmd.Add("-y");
                cmd.Add("-i");
                cmd.Add(inputFile.CanonicalPath);

                MediaMetadataRetriever m = new MediaMetadataRetriever();
                m.SetDataSource(inputFile.CanonicalPath);

                string rotate = m.ExtractMetadata(Android.Media.MetadataKey.VideoRotation);

                int r = 0;

                if (!string.IsNullOrWhiteSpace(rotate)) {
                    r = int.Parse(rotate);
                }

                cmd.Add("-b:v");
                cmd.Add("1M");

                cmd.Add("-b:a");
                cmd.Add("128k");


                switch (r)
                {
                    case 270:
                        cmd.Add("-vf scale=-1:480,transpose=cclock");
                        break;
                    case 180:
                        cmd.Add("-vf scale=-1:480,transpose=cclock,transpose=cclock");
                        break;
                    case 90:
                        cmd.Add("-vf scale=480:-1,transpose=clock");
                        break;
                    case 0:
                        cmd.Add("-vf scale=-1:480");
                        break;
                    default:

                        break;
                }

                cmd.Add("-f");
                cmd.Add("mpeg");

                cmd.Add(ouputFile.CanonicalPath);

                string cmdParams = string.Join(" ", cmd);

                int total = 0;
                int current = 0;

		await FFMpeg.Xamarin.FFMpegLibrary.Run(
			context,
			cmdParams 
			, (s) => {
				logger?.Invoke(s);
				int n = Extract(s, "Duration:", ",");
				if (n != -1) {
					total = n;
				}
				n = Extract(s, "time=", " bitrate=");
				if (n != -1) {
					current = n;
					onProgress?.Invoke(current, total);
				}
			});

                return ouputFile;
            }

            int Extract(String text, String start, String end)
            {
                int i = text.IndexOf(start);
                if (i != -1)
                {
                    text = text.Substring(i + start.Length);
                    i = text.IndexOf(end);
                    if (i != -1)
                    {
                        text = text.Substring(0, i);
                        return parseTime(text);
                    }
                }
                return -1;
            }

            public static int parseTime(String time)
            {
                time = time.Trim();
                String[] tokens = time.Split(':');
                int hours = int.Parse(tokens[0]);
                int minutes = int.Parse(tokens[1]);
                float seconds = float.Parse(tokens[2]);
                int s = (int)seconds * 100;
                return hours * 360000 + minutes * 60100 + s;
            }

        }

