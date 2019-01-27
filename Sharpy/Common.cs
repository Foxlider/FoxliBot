using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sharpy.Helpers;
using System.Collections.Generic;
using System.Threading;
using Sharpy.Services;
using System.IO;
using Sharpy.Services.YouTube;

namespace Sharpy.Modules
{
    // Create a module with no prefix
    [Name("Common")]
    [Summary("Common commands for Sharpy")]
    public class Common : ModuleBase
    {
        private readonly CommandService _service;
        private readonly IConfigurationRoot _config;
        private readonly DiscordSocketClient _client;
        public Common(CommandService service)
        {
            _client = Sharpy.client;
            _service = service;
            _config = Sharpy.Configuration;
        }

        #region COMMANDS
        
        #region echo
        /// <summary>
        /// SAY - Echos a message
        /// </summary>
        /// <param name="echo"></param>
        /// <returns></returns>
        [Command("say"), Summary("Echos a message.")]
        [Alias("echo")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Say([Remainder, Summary("The text to echo")] string echo)
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync(echo);
        }

        #endregion echo

        #region userinfo
        /// <summary>
        /// USERINFO - Returns the information of a user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [Command("userinfo"), Summary("Returns info about the current user, or the user parameter, if one passed.")]
        [Alias("user", "whois")]
        public async Task UserInfo([Summary("The (optional) user to get info for")] IUser user = null)
        {
            var userInfo = user ?? Context.Client.CurrentUser;
            await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");
        }

        #endregion userinfo

        #region Help
        /// <summary>
        /// HELP - Displays some help
        /// </summary>
        /// <returns></returns>
        [Command("help")]
        public async Task HelpAsync()
        {
            string prefix = _config["prefix"];
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = "These are the commands you can use"
            };
            
            foreach (var module in _service.Modules)
            {
                string description = null;
                foreach (var cmd in module.Commands)
                {
                    var result = await cmd.CheckPreconditionsAsync(Context);
                    if (result.IsSuccess)
                        description += $"{prefix}{cmd.Aliases.First()}\n";
                }
                
                if (!string.IsNullOrWhiteSpace(description))
                {
                    builder.AddField(x =>
                    {
                        x.Name = module.Name;
                        x.Value = description;
                        x.IsInline = false;
                    });
                }
            }
            await ReplyAsync("", false, builder.Build());
        }

        /// <summary>
        /// HELP - Displays some help about a specific command
        /// </summary>
        /// <returns></returns>
        [Command("help")]
        public async Task HelpAsync(string command)
        {
            var result = _service.Search(Context, command);
            if (!result.IsSuccess)
            {
                await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                return;
            }
            string prefix = _config["prefix"];
            var builder = new EmbedBuilder()
            {
                Color = new Color(114, 137, 218),
                Description = $"Here are some commands like **{command}**"
            };

            foreach (var match in result.Commands)
            {
                var cmd = match.Command;
                builder.AddField(x =>
                {
                    x.Name = $"({string.Join("|", cmd.Aliases)})";
                    x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" + 
                              $"Summary: {cmd.Summary}";
                    x.IsInline = false;
                });
            }
            await ReplyAsync("", false, builder.Build());
        }

        #endregion Help

        #region version

        [Command("version"), Summary("Check the bot's version")]
        [Alias("v")]
        public async Task Version()
        {
            await Context.Message.DeleteAsync();
            await ReplyAsync($"Hello {Context.User.Mention} ! I am {_client.CurrentUser.Username} v{Sharpy.GetVersion()}.");
        }

        #endregion version

        #region choose

        [Command("choose"), Summary("If you want a robot to choose for you")]
        public async Task Choose([Remainder]string cString)
        {
            string[] choices = cString.Split().ToArray();
            Random rnd = new Random();
            string answer = "";
            string chosenOne = choices[rnd.Next(choices.Length)];
            if (choices[0].StartsWith("<@") && choices[0].EndsWith(">"))
            { 
                answer = chosenOne;
            }
            else
            {
                foreach (string word in choices)
                {
                    if (word == chosenOne)
                        answer += $" **{word}**";
                    else
                        answer += $" {word}";
                }
            }
            //if(choices[0].StartsWith("<@") && choices[0])
            //await ReplyAsync(choices[rnd.Next(choices.Length)]);
            await ReplyAsync(answer);
            await Context.Message.DeleteAsync();
        } 

        #endregion choose

        #region roll

        [Command("roll"), Summary("Rolls a dice in NdN format")]
        [Alias("r")]
        public async Task Roll(string dice)
        {
            try
            {
                var result = dice
                    .Split('d')
                    .Select(input =>
                    {
                        int? output = null;
                        if (int.TryParse(input, out var parsed))
                        {
                            output = parsed;
                        }
                        return output;
                    })
                    .Where(x => x != null)
                    .Select(x => x.Value)
                    .ToArray();
                string msg = $"{Context.User.Mention} rolled {result[0]}d{result[1]}";
                var range = Enumerable.Range(0, result[0]);
                int[] dices = new int[result[0]];
                Random rnd = new Random();
                foreach (var r in range)
                { dices[r] = rnd.Next(1, result[1]); }
                msg += "\n [ **";
                msg += string.Join("** | **", dices);
                msg += "** ]";
                await ReplyAsync(msg);
            }
            catch
            { await ReplyAsync("C'est con mais j'ai pas compris..."); }
            finally
            { await Context.Message.DeleteAsync(); }
        }

        #endregion roll

        #region status

        [Command("status")]
        public async Task Status(string stat = "")
        {
            if ( stat == null ||stat == "")
                await _client.SetGameAsync($"Ready to meet {Assembly.GetExecutingAssembly().GetName().Name} v{Sharpy.GetVersion()} ?");
            else
                await _client.SetGameAsync(stat);
        }

        #endregion status

        #endregion COMMANDS


        #region HELPERS

        private Process CreateStream(string path)
        {
            var ffmpeg = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i {path} -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            return Process.Start(ffmpeg);
        }

        private async Task SendAsync(IAudioClient client, string path)
        {
            // Create FFmpeg using the previous example
            var ffmpeg = CreateStream(path);
            var output = ffmpeg.StandardOutput.BaseStream;
            var discord = client.CreatePCMStream(AudioApplication.Mixed);
            await output.CopyToAsync(discord);
            await discord.FlushAsync();
        }
        #endregion HELPERS

    }


    [Name("Music")]
    [Summary("Audio commands for Sharpy")]
    public class Audio : ModuleBase
    {
        //private readonly CommandService _service;
        //private readonly IConfigurationRoot _config;
        //private readonly DiscordSocketClient _client;
        //private IVoiceChannel _voiceChannel;
        //private TaskCompletionSource<bool> _tcs;
        //private CancellationTokenSource _disposeToken;
        //private IAudioClient _audio;

        ///// <summary>
        ///// Tuple(FilePath, Video Name, Duration, Requested by)
        ///// </summary>
        //private Queue<Tuple<string, string, string, string>> _queue;

        //private bool Pause
        //{
        //    get => _internalPause;
        //    set
        //    {
        //        new Thread(() => _tcs.TrySetResult(value)).Start();
        //        _internalPause = value;
        //    }
        //}
        //private bool _internalPause;
        //private bool Skip
        //{
        //    get
        //    {
        //        bool ret = _internalSkip;
        //        _internalSkip = false;
        //        return ret;
        //    }
        //    set => _internalSkip = value;
        //}
        //private bool _internalSkip;

        //public bool IsDisposed;

        //public Audio(CommandService service)
        //{
        //    _queue = new Queue<Tuple<string, string, string, string>>();
        //    _client = Sharpy.client;
        //    _service = service;
        //    _config = Sharpy.Configuration;
        //    _tcs = new TaskCompletionSource<bool>();
        //    _disposeToken = new CancellationTokenSource();
        //}

        //[Command("add"), Summary("")]
        //[Alias("a")]
        //public async Task Add(string link)
        //{
        //    using (Context.Channel.EnterTypingState())
        //    {

        //        //Test for valid URL
        //        bool result = Uri.TryCreate(link, UriKind.Absolute, out Uri uriResult)
        //                  && (uriResult.Scheme == "http" || uriResult.Scheme == "https");

        //        //Answer
        //        if (result)
        //        {
        //            try
        //            {
        //                Console.WriteLine("Downloading Video...", ConsoleColor.Magenta);

        //                Tuple<string, string> info = await DownloadHelper.GetInfo(link);
        //                //await ReplyAsync($"{Context.User.Mention} requested \"{info.Item1}\" ({info.Item2})! Downloading now...");

        //                //Download
        //                string file = await DownloadHelper.Download(link);
        //                var vidInfo = new Tuple<string, string, string, string>(file, info.Item1, info.Item2, Context.User.ToString());

        //                _queue.Enqueue(vidInfo);
        //                Pause = false;
        //                Console.WriteLine($"Song added to playlist! ({vidInfo.Item2} ({vidInfo.Item3}))!", ConsoleColor.Magenta);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"Could not download Song! {ex.Message}", ConsoleColor.Red);
        //                await ReplyAsync($"Sorry {Context.User.Mention}, unfortunately I can't play that Song!");
        //            }
        //        }
        //        else
        //        {
        //            await ReplyAsync($"Sorry {Context.User.Mention}, but that was not a valid URL!");
        //        }
        //    }
        //}
        //[Command("addplaylist"), Summary("")]
        //[Alias("ap")]
        //public async Task AddPlayList(string link)
        //{
        //    using (Context.Channel.EnterTypingState())
        //    {

        //        //Test for valid URL
        //        bool result = Uri.TryCreate(link, UriKind.Absolute, out Uri uriResult)
        //                      && (uriResult.Scheme == "http" || uriResult.Scheme == "https");

        //        //Answer
        //        if (result)
        //        {
        //            try
        //            {
        //                Console.WriteLine("Downloading Playlist...", ConsoleColor.Magenta);

        //                Tuple<string, string> info = await DownloadHelper.GetInfo(link);
        //                await ReplyAsync($"{Context.User.Mention} requested Playlist \"{info.Item1}\" ({info.Item2})! Downloading now...");

        //                //Download
        //                string file = await DownloadHelper.DownloadPlaylist(link);
        //                var vidInfo = new Tuple<string, string, string, string>(file, info.Item1, info.Item2, Context.User.ToString());

        //                _queue.Enqueue(vidInfo);
        //                Pause = false;
        //                Console.WriteLine($"Playlist added to playlist! (\"{vidInfo.Item2}\" ({vidInfo.Item2}))!", ConsoleColor.Magenta);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine($"Could not download Playlist! {ex.Message}", ConsoleColor.Red);
        //                await ReplyAsync($"Sorry {Context.User.Mention}, unfortunately I can't play that Playlist!");
        //            }
        //        }
        //        else
        //        { await ReplyAsync($"Sorry {Context.User.Mention}, but that was not a valid URL!"); }
        //    }
        //}

        //[Command("pause"), Summary("")]
        //public async Task PauseCmd()
        //{
        //    Pause = true;
        //    Console.WriteLine("Playback paused!", ConsoleColor.Magenta);
        //    await ReplyAsync($"{Context.User.Mention} paused playback!");
        //}

        //[Command("resume"), Summary("")]
        //public async Task ResumeCmd()
        //{
        //    Pause = false;
        //    Console.WriteLine("Playback continued!", ConsoleColor.Magenta);
        //    await ReplyAsync($"{Context.User.Mention} resumed playback!");
        //}

        //[Command("clear"), Summary("")]
        //public async Task ClearCmd()
        //{
        //    Pause = true;
        //    _queue.Clear();
        //    Console.WriteLine("Playlist cleared!", ConsoleColor.Magenta);
        //    await ReplyAsync($"{Context.User.Mention} cleared the Playlist!");
        //}

        //[Command("summon"), Summary("")]
        //public async Task Summon()
        //{
        //    _audio?.Dispose();
        //    _voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
        //    if (_voiceChannel == null)
        //    {
        //        Console.WriteLine("Error joining Voice Channel!", ConsoleColor.Red);
        //        await ReplyAsync($"I can't connect to your Voice Channel.");
        //    }
        //    else
        //    {
        //        Console.WriteLine($"Joined Voice Channel \"{_voiceChannel.Name}\"", ConsoleColor.Magenta);
        //        _audio = await _voiceChannel.ConnectAsync();
        //    }
        //}

        //[Command("skip"), Summary("")]
        //public async Task SkipCmd()
        //{
        //    Console.WriteLine("Song Skipped!", ConsoleColor.Magenta);
        //    await ReplyAsync($"{Context.User.Mention} skipped **{_queue.Peek().Item2}**!");
        //    //Skip current Song
        //    Skip = true;
        //    Pause = false;
        //}

        //[Command("queue"), Summary("")]
        //public async Task Queue()
        //{
        //    EmbedBuilder builder = new EmbedBuilder()
        //    {
        //        Author = new EmbedAuthorBuilder { Name = "Music Bot Song Queue" },
        //        Footer = new EmbedFooterBuilder() { Text = "(I don't actually sing)" },
        //        Color = Pause ? new Color(244, 67, 54) /*Red*/ : new Color(00, 99, 33) /*Green*/
        //    };
        //    //builder.ThumbnailUrl = "some cool url";
        //    builder.Url = "http://github.com/Foxlider";

        //    if (_queue.Count == 0)
        //    { await ReplyAsync("Sorry, Song Queue is empty! Add some songs with the `!add [url]` command!"); }
        //    else
        //    {
        //        foreach (Tuple<string, string, string, string> song in _queue)
        //        { builder.AddField($"{song.Item2} ({song.Item3})", $"by {song.Item4}"); }
        //        await ReplyAsync("", embed: builder.Build());
        //    }
        //}

        //public async void MusicPlay()
        //{
        //    bool next = false;

        //    while (true)
        //    {
        //        bool pause = false;
        //        //Next song if current is over
        //        if (!next)
        //        {
        //            pause = await _tcs.Task;
        //            _tcs = new TaskCompletionSource<bool>();
        //        }
        //        else
        //        {
        //            next = false;
        //        }

        //        try
        //        {
        //            if (_queue.Count == 0)
        //            {
        //                await _client.SetGameAsync("Nothing :/");
        //                Console.WriteLine("Playlist ended.", ConsoleColor.Magenta);
        //            }
        //            else
        //            {
        //                if (!pause)
        //                {
        //                    //Get Song
        //                    var song = _queue.Peek();
        //                    //Update "Playing .."
        //                    await _client.SetGameAsync(song.Item2, song.Item1);
        //                    Console.WriteLine($"Now playing: {song.Item2} ({song.Item3})", ConsoleColor.Magenta);
        //                    await ReplyAsync($"Now playing: **{song.Item2}** ({song.Item3})");

        //                    //Send audio (Long Async blocking, Read/Write stream)
        //                    await AudioPlayer.SendAudio(song.Item1, _audio, _tcs, _disposeToken, Skip, Pause);

        //                    try
        //                    { File.Delete(song.Item1); }
        //                    catch
        //                    {
        //                        // ignored
        //                    }
        //                    finally
        //                    {
        //                        //Finally remove song from playlist
        //                        _queue.Dequeue();
        //                    }
        //                    next = true;
        //                }
        //            }
        //        }
        //        catch
        //        {
        //            //audio can't be played
        //        }
        //    }
        //}

        public YouTubeDownloadService YoutubeDownloadService { get; set; }

        public AudioService SongService { get; set; }

        [Alias("sq", "request", "play")]
        [Command("songrequest", RunMode = RunMode.Async)]
        [Summary("Requests a song to be played")]
        public async Task Request([Remainder, Summary("URL of the video to play")] string url)
        {
            await Speedrun(url, 48);
        }

        [Alias("test")]
        [Command("soundtest", RunMode = RunMode.Async)]
        [Summary("Performs a sound test")]
        public async Task SoundTest()
        {
            await Request("https://www.youtube.com/watch?v=i1GOn7EIbLg");
        }

        [Command("speedrun", RunMode = RunMode.Async)]
        [Summary("Performs a sound test")]
        public async Task Speedrun(string url, int speedModifier)
        {
            try
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    await ReplyAsync($"{Context.User.Mention} please provide a valid song URL");
                    return;
                }

                var downloadAnnouncement = await ReplyAsync($"{Context.User.Mention} attempting to download {url}");
                var video = await YoutubeDownloadService.DownloadVideo(url);
                await downloadAnnouncement.DeleteAsync();
                await Context.Message.DeleteAsync();

                if (video == null)
                {
                    await ReplyAsync($"{Context.User.Mention} unable to queue song, make sure its is a valid supported URL or contact a server admin.");
                    return;
                }

                video.Requester = Context.User.Mention;
                video.Speed = speedModifier;

                await ReplyAsync($"{Context.User.Mention} queued **{video.Title}** | `{TimeSpan.FromSeconds(video.Duration)}` | {url}");
                var _voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
                if (_voiceChannel == null)
                {
                    Console.WriteLine("Error joining Voice Channel!", ConsoleColor.Red);
                    await ReplyAsync($"I can't connect to your Voice Channel.");
                }
                else
                {
                    SongService.SetVoiceChannel(_voiceChannel, Context.Message.Channel);
                    SongService.Queue(video);
                }
            }
            catch (Exception e)
            {
                Log.Information($"Error while processing song requet: {e}");
            }
        }

        [Command("stream", RunMode = RunMode.Async)]
        [Summary("Streams a livestream URL")]
        public async Task Stream(string url)
        {
            try
            {
                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    await ReplyAsync($"{Context.User.Mention} please provide a valid URL");
                    return;
                }

                var downloadAnnouncement = await ReplyAsync($"{Context.User.Mention} attempting to open {url}");
                var stream = await YoutubeDownloadService.GetLivestreamData(url);
                await downloadAnnouncement.DeleteAsync();

                if (stream == null)
                {
                    await ReplyAsync($"{Context.User.Mention} unable to open live stream, make sure its is a valid supported URL or contact a server admin.");
                    return;
                }

                stream.Requester = Context.User.Mention;
                stream.Url = url;

                Log.Information($"Attempting to stream {stream}");

                await ReplyAsync($"{Context.User.Mention} queued **{stream.Title}** | `{stream.DurationString}` | {url}");

                SongService.Queue(stream);
            }
            catch (Exception e)
            {
                Log.Information($"Error while processing song requet: {e}");
            }
        }

        [Command("clear")]
        [Summary("Clears all songs in queue")]
        public async Task ClearQueue()
        {
            SongService.Clear();
            await ReplyAsync("Queue cleared");
        }

        [Alias("next", "nextsong")]
        [Command("skip")]
        [Summary("Skips current song")]
        public async Task SkipSong()
        {
            SongService.Next();
            await ReplyAsync("Skipped song");
        }

        [Alias("np", "currentsong", "songname", "song")]
        [Command("nowplaying")]
        [Summary("Prints current playing song")]
        public async Task NowPlaying()
        {
            if (SongService.NowPlaying == null)
            {
                await ReplyAsync($"{Context.User.Mention} current queue is empty");
            }
            else
            {
                await ReplyAsync($"{Context.User.Mention} now playing `{SongService.NowPlaying.Title}` requested by {SongService.NowPlaying.Requester}");
            }
        }
    }

}
