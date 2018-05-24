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
using System.Threading.Tasks;
using Java.IO;
using Android.Content.Res;
using System.IO;

namespace FFMpeg.Xamarin
{
    public class FFMpegLibrary
    {
        private static bool _initialized = false;

        private static Java.IO.File _ffmpegFile;
        public static string ResultString = "";

        private static void copyAssets(Context context)
        {
            AssetManager assetManager = context.Assets;
            String appFileDirectory = context.FilesDir.Path;
            String executableFilePath = appFileDirectory + "/ffmpeg";

            try
            {
                var fIn = assetManager.Open("ffmpeg");
                Java.IO.File outFile = new Java.IO.File(executableFilePath);

                OutputStream fOut = null;
                fOut = new FileOutputStream(outFile);

                byte[] buffer = new byte[1024];
                int read;
                while ((read = fIn.Read(buffer, 0, 1024)) >0)
                {
                    fOut.Write(buffer, 0, read);
                }
                fIn.Close();
                fIn = null;
                fOut.Flush();
                fOut.Close();
                fOut = null;

                Java.IO.File execFile = new Java.IO.File(executableFilePath);
                execFile.SetExecutable(true);
            }
            catch (Exception e)
            {
                //Log.e(TAG, "Failed to copy asset file: " + filename, e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task Init(Context context, string cdn = null, string downloadTitle = null, string downloadMessage = null)
        {
            ResultString = "init "+ _initialized;
            if (_initialized)
                return;

            var filesDir = context.FilesDir;

            _ffmpegFile = new Java.IO.File(filesDir + "/ffmpeg");

            if (_ffmpegFile.Exists() && _ffmpegFile.CanExecute())
            {
                _initialized = true;
                return;
            }
            copyAssets(context);
            if (_ffmpegFile.Exists())
            {
                _initialized = true;
            }
        }

        /// <summary>
        /// This must be called from main ui thread only...
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cmd"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static async Task<int> Run(Context context, string cmd, Action<string> logger = null)
        {
            try
            {
                TaskCompletionSource<int> source = new TaskCompletionSource<int>();

                await Init(context);

                if(!_initialized)
                {
                    if (!_ffmpegFile.Exists())
                        ResultString = "Ffmpeg missing";
                    else if(!_ffmpegFile.CanExecute())
                        ResultString = "Ffmpeg cant execute";

                    source.SetResult(-1);
                    return await source.Task;
                }

                await Task.Run(() =>
                {
                    try
                    {
                        int n = _Run(context, cmd, logger);
                        source.SetResult(n);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                        source.SetException(ex);
                    }
                });

                return await source.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                throw ex;
            }
        }
        
        private static int _Run(Context context, string cmd, Action<string> logger = null)
        {
            TaskCompletionSource<int> task = new TaskCompletionSource<int>();

            System.Diagnostics.Debug.WriteLine($"ffmpeg initialized");

            //var process = Java.Lang.Runtime.GetRuntime().Exec( Instance.ffmpegFile.CanonicalPath + " " + cmd );

            var startInfo = new System.Diagnostics.ProcessStartInfo(_ffmpegFile.CanonicalPath, cmd);

            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            var process = new System.Diagnostics.Process();

            process.StartInfo = startInfo;


            bool finished = false;

            string error = null;

            process.Start();
            
            Task.Run(() =>
            {
                try
                {
                    using (var reader = process.StandardError)
                    {
                        StringBuilder processOutput = new StringBuilder();
                        while (!finished)
                        {
                            var line = reader.ReadLine();
                            if (line == null)
                                break;
                            logger?.Invoke(line);
                            processOutput.Append(line);

                            if (line.StartsWith("final ratefactor:"))
                            {
                                Task.Run(async () =>
                                {
                                    await Task.Delay(TimeSpan.FromMinutes(1));
                                    finished = true;
                                });
                            }
                        }
                        error = processOutput.ToString();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            });

            while (!finished)
            {
                process.WaitForExit(10000);
                if (process.HasExited)
                {
                    break;
                }
            }

            return process.ExitCode;
        }
    }
}