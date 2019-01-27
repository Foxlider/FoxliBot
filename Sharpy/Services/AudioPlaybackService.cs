using Discord.Audio;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sharpy.Services
{
    public class AudioPlaybackService
    {
        private Process _currentProcess;

        public async Task SendAsync(IAudioClient client, string path, int speedModifier)
        {
            _currentProcess = CreateStream(path, speedModifier);
            var output = _currentProcess.StandardOutput.BaseStream;
            var discord = client.CreatePCMStream(AudioApplication.Music, 96 * 1024);
            await output.CopyToAsync(discord);
            await discord.FlushAsync();
            _currentProcess.WaitForExit();
            Console.WriteLine($"ffmpeg exited with code {_currentProcess.ExitCode}", ConsoleColor.Blue);
        }

        public void StopCurrentOperation()
        {
            _currentProcess.Kill();
            _currentProcess?.Dispose();
        }

        private static Process CreateStream(string path, int speedModifier)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                //Arguments = $"-i \"{path}\" -ac 2 -f s16le -filter:a \"volume=0.02\" -ar {speedModifier}000 pipe:1",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            Console.WriteLine($"Starting ffmpeg with args {ffmpeg.Arguments}", ConsoleColor.Blue);
            return Process.Start(ffmpeg);
        }
    }
}