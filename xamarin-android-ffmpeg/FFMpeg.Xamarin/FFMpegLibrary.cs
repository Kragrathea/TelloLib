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

namespace FFMpeg.Xamarin
{
    public class FFMpegLibrary
    {
        public static string EndOfFFMPEGLine = "final ratefactor:";

        public string CDNHost { get; set; } = "raw.githubusercontent.com";
        
        public readonly static FFMpegLibrary Instance = new FFMpegLibrary();

        private bool _initialized = false;

        private Java.IO.File _ffmpegFile;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task Init(Context context, string cdn = null, string downloadTitle = null, string downloadMessage = null)
        {
            if (_initialized)
                return;

            if (cdn != null)
            {
                CDNHost = cdn;
            }

            // do all initialization...
            var filesDir = context.FilesDir;

            _ffmpegFile = new Java.IO.File(filesDir + "/ffmpeg");

            FFMpegSource source = FFMpegSource.Get();

            await Task.Run(() =>
            {
                if (_ffmpegFile.Exists() && _ffmpegFile.Length()>15413150)
                {
                    try
                    {
                        //if (source.IsHashMatch(System.IO.File.ReadAllBytes(_ffmpegFile.CanonicalPath)))
                        {
                            if (!_ffmpegFile.CanExecute())
                                _ffmpegFile.SetExecutable(true);
                            _initialized = true;
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($" Error validating file {ex}");
                    }

                    // file is not same...

                    // delete the file...

                    if (_ffmpegFile.CanExecute())
                        _ffmpegFile.SetExecutable(false);
                    _ffmpegFile.Delete();
                    System.Diagnostics.Debug.WriteLine($"ffmpeg file deleted at {_ffmpegFile.AbsolutePath}");
                }
            });

            if (_initialized)
            {
                // ffmpeg file exists...
                return;
            }

            if (_ffmpegFile.Exists())
            {
                _ffmpegFile.Delete();
            }

            // lets try to download
            var dialog = new ProgressDialog(context);
            dialog.SetTitle(downloadMessage ?? "Downloading Video Converter");
            //dlg.SetMessage(downloadMessage ?? "Downloading Video Converter");
            dialog.Indeterminate = false;
            dialog.SetProgressStyle(ProgressDialogStyle.Horizontal);
            dialog.SetCancelable(false);
            dialog.CancelEvent += (s, e) =>
            {

            };

            dialog.SetCanceledOnTouchOutside(false);
            dialog.Show();
            using (var c = new System.Net.Http.HttpClient())
            {
                using (var fout = System.IO.File.OpenWrite(_ffmpegFile.AbsolutePath))
                {
                    string url = source.Url;
                    var g = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);

                    var h = await c.SendAsync(g, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);

                    var buffer = new byte[51200];

                    var s = await h.Content.ReadAsStreamAsync();
                    long total = h.Content.Headers.ContentLength.GetValueOrDefault();

                    IEnumerable<string> sl;
                    if (h.Headers.TryGetValues("Content-Length", out sl))
                    {
                        if (total == 0 && sl.Any())
                        {
                            long.TryParse(sl.FirstOrDefault(), out total);
                        }
                    }

                    int count = 0;
                    int progress = 0;

                    dialog.Max = (int)total;

                    while ((count = await s.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fout.WriteAsync(buffer, 0, count);

                        progress += count;

                        //System.Diagnostics.Debug.WriteLine($"Downloaded {progress} of {total} from {url}");

                        dialog.Progress = progress;
                    }
                    dialog.Hide();
                }
            }

            System.Diagnostics.Debug.WriteLine($"ffmpeg file copied at {_ffmpegFile.AbsolutePath}");

            if (!_ffmpegFile.CanExecute())
            {
                _ffmpegFile.SetExecutable(true);
                System.Diagnostics.Debug.WriteLine($"ffmpeg file made executable");
            }

            _initialized = true;
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

                await Instance.Init(context);

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

            var startInfo = new System.Diagnostics.ProcessStartInfo(Instance._ffmpegFile.CanonicalPath, cmd);

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
                            
                            if (line.StartsWith(EndOfFFMPEGLine))
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