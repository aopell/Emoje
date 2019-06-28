using Discord.Commands;
using DiscordHackWeek2019.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DiscordHackWeek2019.Helpers
{
    public static class HelpHelper
    {
        private static List<HelpInfo> commands = null;
        public static List<HelpInfo> AllCommands => commands ?? (commands = GetAllCommands());

        private static List<HelpInfo> GetAllCommands()
        {
            List<HelpInfo> commands = new List<HelpInfo>();

            var modules = Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsNested && t.IsSubclassOf(typeof(ModuleBase<BotCommandContext>)));
            foreach (var module in modules)
            {
                commands.AddRange(GetAllCommands(module));
            }

            return commands;
        }

        private static List<HelpInfo> GetAllCommands(Type module, string prefix = "")
        {
            List<HelpInfo> infos = new List<HelpInfo>();
            if (Attribute.IsDefined(module, typeof(GroupAttribute)))
            {
                var attr = module.GetCustomAttribute<GroupAttribute>();
                prefix += attr.Prefix + " ";
            }
            var nestedTypes = module.GetNestedTypes().SelectMany(x => GetAllCommands(x, prefix));
            infos.AddRange(nestedTypes);

            var commands = module.GetMethods().Where(m => Attribute.IsDefined(m, typeof(CommandAttribute)));

            foreach (var command in commands)
            {
                var commandAttr = command.GetCustomAttribute<CommandAttribute>();
                var summaryAttr = command.GetCustomAttribute<SummaryAttribute>();
                infos.Add(new HelpInfo((prefix + commandAttr.Text).Trim(), summaryAttr?.Text, getParams(command)));
            }

            return infos;

            List<HelpParam> getParams(MethodInfo command)
            {
                List<HelpParam> paramInfo = new List<HelpParam>();
                foreach (var param in command.GetParameters())
                {
                    var summaryAttr = param.GetCustomAttribute<SummaryAttribute>();
                    paramInfo.Add(new HelpParam(param.Name, param.ParameterType.Name, summaryAttr?.Text, Attribute.IsDefined(param, typeof(RemainderAttribute)), param.HasDefaultValue ? (param.DefaultValue?.ToString() ?? "null") : null));
                }
                return paramInfo;
            }
        }
    }

    public struct HelpInfo
    {
        public string Command { get; }
        public string Summary { get; }
        public List<HelpParam> Parameters { get; }

        public HelpInfo(string command, string summary, List<HelpParam> parameters)
        {
            Command = command;
            Summary = summary;
            Parameters = parameters;
        }

        public override string ToString() => $"`{$"{Command} {string.Join(' ', Parameters)}".Trim()}`\n{Summary}".Trim();
    }

    public struct HelpParam
    {
        public string Name { get; }
        public string Type { get; }
        public string Summary { get; }
        public bool Optional => !string.IsNullOrEmpty(DefaultValue);
        public bool Remainder { get; }
        public string DefaultValue { get; }

        public HelpParam(string name, string type, string summary, bool remainder, string defaultValue)
        {
            Name = name;
            Type = type;
            Summary = summary;
            Remainder = remainder;
            DefaultValue = defaultValue;
        }

        public override string ToString() => $"{(Optional ? "[" : "<")}{Name}{(Remainder ? "..." : "")}{(Optional && DefaultValue != "null" ? $" = {DefaultValue}" : "")}{(Optional ? "]" : ">")}".Trim();
    }
}
