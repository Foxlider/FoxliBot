using Discord.Audio;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sharpy.Helpers
{
    /**
     * AudioPlayer
     * Helper class to handle a single audio playback.
     */
    class AudioPlayer
    {
        public static Process GetFfmpeg(string path)
        {
            ProcessStartInfo ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-xerror -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                //UseShellExecute = false,    //TODO: true or false?
                RedirectStandardOutput = true
            };
            return Process.Start(ffmpeg);
        }

        //Get ffplay Audio Procecss
        public static Process GetFfplay(string path)
        {
            ProcessStartInfo ffplay = new ProcessStartInfo
            {
                FileName = "ffplay",
                Arguments = $"-i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1 -autoexit",
                //UseShellExecute = false,    //TODO: true or false?
                RedirectStandardOutput = true
            };

            return new Process { StartInfo = ffplay };
        }

        //Send Audio with ffmpeg
        public static async Task SendAudio(string path, IAudioClient _audio, TaskCompletionSource<bool> _tcs, CancellationTokenSource _disposeToken, bool Skip, bool Pause)
        {
            //FFmpeg.exe
            Process ffmpeg = GetFfmpeg(path);
            //Read FFmpeg output
            using (Stream output = ffmpeg.StandardOutput.BaseStream)
            {
                using (AudioOutStream discord = _audio.CreatePCMStream(AudioApplication.Mixed, 1920))
                {

                    //Adjust?
                    int bufferSize = 1024;
                    int bytesSent = 0;
                    bool fail = false;
                    bool exit = false;
                    byte[] buffer = new byte[bufferSize];

                    while (
                        !Skip &&                                    // If Skip is set to true, stop sending and set back to false (with getter)
                        !fail &&                                    // After a failed attempt, stop sending
                        !_disposeToken.IsCancellationRequested &&   // On Cancel/Dispose requested, stop sending
                        !exit                                       // Audio Playback has ended (No more data from FFmpeg.exe)
                            )
                    {
                        try
                        {
                            int read = await output.ReadAsync(buffer, 0, bufferSize, _disposeToken.Token);
                            if (read == 0)
                            {
                                //No more data available
                                exit = true;
                                break;
                            }

                            await discord.WriteAsync(buffer, 0, read, _disposeToken.Token);

                            if (Pause)
                            {
                                bool pauseAgain;

                                do
                                {
                                    pauseAgain = await _tcs.Task;
                                    _tcs = new TaskCompletionSource<bool>();
                                } while (pauseAgain);
                            }

                            bytesSent += read;
                        }
                        catch (TaskCanceledException)
                        {
                            exit = true;
                        }
                        catch
                        {
                            fail = true;
                            // could not send
                        }
                    }
                    await discord.FlushAsync();
                }
            }
        }
    }
}