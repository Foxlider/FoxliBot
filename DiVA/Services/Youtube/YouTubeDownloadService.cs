using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DiVA.Services.YouTube
{
    /// <summary>
    /// Youtube Downloader
    /// </summary>
    public class YouTubeDownloadService
    {
        /// <summary>
        /// Donwload a video
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task<DownloadedVideo> DownloadVideo(DownloadedVideo video)
        {
            //var filename = Guid.NewGuid();
            //DownloadedVideo file = await GetVideoData(search);
            //if (File.Exists(Path.Combine("Songs", $"{file.DisplayID}.mp3")))
            //{
            //    return file;
            //}
            var youtubeDl = StartYoutubeDl(
                $"-o Songs/{video.DisplayID}.mp3 --restrict-filenames --extract-audio --no-overwrites --print-json --audio-format mp3 {video.Url}");

            if (youtubeDl == null)
            {
                Log.Warning("Error: Unable to start process", "Audio Download");
                return null;
            }

            var jsonOutput = await youtubeDl.StandardOutput.ReadToEndAsync();
            youtubeDl.WaitForExit();
            Log.Information($"Download completed with exit code {youtubeDl.ExitCode}", "Audio Download");

            return JsonConvert.DeserializeObject<DownloadedVideo>(jsonOutput);
        }

        /// <summary>
        /// Download a Livestream
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<StreamMetadata> GetLivestreamData(string url)
        {
            var youtubeDl = StartYoutubeDl("--print-json --skip-download " + url);
            var jsonOutput = await youtubeDl.StandardOutput.ReadToEndAsync();
            youtubeDl.WaitForExit();
            Log.Information($"Download completed with exit code {youtubeDl.ExitCode}", "Audio Download");

            return JsonConvert.DeserializeObject<StreamMetadata>(jsonOutput);
        }
        
        /// <summary>
        /// Download a Livestream
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static async Task<DownloadedVideo> GetVideoData(string search)
        {
            var youtubeDl = StartYoutubeDl($"--print-json --skip-download ytsearch:\"{search}\"");
            var jsonOutput = await youtubeDl.StandardOutput.ReadToEndAsync();
            youtubeDl.WaitForExit();
            Log.Information($"Download completed with exit code {youtubeDl.ExitCode}", "Audio Download");

            return JsonConvert.DeserializeObject<DownloadedVideo>(jsonOutput);
        }

        private static Process StartYoutubeDl(string arguments)
        { 
            var youtubeDlStartupInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                FileName = "youtube-dl",
                Arguments = arguments
            };

            Log.Information($"Starting youtube-dl with arguments: {youtubeDlStartupInfo.Arguments}", "Audio Download");
            return Process.Start(youtubeDlStartupInfo);
        }
    }
}