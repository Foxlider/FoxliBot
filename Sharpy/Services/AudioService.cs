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
    //public class AudioService
    //{
    //    //private BufferBlock<IPlayable> _songQueue;
    //    public readonly ConcurrentDictionary<ulong, BufferBlock<IPlayable>> Queues = new ConcurrentDictionary<ulong, BufferBlock<IPlayable>>();
    //    public readonly ConcurrentDictionary<ulong, IVoiceChannel> ConnectedChannels = new ConcurrentDictionary<ulong, IVoiceChannel>();

    //    public AudioService()
    //    { //_songQueue = new BufferBlock<IPlayable>(); 
    //    }

    //    public AudioPlaybackService AudioPlaybackService { get; set; }

    //    public IPlayable NowPlaying { get; private set; }

    //    public void SetVoiceChannel(IVoiceChannel voiceChannel, IMessageChannel messageChannel)
    //    {
    //        if (!ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out IVoiceChannel voice))
    //        {
    //            ProcessQueue(voiceChannel, messageChannel);
    //        }
    //    }

    //    public async Task Quit(IGuild guild)
    //    {
    //        ConnectedChannels.TryGetValue(guild.Id, out IVoiceChannel voiceChannel);
    //        await voiceChannel.DisconnectAsync();
    //        ConnectedChannels.TryRemove(voiceChannel.Guild.Id, out IVoiceChannel voice);
    //    }

    //    public void Next()
    //    {
    //        AudioPlaybackService.StopCurrentOperation();
    //    }

    //    public IList<IPlayable> Clear(IGuild guild)
    //    {
    //        try
    //        {
    //            Queues.TryGetValue(guild.Id, out BufferBlock<IPlayable> songQueue);
    //            songQueue.TryReceiveAll(out var skippedSongs);
    //            Log.Information($"Skipped {skippedSongs.Count} songs");
    //            return skippedSongs;
    //        }
    //        catch
    //        { return null; }
    //    }

    //    public void Queue(IGuild guild, IPlayable video)
    //    {
    //        Queues.TryGetValue(guild.Id, out BufferBlock<IPlayable> songQueue);
    //        songQueue.Post(video);
    //    }

    //    public BufferBlock<IPlayable> SongList(IGuild guild)
    //    {
    //        Queues.TryGetValue(guild.Id, out BufferBlock<IPlayable> _songQueue);
    //        return _songQueue;
    //    }

    //    private async void ProcessQueue(IVoiceChannel voiceChannel, IMessageChannel messageChannel)
    //    {
    //        IAudioClient audioClient = null;
    //        Queues.TryGetValue(voiceChannel.Guild.Id, out BufferBlock<IPlayable> _songQueue);
    //        while (await _songQueue.OutputAvailableAsync())
    //        {
    //            Log.Information("Waiting for songs");
    //            NowPlaying = await _songQueue.ReceiveAsync();
    //            try
    //            {
    //                Log.Information("Connecting to voice channel");
    //                if (!ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out voiceChannel))
    //                {
    //                    audioClient = await voiceChannel.ConnectAsync();
    //                    if (ConnectedChannels.TryAdd(voiceChannel.Guild.Id, voiceChannel))
    //                    { Log.Information("Connected!"); }
    //                }
    //                await messageChannel?.SendMessageAsync($"Now playing **{NowPlaying.Title}** | `{NowPlaying.DurationString}` | requested by {NowPlaying.Requester}");
    //                await AudioPlaybackService.SendAsync(audioClient, NowPlaying.Uri, NowPlaying.Speed);
    //                NowPlaying.OnPostPlay();
    //            }
    //            catch (Exception e)
    //            { Log.Information($"Error while playing song: {e}"); }
    //        }
    //        await voiceChannel.DisconnectAsync();
    //        ConnectedChannels.TryRemove(voiceChannel.Guild.Id, out IVoiceChannel voice);
    //    }
    //}


    public class AudioService
    {
        //private BufferBlock<IPlayable> _songQueue;
        public readonly ConcurrentDictionary<ulong, List<IPlayable>> Queues = new ConcurrentDictionary<ulong, List<IPlayable>>();
        public readonly ConcurrentDictionary<ulong, IVoiceChannel> ConnectedChannels = new ConcurrentDictionary<ulong, IVoiceChannel>();

        public AudioService()
        { //_songQueue = new BufferBlock<IPlayable>(); 
        }

        public AudioPlaybackService AudioPlaybackService { get; set; }

        public IPlayable NowPlaying { get; private set; }

        public async Task Quit(IGuild guild)
        {
            ConnectedChannels.TryGetValue(guild.Id, out IVoiceChannel voiceChannel);
            await voiceChannel.DisconnectAsync();
            ConnectedChannels.TryRemove(voiceChannel.Guild.Id, out IVoiceChannel voice);
        }

        public void Next()
        {
            AudioPlaybackService.StopCurrentOperation();
        }

        public IList<IPlayable> Clear(IGuild guild)
        {
            try
            {
                Queues.TryGetValue(guild.Id, out List<IPlayable> songQueue);
                Log.Information($"Skipped {songQueue.Count} songs");
                songQueue.Clear();
                return songQueue;
            }
            catch
            { return null; }
        }

        public void Queue(IGuild guild, IPlayable video, IVoiceChannel voiceChannel, IMessageChannel messageChannel)
        {
            Queues.TryGetValue(guild.Id, out List<IPlayable> songQueue);
            if (songQueue == null)
                songQueue = new List<IPlayable>();
            songQueue.Add(video);
            //Queues.TryRemove(guild.Id, out var q);
            Queues.AddOrUpdate(guild.Id, songQueue, (k, v) => v);
            if (!ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out IVoiceChannel voice))
            {
                ProcessQueue(voiceChannel, messageChannel);
            }
        }

        public List<IPlayable> SongList(IGuild guild)
        {
            Queues.TryGetValue(guild.Id, out List<IPlayable> _songQueue);
            return _songQueue;
        }

        private async void ProcessQueue(IVoiceChannel voiceChannel, IMessageChannel messageChannel)
        {
            IAudioClient audioClient = null;
            Queues.TryGetValue(voiceChannel.Guild.Id, out List<IPlayable> _songQueue);
            while (_songQueue.Count > 0)
            {
                Log.Information("Waiting for songs");
                NowPlaying = _songQueue.FirstOrDefault();
                try
                {
                    Log.Information("Connecting to voice channel");
                    if (!ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out IVoiceChannel tempChannel))
                    {
                        audioClient = await voiceChannel.ConnectAsync();
                        if (ConnectedChannels.TryAdd(voiceChannel.Guild.Id, voiceChannel))
                        { Log.Information("Connected!"); }
                    }
                    await messageChannel?.SendMessageAsync($"Now playing **{NowPlaying.Title}** | `{NowPlaying.DurationString}` | requested by {NowPlaying.Requester}");
                    await AudioPlaybackService.SendAsync(audioClient, NowPlaying.Uri, NowPlaying.Speed);
                    var newQueue = _songQueue;
                    newQueue.Remove(NowPlaying);
                    Queues.TryUpdate(voiceChannel.Guild.Id, _songQueue, newQueue);
                    NowPlaying.OnPostPlay();
                }
                catch (Exception e)
                { Log.Information($"Error while playing song: {e}"); }
            }
            await voiceChannel.DisconnectAsync();
            ConnectedChannels.TryRemove(voiceChannel.Guild.Id, out IVoiceChannel voice);
        }
    }


}