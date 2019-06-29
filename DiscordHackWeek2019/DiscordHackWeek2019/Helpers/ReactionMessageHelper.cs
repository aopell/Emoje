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
using Discord.WebSocket;
using System.Linq;

namespace DiscordHackWeek2019.Helpers
{
    public static class ReactionMessageHelper
    {
        private static ObjectCache ReactionMessageCache = new MemoryCache("reactionMessages");

        public static void CreatePaginatedMessage(BotCommandContext context, IUserMessage message, int pageCount, int initialPage, PageAction action, int timeout = 300000, Action onTimeout = null)
        {
            if (pageCount == 1) return;
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

        private static ReactionMessage GetReactionMessageById(ulong id)
        {
            if (!ReactionMessageCache.Contains(id.ToString())) return null;

            return ReactionMessageCache.Get(id.ToString()) as ReactionMessage;
        }

        private static void DeleteReactionMessage(ReactionMessage reactionMessage)
        {
            ReactionMessageCache.Remove(reactionMessage.Message.ToString());
        }

        public static async Task HandleReactionMessage(ISocketMessageChannel channel, SocketSelfUser botUser, SocketReaction reaction, IUserMessage message)
        {
            if (message.Author.Id == botUser.Id && reaction.UserId != botUser.Id)
            {
                var reactionMessage = GetReactionMessageById(message.Id);
                if (reactionMessage != null && reaction.UserId == reactionMessage.Context.User.Id && reactionMessage.AcceptsAllReactions || reactionMessage.AcceptedReactions.Contains(reaction.Emote.ToString()))
                {
                    try
                    {
                        await reactionMessage.RunAction(reaction.Emote);
                    }
                    catch (Exception ex)
                    {
                        await ExceptionMessageHelper.HandleException(ex, channel);
                    }

                    if (reactionMessage.AllowMultipleReactions)
                    {
                        await message.RemoveReactionAsync(reaction.Emote, reactionMessage.Context.User);
                    }
                    else
                    {
                        await message.RemoveAllReactionsAsync();
                        DeleteReactionMessage(reactionMessage);
                    }
                }
                else if (reactionMessage != null && reaction.User.IsSpecified)
                {
                    await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                }
            }
        }
    }

    public class ReactionMessage
    {
        public BotCommandContext Context { get; }
        public IUserMessage Message { get; }
        public bool AllowMultipleReactions { get; }
        public bool AcceptsAllReactions { get; }
        public virtual IEnumerable<string> AcceptedReactions => Actions.Keys;
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
        public override IEnumerable<string> AcceptedReactions => new[] { FirstPage, LastPage, PreviousPage, NextPage };

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
