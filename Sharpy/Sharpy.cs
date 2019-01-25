using System;
using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Newtonsoft.Json;
using Sharpy.Properties;
using Microsoft.Extensions.Configuration;

namespace Sharpy
{
    class Sharpy
    {
        private CommandService commands;
        private DiscordSocketClient client;
        private IServiceProvider services;
        //public IConfigurationRoot Configuration { get; }

        //static void Main(string[] args)
        //{
        //    Console.WriteLine($"Booting up...\n___________\n{}");
        //    => new Program().MainAsync().GetAwaiter().GetResult();
        //}

        //static void Main(string[] args) => new Sharpy().MainAsync().GetAwaiter().GetResult();

        static void Main(string[] args) => RunAsync(args).GetAwaiter().GetResult();

        public static async Task RunAsync(string[] args)
        {
            var sharpy = new Sharpy(args);
            await sharpy.RunAsync();
        }


        public Sharpy(string[] args)
        {
            TryGenerateConfiguration();
            var builder = new ConfigurationBuilder()        // Create a new instance of the config builder
                .SetBasePath(AppContext.BaseDirectory)      // Specify the default location for the config file
                .AddJsonFile("config.json");        // Add this (json encoded) file to the configuration
            //Configuration = builder.Build();                // Build the configuration
        }

        /// <summary>
        /// Main Thread
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync()
        {
            Console.WriteLine(
                $"Booting up...\n" +
                $"____________\n" +
                $"{Assembly.GetExecutingAssembly().GetName().Name} " +
                $"v{GetVersion()}\n" +
                $"____________\n");


            client = new DiscordSocketClient();
            commands = new CommandService();
            services = new ServiceCollection().BuildServiceProvider();             // Create a new instance of a service collection
            await InstallCommands();

            await client.LoginAsync(TokenType.Bot, Configuration["tokens:discord"]);
            await client.StartAsync();

            client.Ready += () =>
            {
                Console.WriteLine($"{client.CurrentUser.Username}#{client.CurrentUser.Discriminator} is connected !\n\n" +
                    $"__[ CONNECTED TO ]__\n");
                foreach (var guild in client.Guilds)
                {
                    Console.WriteLine(
                        $"\t_______________\n" +
                        $"\t{guild.Name} \n" +
                        $"\tOwned by {guild.Owner.Nickname}#{guild.Owner.Discriminator}\n" +
                        $"\t{guild.MemberCount} members");
                }
                Console.WriteLine("\t\t_______________");
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
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            if (!(messageParam is SocketUserMessage message)) return;
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            // Determine if the message is a command, based on if it starts with '!' or a mention prefix
            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;
            // Create a Command Context
            var context = new CommandContext(client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }


        public static bool TryGenerateConfiguration()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (File.Exists(filePath)) return false;

            var json = JsonConvert.SerializeObject(new SharpyConfiguration(), Formatting.Indented);
            File.WriteAllText(filePath, json);
            return true;
        }

        public void SetDefaultStatus()
        { client.SetGameAsync($"Ready to meet {Assembly.GetExecutingAssembly().GetName().Name} v{GetVersion()} ?"); }

        public static string GetVersion()
        {
            string rev = "b";
            if (Settings.Default.DEV_MODE)
                rev = "a";
            return $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}{rev}";
        }

    }
}
