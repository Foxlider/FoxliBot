using Discord;
using Discord.Audio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sharpy.Services
{
    ///// <summary>
    ///// Audio service
    ///// </summary>
    //public class AudioService
    //{
    //    //private BufferBlock<IPlayable> _songQueue;
    //    /// <summary>
    //    /// List of Queues by server
    //    /// </summary>
    //    public readonly ConcurrentDictionary<ulong, List<IPlayable>> Queues = new ConcurrentDictionary<ulong, List<IPlayable>>();
    //    /// <summary>
    //    /// List of VoiceChannels by server
    //    /// </summary>
    //    public readonly ConcurrentDictionary<ulong, IVoiceChannel> ConnectedChannels = new ConcurrentDictionary<ulong, IVoiceChannel>();

    //    /// <summary>
    //    /// Service CTOR
    //    /// </summary>
    //    public AudioService()
    //    { //_songQueue = new BufferBlock<IPlayable>(); 
    //    }

    //    /// <summary>
    //    /// Playback service
    //    /// </summary>
    //    public AudioPlaybackService AudioPlaybackService { get; set; }

    //    /// <summary>
    //    /// NowPlaying var
    //    /// </summary>
    //    public IPlayable NowPlaying { get; private set; }

    //    /// <summary>
    //    /// Quit the voice channel
    //    /// </summary>
    //    /// <param name="guild"></param>
    //    /// <returns></returns>
    //    public async Task Quit(IGuild guild)
    //    {
    //        ConnectedChannels.TryGetValue(guild.Id, out IVoiceChannel voiceChannel);
    //        await voiceChannel.DisconnectAsync();
    //        ConnectedChannels.TryRemove(voiceChannel.Guild.Id, out IVoiceChannel voice);
    //    }

    //    /// <summary>
    //    /// Skips current song
    //    /// </summary>
    //    public void Next()
    //    {
    //        AudioPlaybackService.StopCurrentOperation();
    //    }

    //    /// <summary>
    //    /// Clear queue
    //    /// </summary>
    //    /// <param name="guild"></param>
    //    /// <returns></returns>
    //    public IList<IPlayable> Clear(IGuild guild)
    //    {
    //        try
    //        {
    //            Queues.TryGetValue(guild.Id, out List<IPlayable> songQueue);
    //            Log.Information($"Skipped {songQueue.Count} songs");
    //            songQueue.Clear();
    //            return songQueue;
    //        }
    //        catch
    //        { return null; }
    //    }

    //    /// <summary>
    //    /// Add a song to the queue
    //    /// </summary>
    //    /// <param name="guild"></param>
    //    /// <param name="video"></param>
    //    /// <param name="voiceChannel"></param>
    //    /// <param name="messageChannel"></param>
    //    public void Queue(IGuild guild, IPlayable video, IVoiceChannel voiceChannel, IMessageChannel messageChannel)
    //    {
    //        Queues.TryGetValue(guild.Id, out List<IPlayable> songQueue);
    //        if (songQueue == null)
    //            songQueue = new List<IPlayable>();
    //        songQueue.Add(video);
    //        //Queues.TryRemove(guild.Id, out var q);
    //        Queues.AddOrUpdate(guild.Id, songQueue, (k, v) => v);
    //        if (!ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out IVoiceChannel voice))
    //        {
    //            ProcessQueue(voiceChannel, messageChannel);
    //        }
    //    }

    //    /// <summary>
    //    /// Lists current songs
    //    /// </summary>
    //    /// <param name="guild"></param>
    //    /// <returns></returns>
    //    public List<IPlayable> SongList(IGuild guild)
    //    {
    //        Queues.TryGetValue(guild.Id, out List<IPlayable> _songQueue);
    //        return _songQueue;
    //    }

    //    private async void ProcessQueue(IVoiceChannel voiceChannel, IMessageChannel messageChannel)
    //    {
    //        IAudioClient audioClient = null;
    //        Queues.TryGetValue(voiceChannel.Guild.Id, out List<IPlayable> _songQueue);
    //        while (_songQueue.Count > 0)
    //        {
    //            Log.Information("Waiting for songs");
    //            NowPlaying = _songQueue.FirstOrDefault();
    //            try
    //            {
    //                Log.Information("Connecting to voice channel");
    //                if (!ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out IVoiceChannel tempChannel))
    //                {
    //                    audioClient = await voiceChannel.ConnectAsync();
    //                    if (ConnectedChannels.TryAdd(voiceChannel.Guild.Id, voiceChannel))
    //                    { Log.Information("Connected!"); }
    //                }
    //                await messageChannel?.SendMessageAsync($"Now playing **{NowPlaying.Title}** | `{NowPlaying.DurationString}` | requested by {NowPlaying.Requester}");
    //                await AudioPlaybackService.SendAsync(audioClient, NowPlaying.Uri, NowPlaying.Speed);
    //                var newQueue = _songQueue;
    //                newQueue.Remove(NowPlaying);
    //                Queues.TryUpdate(voiceChannel.Guild.Id, _songQueue, newQueue);
    //                NowPlaying.OnPostPlay();
    //            }
    //            catch (Exception e)
    //            { Log.Information($"Error while playing song: {e}"); }
    //        }
    //        await voiceChannel.DisconnectAsync();
    //        ConnectedChannels.TryRemove(voiceChannel.Guild.Id, out IVoiceChannel voice);
    //    }
    //}

    /// <summary>
    /// Audio service
    /// </summary>
    public class AudioService
    {

        /// <summary>
        /// List of VoiceChannels by server
        /// </summary>
        public readonly ConcurrentDictionary<ulong, VoiceConnexion> ConnectedChannels = new ConcurrentDictionary<ulong, VoiceConnexion>();

        /// <summary>
        /// Service CTOR
        /// </summary>
        public AudioService()
        { //_songQueue = new BufferBlock<IPlayable>(); 
        }

        /// <summary>
        /// Playback service
        /// </summary>
        public AudioPlaybackService AudioPlaybackService { get; set; }

        /// <summary>
        /// NowPlaying var
        /// </summary>
        public IPlayable NowPlaying { get; private set; }

        /// <summary>
        /// Quit the voice channel
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public async Task Quit(IGuild guild)
        {
            ConnectedChannels.TryGetValue(guild.Id, out VoiceConnexion voice);
            await voice.Channel.DisconnectAsync();
            ConnectedChannels.TryRemove(voice.Channel.Guild.Id, out VoiceConnexion tempVoice);
        }

        /// <summary>
        /// Skips current song
        /// </summary>
        public void Next()
        { AudioPlaybackService.StopCurrentOperation(); }

        /// <summary>
        /// Clear queue
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public IList<IPlayable> Clear(IGuild guild)
        {
            try
            {
                ConnectedChannels.TryGetValue(guild.Id, out VoiceConnexion voice);
                var songQueue = voice.Queue;
                Log.Information($"Skipped {songQueue.Count} songs");
                songQueue.Clear();
                return songQueue;
            }
            catch
            { return null; }
        }

        /// <summary>
        /// Add a song to the queue
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="video"></param>
        /// <param name="voiceChannel"></param>
        /// <param name="messageChannel"></param>
        public async void Queue(IPlayable video, IVoiceChannel voiceChannel, IMessageChannel messageChannel)
        {
            bool firstConnexion = false;
            if (!ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out VoiceConnexion tempsVoice))
            {
                Log.Information("Connecting to voice channel");
                VoiceConnexion connexion = new VoiceConnexion
                {
                    Channel = voiceChannel,
                    Queue = new List<IPlayable>(),
                    Client = await voiceChannel.ConnectAsync()
                };
                if (ConnectedChannels.TryAdd(voiceChannel.Guild.Id, connexion))
                { Log.Information("Connected!"); }
                firstConnexion = true;
                
            }
            ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out VoiceConnexion voice);

            voice.Queue.Add(video);

            if (firstConnexion)
            { ProcessQueue(voiceChannel, messageChannel); }
        }

        /// <summary>
        /// Lists current songs
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        public List<IPlayable> SongList(IGuild guild)
        {
            ConnectedChannels.TryGetValue(guild.Id, out VoiceConnexion voice);
            return voice.Queue;
        }

        /// <summary>
        /// Handled the Queue of a Voice Client
        /// </summary>
        /// <param name="voiceChannel"></param>
        /// <param name="messageChannel"></param>
        private async void ProcessQueue(IVoiceChannel voiceChannel, IMessageChannel messageChannel)
        {
            ConnectedChannels.TryGetValue(voiceChannel.Guild.Id, out VoiceConnexion voice);
            while (voice.Queue.Count > 0)
            {
                Log.Information("Waiting for songs");
                NowPlaying = voice.Queue.FirstOrDefault();
                try
                {
                    await messageChannel?.SendMessageAsync($"Now playing **{NowPlaying.Title}** | `{NowPlaying.DurationString}` | requested by {NowPlaying.Requester}");
                    await AudioPlaybackService.SendAsync(voice.Client, NowPlaying.Uri, NowPlaying.Speed);
                    voice.Queue.Remove(NowPlaying);
                    NowPlaying.OnPostPlay();
                }
                catch (Exception e)
                { Log.Information($"Error while playing song: {e}"); }
            }
            await voice.Channel.DisconnectAsync();
            ConnectedChannels.TryRemove(voiceChannel.Guild.Id, out VoiceConnexion tempVoice);
        }
    }


}