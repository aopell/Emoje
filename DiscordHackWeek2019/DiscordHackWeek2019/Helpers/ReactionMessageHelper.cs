using Discord;
using DiscordHackWeek2019.Commands;
using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using ReactionAction = System.Func<DiscordHackWeek2019.Helpers.ReactionMessage, System.Threading.Tasks.Task>;
using CustomReactionAction = System.Func<DiscordHackWeek2019.Helpers.ReactionMessage, string, System.Threading.Tasks.Task>;
using PageAction = System.Func<DiscordHackWeek2019.Helpers.PaginatedMessage, System.Threading.Tasks.Task<(string, Discord.Embed)>>;

namespace DiscordHackWeek2019.Helpers
{
    public static class ReactionMessageHelper
    {
        private static ObjectCache ReactionMessageCache = new MemoryCache("reactionMessages");

        public static void CreatePaginatedMessage(BotCommandContext context, IUserMessage message, int pageCount, int initialPage, PageAction action, int timeout = 300000, Action onTimeout = null)
        {
            message.AddReactionsAsync(new[] { new Emoji(PaginatedMessage.FirstPage), new Emoji(PaginatedMessage.PreviousPage), new Emoji(PaginatedMessage.NextPage), new Emoji(PaginatedMessage.LastPage) });

            var paginatedMessage = new PaginatedMessage(context, message, pageCount, initialPage, action);
            ReactionMessageCache.Add(message.Id.ToString(), paginatedMessage, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMilliseconds(timeout), RemovedCallback = onTimeout == null ? null : (CacheEntryRemovedCallback)(_ => onTimeout()) });
        }

        public static void CreateCustomReactionMessage(BotCommandContext context, IUserMessage message, CustomReactionAction defaultAction, bool allowMultipleReactions = false, int timeout = 300000, Action onTimeout = null)
        {
            var reactionMessage = new ReactionMessage(context, message, defaultAction, allowMultipleReactions);
            ReactionMessageCache.Add(message.Id.ToString(), reactionMessage, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMilliseconds(timeout), RemovedCallback = onTimeout == null ? null : (CacheEntryRemovedCallback)(_ => onTimeout()) });
        }

        public static void CreateConfirmReactionMessage(BotCommandContext context, IUserMessage message, ReactionAction onPositiveResponse, ReactionAction onNegativeResponse, bool allowMultipleReactions = false, int timeout = 300000, Action onTimeout = null)
        {
            CreateReactionMessage(context, message, new Dictionary<string, ReactionAction>
            {
                ["✅"] = onPositiveResponse,
                ["❌"] = onNegativeResponse
            }, allowMultipleReactions, timeout, onTimeout);
        }

        public static void CreateReactionMessage(BotCommandContext context, IUserMessage message, Dictionary<string, ReactionAction> actions, bool allowMultipleReactions = false, int timeout = 300000, Action onTimeout = null)
        {
            foreach (string e in actions.Keys)
            {
                message.AddReactionAsync(Emote.TryParse(e, out Emote emote) ? emote : (IEmote)new Emoji(e));
            }

            var reactionMessage = new ReactionMessage(context, message, actions, allowMultipleReactions);
            ReactionMessageCache.Add(message.Id.ToString(), reactionMessage, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMilliseconds(timeout), RemovedCallback = onTimeout == null ? null : (CacheEntryRemovedCallback)(_ => onTimeout()) });
        }

        public static ReactionMessage GetReactionMessageById(ulong id)
        {
            if (!ReactionMessageCache.Contains(id.ToString())) return null;

            return ReactionMessageCache.Get(id.ToString()) as ReactionMessage;
        }

        public static void DeleteReactionMessage(ReactionMessage reactionMessage)
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
        protected CustomReactionAction DefaultAction { get; }
        protected Dictionary<string, ReactionAction> Actions { get; }

        public ReactionMessage(BotCommandContext context, IUserMessage message, CustomReactionAction defaultAction, bool allowMultipleReactions = false)
        {
            Context = context;
            Message = message;
            DefaultAction = defaultAction;
            AllowMultipleReactions = allowMultipleReactions;
            AcceptsAllReactions = true;
        }

        public ReactionMessage(BotCommandContext context, IUserMessage message, Dictionary<string, ReactionAction> actions, bool allowMultipleReactions = false)
        {
            Context = context;
            Message = message;
            Actions = actions;
            AllowMultipleReactions = allowMultipleReactions;
            AcceptsAllReactions = false;
        }

        public virtual async Task RunAction(IEmote reaction)
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

    public class PaginatedMessage : ReactionMessage
    {
        public const string FirstPage = "⏪";
        public const string LastPage = "⏩";
        public const string PreviousPage = "◀";
        public const string NextPage = "▶";

        public int PageCount { get; }
        public int CurrentPage { get; private set; }
        public PageAction OnChage { get; }

        public PaginatedMessage(BotCommandContext context, IUserMessage message, int count, int initial, PageAction action) : base(context, message, new Dictionary<string, ReactionAction>(), true)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            if (initial < 1 || initial > count) throw new ArgumentOutOfRangeException(nameof(initial));

            PageCount = count;
            CurrentPage = initial;
            OnChage = action;
        }

        public override async Task RunAction(IEmote reaction)
        {
            switch (reaction.ToString())
            {
                case FirstPage:
                    if (CurrentPage == 1) return;
                    CurrentPage = 1;
                    break;
                case LastPage:
                    if (CurrentPage == PageCount) return;
                    CurrentPage = PageCount;
                    break;
                case PreviousPage:
                    if (CurrentPage == 1) return;
                    CurrentPage--;
                    break;
                case NextPage:
                    if (CurrentPage == PageCount) return;
                    CurrentPage++;
                    break;
            }

            (string text, Embed embed) = await OnChage(this);

            await Message.ModifyAsync(m =>
            {
                m.Content = text;
                m.Embed = embed;
            });
        }
    }
}
