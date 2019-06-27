using Discord;
using DiscordHackWeek2019.Commands;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace DiscordHackWeek2019.Helpers
{
    public static class ReactionMessageHelper
    {
        private static ObjectCache ReactionMessageCache = new MemoryCache("reactionMessages");

        public static void CreateReactionMessage(BotCommandContext context, IUserMessage message, Func<ReactionMessage, string, Task> defaultAction, bool allowMultipleReactions = false, int timeout = 300000)
        {
            var reactionMessage = new ReactionMessage(context, message, defaultAction, allowMultipleReactions);
            ReactionMessageCache.Add(message.Id.ToString(), reactionMessage, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMilliseconds(timeout) });
        }

        public static void CreateReactionMessage(BotCommandContext context, IUserMessage message, Func<ReactionMessage, Task> onPositiveResponse, Func<ReactionMessage, Task> onNegativeResponse, bool allowMultipleReactions = false, int timeout = 300000)
        {
            CreateReactionMessage(context, message, new Dictionary<string, Func<ReactionMessage, Task>>
            {
                ["✅"] = onPositiveResponse,
                ["❌"] = onNegativeResponse
            }, allowMultipleReactions, timeout);
        }

        public static void CreateReactionMessage(BotCommandContext context, IUserMessage message, Dictionary<string, Func<ReactionMessage, Task>> actions, bool allowMultipleReactions = false, int timeout = 300000)
        {
            foreach (string e in actions.Keys)
            {
                message.AddReactionAsync(Emote.TryParse(e, out Emote emote) ? emote : (IEmote)new Emoji(e));
            }

            var reactionMessage = new ReactionMessage(context, message, actions, allowMultipleReactions);
            ReactionMessageCache.Add(message.Id.ToString(), reactionMessage, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMilliseconds(timeout) });
        }

        public static ReactionMessage GetMessage(ulong id)
        {
            if (!ReactionMessageCache.Contains(id.ToString())) return null;

            return ReactionMessageCache.Get(id.ToString()) as ReactionMessage;
        }

        public static void Delete(ReactionMessage reactionMessage)
        {
            ReactionMessageCache.Remove(reactionMessage.Message.ToString());
        }
    }

    public class ReactionMessage
    {
        public BotCommandContext Context { get; }
        public IUserMessage Message { get; }
        public bool AllowMultipleReactions { get; }
        public bool AcceptsAllReactions { get; }
        private Func<ReactionMessage, string, Task> DefaultAction { get; }
        private Dictionary<string, Func<ReactionMessage, Task>> Actions { get; }

        public ReactionMessage(BotCommandContext context, IUserMessage message, Func<ReactionMessage, string, Task> defaultAction, bool allowMultipleReactions = false)
        {
            Context = context;
            Message = message;
            DefaultAction = defaultAction;
            AllowMultipleReactions = allowMultipleReactions;
            AcceptsAllReactions = true;
        }

        public ReactionMessage(BotCommandContext context, IUserMessage message, Dictionary<string, Func<ReactionMessage, Task>> actions, bool allowMultipleReactions = false)
        {
            Context = context;
            Message = message;
            Actions = actions;
            AllowMultipleReactions = allowMultipleReactions;
            AcceptsAllReactions = false;
        }

        public async Task RunAction(IEmote reaction)
        {
            string text = reaction.ToString();
            if (AcceptsAllReactions)
            {
                await DefaultAction(this, text);
            }
            else if (Actions.ContainsKey(text))
            {
                await Actions[text](this);
            }
        }
    }
}
