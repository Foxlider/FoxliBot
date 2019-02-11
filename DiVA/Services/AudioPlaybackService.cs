using Discord.Audio;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DiVA.Services
{
    /// <summary>
    /// Audio Player Service
    /// </summary>
    public class AudioPlaybackService
    {
        private Process _currentProcess;

        /// <summary>
        /// Voice sender
        /// </summary>
        /// <param name="client"></param>
        /// <param name="path"></param>
        /// <param name="speedModifier"></param>
        /// <returns></returns>
        public async Task SendAsync(IAudioClient client, string path)
        {
            //_currentProcess = CreateStream(path, speedModifier);
            //var output = _currentProcess.StandardOutput.BaseStream;
            //var discord = client.CreatePCMStream(AudioApplication.Mixed, bitrate: 48000, bufferMillis: 2000);
            //await output.CopyToAsync(discord);
            //await discord.FlushAsync();
            //await discord.DisposeAsync();
            //_currentProcess.WaitForExit();

            _currentProcess = CreateStream(path);
            using (var discord = client.CreatePCMStream(AudioApplication.Mixed, bitrate: 48000, bufferMillis: 2000))
            {
                await Task.Delay(2000);
                while (true)
                {
                    if (_currentProcess.HasExited)
                        break;
                    int blockSize = 2880;
                    byte[] buffer = new byte[blockSize];
                    int byteCount;
                    byteCount = await _currentProcess.StandardOutput.BaseStream.ReadAsync(buffer, 0, blockSize);
                    if (byteCount == 0)
                        break;
                    await discord.WriteAsync(buffer, 0, byteCount);
                }
                await discord.FlushAsync();
            }
        }

        /// <summary>
        /// Skipper
        /// </summary>
        public void StopCurrentOperation()
        {
            _currentProcess.Close();
            _currentProcess?.Kill();
            _currentProcess?.Dispose();
        }

        /// <summary>
        /// Stream creator
        /// </summary>
        /// <param name="path"></param>
        /// <param name="speedModifier"></param>
        /// <returns></returns>
        private static Process CreateStream(string path)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                //Arguments = $"-i \"{path}\" -ac 2 -f s16le -filter:a \"volume=0.02\" -ar {speedModifier}000 pipe:1",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            Log.Information($"Starting ffmpeg with args {ffmpeg.Arguments}", "Audio Create");
            return Process.Start(ffmpeg);
        }
    }
}