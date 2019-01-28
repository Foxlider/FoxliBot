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
using System.Threading.Tasks.Dataflow;

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
        public YouTubeDownloadService YoutubeDownloadService { get; set; }

        public AudioService SongService { get; set; }

        /// <summary>
        /// Function Play
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [Alias("sq", "request", "songrequest")]
        [Command("play", RunMode = RunMode.Async)]
        [Summary("Requests a song to be played")]
        public async Task Request([Remainder, Summary("URL of the video to play")] string url)
        { await Speedrun(url, 48); }

        [Alias("test")]
        [Command("soundtest", RunMode = RunMode.Async)]
        [Summary("Performs a sound test")]
        public async Task SoundTest()
        { await Request("https://www.youtube.com/watch?v=i1GOn7EIbLg"); }

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

                await ReplyAsync($"{Context.User.Mention} queued **{video.Title}** | `{TimeSpan.FromSeconds(video.Duration)}`");
                var _voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
                if (_voiceChannel == null)
                {
                    Console.WriteLine("Error joining Voice Channel!", ConsoleColor.Red);
                    await ReplyAsync($"I can't connect to your Voice Channel.");
                }
                else
                { SongService.Queue(Context.Guild, video, _voiceChannel, Context.Message.Channel); }
            }
            catch (Exception e)
            { Log.Information($"Error while processing song requet: {e}"); }
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

                await ReplyAsync($"{Context.User.Mention} queued **{stream.Title}** | `{stream.DurationString}`");
                var _voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
                if (_voiceChannel == null)
                {
                    Console.WriteLine("Error joining Voice Channel!", ConsoleColor.Red);
                    await ReplyAsync($"I can't connect to your Voice Channel.");
                }
                else
                { SongService.Queue(Context.Guild, stream, _voiceChannel, Context.Message.Channel); }
            }
            catch (Exception e)
            { Log.Information($"Error while processing song requet: {e}"); }
        }

        [Command("clear")]
        [Summary("Clears all songs in queue")]
        public async Task ClearQueue()
        {
            SongService.Clear(Context.Guild);
            await ReplyAsync("Queue cleared");
        }

        [Command("stop")]
        [Summary("Stops the playback and disconnect")]
        public async Task Stop()
        {
            SongService.Clear(Context.Guild);
            //ConcurrentDictionary<ulong, IAudioClient> channels = SongService.ConnectedChannels;
            //channels.TryGetValue(Context.Guild)
            await SongService.Quit(Context.Guild);
        }

        [Alias("next", "nextsong")]
        [Command("skip")]
        [Summary("Skips current song")]
        public async Task SkipSong()
        {
            SongService.Next();
            await ReplyAsync("Skipped song");
        }


        [Alias("songlist")]
        [Command("queue")]
        [Summary("Lists current songs")]
        public async Task SongList()
        {
            List<IPlayable> songlist = SongService.SongList(Context.Guild);
            if (songlist.Count == 0)
            { await ReplyAsync($"{Context.User.Mention} current queue is empty"); }
            else
            {
                string msg = "";
                var nowPlaying = songlist.FirstOrDefault();
                var qList = songlist;
                qList.Remove(nowPlaying);
                msg += $"** Now Playing : **\n  - *{nowPlaying.Title}* (`{nowPlaying.DurationString}`\n\n";
                if (qList.Count > 0)
                { msg += "** Songs in queue : **"; }
                foreach (var song in qList)
                { msg += $"\n  - *{song.Title}* (`{song.DurationString}`)"; }
                await ReplyAsync(msg);
            }
        }


        
        [Command("nowplaying")]
        [Alias("np", "currentsong", "songname", "song")]
        [Summary("Prints current playing song")]
        public async Task NowPlaying()
        {
            List<IPlayable> songlist = SongService.SongList(Context.Guild);
            if (songlist.Count == 0)
            { await ReplyAsync($"{Context.User.Mention} current queue is empty"); }
            else
            { await ReplyAsync($"{Context.User.Mention} now playing `{songlist.FirstOrDefault().Title}` requested by {songlist.FirstOrDefault().Requester}"); }
        }
    }

}
