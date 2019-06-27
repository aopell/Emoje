using Discord;
using DiscordHackWeek2019.Commands;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Text;

namespace DiscordHackWeek2019.Helpers
{
    public static class ReactionMessageHelper
    {
        private static ObjectCache ReactionMessageCache = new MemoryCache("reactionMessages");

        public static void CreateReactionMessage(BotCommandContext context, IUserMessage message, Action<ReactionMessage, string> defaultAction, bool allowMultipleReactions = false, int timeout = 300000)
        {
            var reactionMessage = new ReactionMessage(context, message, defaultAction, allowMultipleReactions);
            ReactionMessageCache.Add(message.Id.ToString(), reactionMessage, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMilliseconds(timeout) });
        }

        public static void CreateReactionMessage(BotCommandContext context, IUserMessage message, Action<ReactionMessage> onPositiveResponse, Action<ReactionMessage> onNegativeResponse, bool allowMultipleReactions = false, int timeout = 300000)
        {
            CreateReactionMessage(context, message, new Dictionary<string, Action<ReactionMessage>>
            {
                ["✅"] = onPositiveResponse,
                ["❌"] = onNegativeResponse
            }, allowMultipleReactions, timeout);
        }

        public static void CreateReactionMessage(BotCommandContext context, IUserMessage message, Dictionary<string, Action<ReactionMessage>> actions, bool allowMultipleReactions = false, int timeout = 300000)
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

            var reactionMessage = ReactionMessageCache.Get(id.ToString()) as ReactionMessage;

            if (!reactionMessage.AllowMultipleReactions)
            {
                ReactionMessageCache.Remove(id.ToString());
            }

            return reactionMessage;
        }
    }

    public class ReactionMessage
    {
        public BotCommandContext Context { get; }
        public IUserMessage Message { get; }
        public bool AllowMultipleReactions { get; }
        public bool AcceptsAllReactions { get; }
        private Action<ReactionMessage, string> DefaultAction { get; }
        private Dictionary<string, Action<ReactionMessage>> Actions { get; }

        public ReactionMessage(BotCommandContext context, IUserMessage message, Action<ReactionMessage, string> defaultAction, bool allowMultipleReactions = false)
        {
            Context = context;
            Message = message;
            DefaultAction = defaultAction;
            AllowMultipleReactions = allowMultipleReactions;
            AcceptsAllReactions = true;
        }

        public ReactionMessage(BotCommandContext context, IUserMessage message, Dictionary<string, Action<ReactionMessage>> actions, bool allowMultipleReactions = false)
        {
            Context = context;
            Message = message;
            Actions = actions;
            AllowMultipleReactions = allowMultipleReactions;
            AcceptsAllReactions = false;
        }

        public void RunAction(IEmote reaction)
        {
            string text = reaction.ToString();
            if (AcceptsAllReactions)
            {
                DefaultAction(this, text);
            }
            else if (Actions.ContainsKey(text))
            {
                Actions[text](this);
            }
        }
    }
}
