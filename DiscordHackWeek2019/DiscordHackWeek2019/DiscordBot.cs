using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordHackWeek2019.Commands;
using DiscordHackWeek2019.Config;
using DiscordHackWeek2019.Helpers;
using DiscordHackWeek2019.Models;
using LiteDB;
using System;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordHackWeek2019
{
    public class DiscordBot
    {
        public static DiscordBot MainInstance = null;
        public DiscordSocketClient Client { get; private set; }
        public Secret Secret { get; private set; }
        public Options Options { get; private set; }
        public LiteDatabase DataProvider { get; private set; }
        public EmojiHelper EmojiHelper { get; private set; }
        public Random Random { get; private set; }

        private static async Task Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            MainInstance = new DiscordBot();

            ConfigFileManager.LoadConfigFiles(MainInstance);
            MainInstance.Client = new DiscordSocketClient();
            MainInstance.Random = new Random();

            MainInstance.DataProvider = new LiteDatabase("data.db");
            MainInstance.EmojiHelper = new EmojiHelper("Config/emoji.tsv");

            MainInstance.Client.Log += MainInstance.Log;
            MainInstance.Client.Ready += MainInstance.Client_Ready;
            MainInstance.Client.ReactionAdded += MainInstance.Client_ReactionAdded;

            await MainInstance.Client.LoginAsync(TokenType.Bot, MainInstance.Secret.Token);
            await MainInstance.Client.StartAsync();

            var ch = new CommandHandler(
                MainInstance.Client,
                new CommandService(
                    new CommandServiceConfig()
                    {
                        CaseSensitiveCommands = false,
                        LogLevel = LogSeverity.Info
                    }
                ),
                MainInstance
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

        private async Task Client_ReactionAdded(Cacheable<IUserMessage, ulong> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var message = await cachedMessage.GetOrDownloadAsync();

            if (message.Author.Id == Client.CurrentUser.Id && reaction.UserId != Client.CurrentUser.Id)
            {
                var reactionMessage = ReactionMessageHelper.GetMessage(message.Id);
                if (reactionMessage != null && reaction.UserId == reactionMessage.Context.User.Id)
                {
                    try
                    {
                        await reactionMessage.RunAction(reaction.Emote);
                    }
                    catch (Exception ex)
                    {
                        await channel.SendMessageAsync(ex.ToString());
                    }

                    if (reactionMessage.AllowMultipleReactions)
                    {
                        await message.RemoveReactionAsync(reaction.Emote, reactionMessage.Context.User);
                    }
                    else
                    {
                        await message.RemoveAllReactionsAsync();
                        ReactionMessageHelper.Delete(reactionMessage);
                    }
                }
            }
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
