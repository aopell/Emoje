using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordHackWeek2019.Commands;
using DiscordHackWeek2019.Config;
using System;
using System.Threading.Tasks;

namespace DiscordHackWeek2019
{
    public class DiscordBot
    {
        public static DiscordBot MainInstance = null;
        public DiscordSocketClient Client { get; private set; }
        public Secret Secret { get; private set; }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            MainInstance = new DiscordBot();
            MainInstance.MainAsync().GetAwaiter().GetResult();
        }

        private async Task MainAsync()
        {
            ConfigFileManager.LoadConfigFiles(this);
            Client = new DiscordSocketClient();

            Client.Log += Log;
            Client.Ready += Client_Ready;
            Client.ReactionAdded += Client_ReactionAdded;

            await Client.LoginAsync(TokenType.Bot, Secret.Token);
            await Client.StartAsync();

            var ch = new CommandHandler(
                Client,
                new CommandService(
                    new CommandServiceConfig()
                    {
                        CaseSensitiveCommands = false,
                        LogLevel = LogSeverity.Info
                    }
                )
            );

            await ch.InstallCommandsAsync();

            await Task.Delay(-1);
        }

        private Task Client_Ready()
        {
            return Task.CompletedTask;
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg.ToString());
            return Task.CompletedTask;
        }

        private Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            return Task.CompletedTask;
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            // Called on program close, should log this somewhere?
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Called on unhandled exception, should log this somewhere
        }
    }
}
