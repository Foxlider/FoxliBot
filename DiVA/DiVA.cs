using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using DiVA.Helpers;
using DiVA.Services;
using DiVA.Services.YouTube;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DiVA
{
    class DiVA
    {
        private CommandService commands;
        public static DiscordSocketClient client;
        private IServiceProvider services;
        public static IConfigurationRoot Configuration;
        public static bool DEV_MODE = false;

        static void Main(string[] args) => RunAsync(args).GetAwaiter().GetResult();

        public static async Task RunAsync(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            var DiVA = new DiVA(args);
            await DiVA.RunAsync();
        }


        public DiVA(string[] args)
        {
            TryGenerateConfiguration();
            var builder = new ConfigurationBuilder()        // Create a new instance of the config builder
                .SetBasePath(AppContext.BaseDirectory)      // Specify the default location for the config file
                .AddJsonFile("config.json");        // Add this (json encoded) file to the configuration
            Configuration = builder.Build();                // Build the configuration
        }

        /// <summary>
        /// Main Thread
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync()
        {
            Log.Neutral(
                $"Booting up...\n" +
                $"____________\n" +
                $"{Assembly.GetExecutingAssembly().GetName().Name} " +
                $"v{GetVersion()}\n" +
                $"____________\n");

            //Clearing Song folder
            try
            {
                DirectoryInfo d = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "Songs"));
                foreach (FileInfo file in d.GetFiles())
                { file.Delete(); }
            }
            catch
            { }
            //Creating Websocket
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });
            client.Log += LogMessage;
            commands = new CommandService();

            IServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            services = serviceCollection.BuildServiceProvider();

            services.GetService<AudioService>().AudioPlaybackService = services.GetService<AudioPlaybackService>();



            //services = new ServiceCollection().BuildServiceProvider();             // Create a new instance of a service collection

            await InstallCommands();
            if (Configuration["tokens:discord"] == null || Configuration["tokens:discord"] == "")
            {
                Log.Error("Impossible to read Configuration.", "DiVA Login");
                Log.Neutral("Do you want to edit the Discord Token ? (Y/n)\n", "DiVA Login");
                var answer = Console.ReadKey();
                if (answer.Key == ConsoleKey.Enter || answer.Key == ConsoleKey.Y)
                { EditToken(); }
                else
                {
                    Log.Warning("Shutting Down...\nPress Enter to continue.", "DiVA Logout");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
            }
            await client.LoginAsync(TokenType.Bot, Configuration["tokens:discord"]);
            await client.StartAsync();

            client.Ready += () =>
            {
                Log.Neutral($"{client.CurrentUser.Username}#{client.CurrentUser.Discriminator} is connected !\n\n" +
                    $"__[ CONNECTED TO ]__\n", "DiVA Login");
                foreach (var guild in client.Guilds)
                {
                    Log.Neutral(
                        $"\t_______________\n" +
                        $"\t{guild.Name} \n" +
                        $"\tOwned by {guild.Owner.Nickname}#{guild.Owner.Discriminator}\n" +
                        $"\t{guild.MemberCount} members", "DiVA Login");
                }
                Log.Neutral("\t_______________", "DiVA Login");
                //ConsoleColor[] colors = (ConsoleColor[])ConsoleColor.GetValues(typeof(ConsoleColor));
                //foreach (var color in colors)
                //{
                //    Console.ForegroundColor = color;
                //    Console.WriteLine(" The foreground color is {0}.", color);
                //}
                Console.Title = $"{Assembly.GetExecutingAssembly().GetName().Name} v{GetVersion()}";
                SetDefaultStatus();
                return Task.CompletedTask;
            };

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }


        public async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            client.MessageReceived += HandleCommand;
            client.UserLeft += UserLeftGuildHandler;
            client.UserJoined += UserJoinedGuildHandler;
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        private void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(new YouTubeDownloadService());
            serviceCollection.AddSingleton(new AudioPlaybackService());
            serviceCollection.AddSingleton(new AudioService());
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            if (!(messageParam is SocketUserMessage message)) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            if (!(message.HasStringPrefix(Configuration["prefix"], ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;
            // Create a Command Context
            var context = new CommandContext(client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }

        private async Task UserJoinedGuildHandler(SocketGuildUser param)
        {
            Random _rnd = new Random();
            var channel = client.GetChannel(param.Guild.DefaultChannel.Id) as SocketTextChannel;
            await CommandHelper.SayHelloAsync(channel, client, param as IUser, _rnd);
        }

        private async Task UserLeftGuildHandler(SocketGuildUser param)
        {
            Random _rnd = new Random();
            var channel = client.GetChannel(param.Guild.DefaultChannel.Id) as SocketTextChannel;
            await channel.SendMessageAsync($"{param.Mention} left us... Say bye ! ");
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            try
            { client.LogoutAsync(); }
            catch { }
            finally
            { client.Dispose(); }
            Log.Warning("Shutting Down...", "DiVA Logout");
            Environment.Exit(0);
        }

        private Task LogMessage(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Debug:
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                default:
                    break;
            }
            //Console.WriteLine($"[{message.Severity} {message.Source}][{DateTime.Now.ToString()}] : {message.Message}");
            Log.Message(message.Severity, message.Message, message.Source);
            //Console.ResetColor();
            return Task.CompletedTask;
        }


        public static bool TryGenerateConfiguration()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (File.Exists(filePath)) return false;
            object config = new DiVAConfiguration();
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filePath, json);
            return true;
        }

        private void EditToken()
        {
            string url = "https://discordapp.com/developers/applications/538306821333712916/bots";
            try
            {
                Process.Start(url);
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }

            Log.Neutral("Please enter the bot's token below.", "DiVA Login");
            string answer = Console.ReadLine();
            Configuration["tokens:discord"] = answer;
            var filePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            object config = new DiVAConfiguration(Configuration["prefix"], new Tokens(Configuration["tokens:discord"]));
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.Delete(filePath);
            File.WriteAllText(filePath, json);
        }


        public void SetDefaultStatus()
        { client.SetGameAsync($"Discord Virtual Assistant or DiVA v{GetVersion()}"); }

        public static string GetVersion()
        {
            string rev = "b";
            if (DEV_MODE)
                rev = "a";
            return $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}{rev}";
        }

    }
}
