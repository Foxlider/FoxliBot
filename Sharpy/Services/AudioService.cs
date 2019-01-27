using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Sharpy.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Sharpy.Services
{
    /**
     * AudioService
     * This handles the entire audio service functionality. 
     * This service used to perform all the tasks required by the module, but most have been separated
     * into helper functions.
     * 
     * AudioDownloader handles reading simple meta data from network links and local songs. If specified,
     * it'll download network songs into a default folder.
     * 
     * AudioPlayer handles the local and network streams then passes it into FFmpeg to output to the voice channel.
     * 
     * Right now the playlist is maintained in the service, but may be abstracted or moved into another
     * class in the future.
     */
    //public class AudioService
    //{

    //    private readonly DiscordSocketClient _discord;
    //    private readonly CommandService _commands;

    //    public AudioService(DiscordSocketClient discord, CommandService commands)
    //    {
    //        _discord = discord;
    //        _commands = commands;
    //    }

    //    //Looped Music Play
    //    //public static async void MusicPlay()
    //    //{
    //    //    bool next = false;

    //    //    while (true)
    //    //    {
    //    //        bool pause = false;
    //    //        //Next song if current is over
    //    //        if (!next)
    //    //        {
    //    //            pause = await _tcs.Task;
    //    //            _tcs = new TaskCompletionSource<bool>();
    //    //        }
    //    //        else
    //    //        {
    //    //            next = false;
    //    //        }

    //    //        try
    //    //        {
    //    //            if (_queue.Count == 0)
    //    //            {
    //    //                await _client.SetGameAsync("Nothing :/");
    //    //                Print("Playlist ended.", ConsoleColor.Magenta);
    //    //            }
    //    //            else
    //    //            {
    //    //                if (!pause)
    //    //                {
    //    //                    //Get Song
    //    //                    var song = _queue.Peek();
    //    //                    //Update "Playing .."
    //    //                    await _client.SetGameAsync(song.Item2, song.Item1);
    //    //                    Print($"Now playing: {song.Item2} ({song.Item3})", ConsoleColor.Magenta);
    //    //                    await SendMessage($"Now playing: **{song.Item2}** ({song.Item3})");

    //    //                    //Send audio (Long Async blocking, Read/Write stream)
    //    //                    await SendAudio(song.Item1);

    //    //                    try
    //    //                    {
    //    //                        File.Delete(song.Item1);
    //    //                    }
    //    //                    catch
    //    //                    {
    //    //                        // ignored
    //    //                    }
    //    //                    finally
    //    //                    {
    //    //                        //Finally remove song from playlist
    //    //                        _queue.Dequeue();
    //    //                    }
    //    //                    next = true;
    //    //                }
    //    //            }
    //    //        }
    //    //        catch
    //    //        {
    //    //            //audio can't be played
    //    //        }
    //    //    }
    //    //}
    //}

    public class AudioService
    {
        private IVoiceChannel _voiceChannel;
        private IMessageChannel _messageChannel;
        private BufferBlock<IPlayable> _songQueue;
        private readonly ConcurrentDictionary<ulong, IAudioClient> ConnectedChannels = new ConcurrentDictionary<ulong, IAudioClient>();

        public AudioService()
        {
            _songQueue = new BufferBlock<IPlayable>();
        }

        public AudioPlaybackService AudioPlaybackService { get; set; }

        public IPlayable NowPlaying { get; private set; }

        public void SetVoiceChannel(IVoiceChannel voiceChannel, IMessageChannel messageChannel)
        {
            this._voiceChannel = voiceChannel;
            this._messageChannel = messageChannel;
            ProcessQueue();
        }

        public void SetMessageChannel(IMessageChannel messageChannel)
        {
            this._messageChannel = messageChannel;
        }

        public void Next()
        {
            AudioPlaybackService.StopCurrentOperation();
        }

        public IList<IPlayable> Clear()
        {
            _songQueue.TryReceiveAll(out var skippedSongs);

            Console.WriteLine($"Skipped {skippedSongs.Count} songs");

            return skippedSongs;
        }

        public void Queue(IPlayable video)
        {
            _songQueue.Post(video);
        }

        private async void ProcessQueue()
        {
            IAudioClient audioClient;
            while (await _songQueue.OutputAvailableAsync())
            {
                Log.Information("Waiting for songs");
                NowPlaying = await _songQueue.ReceiveAsync();
                try
                {
                    
                    Log.Information("Connecting to voice channel");
                    if (!ConnectedChannels.TryGetValue(_voiceChannel.Guild.Id, out audioClient))
                    {
                        audioClient = await _voiceChannel.ConnectAsync();
                        if (ConnectedChannels.TryAdd(_voiceChannel.Guild.Id, audioClient))
                        { Log.Information("Connected!"); }
                    }
                    await _messageChannel?.SendMessageAsync($"Now playing **{NowPlaying.Title}** | `{NowPlaying.DurationString}` | requested by {NowPlaying.Requester}");
                    await AudioPlaybackService.SendAsync(audioClient, NowPlaying.Uri, NowPlaying.Speed);
                    NowPlaying.OnPostPlay();
                }
                catch (Exception e)
                {
                    Log.Information($"Error while playing song: {e}");
                }
            }
            await _voiceChannel.DisconnectAsync();
            ConnectedChannels.TryRemove(_voiceChannel.Guild.Id, out audioClient);
        }
    }


}