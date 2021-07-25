﻿using System;
using System.Linq;
using System.Reflection;

using Dalamud.Game.Command;

using TinyCmds.Attributes;

namespace TinyCmds {
	public partial class TinyCmdPluginCommandManager {
		internal class PluginCommand {
			internal delegate void PluginCommandInvocationErrorHandlerDelegate(params object[] payloads);
			private readonly PluginCommandDelegate handler, helper;
			private readonly PluginCommandInvocationErrorHandlerDelegate error;
			public CommandInfo MainCommandInfo => new(this.Dispatch) {
				HelpMessage = this.Summary,
				ShowInHelp = this.ShowInDalamud,
			};
			public CommandInfo AliasCommandInfo => new(this.Dispatch) {
				HelpMessage = this.Summary,
				ShowInHelp = false,
			};
			public string CommandComparable => this.Command.TrimStart('/').ToLower();
			public string[] AliasesComparable => this.Aliases.Select(s => s.TrimStart('/').ToLower()).ToArray();
			public string[] HelpLines => this.Help.Split('\r', '\n').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
			public string Command { get; }
			public string Summary { get; }
			public string Help { get; }
			public string Usage { get; }
			public string[] Aliases { get; }
			public int MinArgs { get; }
			public int MaxArgs { get; }
			public bool ShowInDalamud { get; }
			public bool ShowInListing { get; }
			public PluginCommand(object instance, MethodInfo method, PluginCommandDelegate printHelp, PluginCommandInvocationErrorHandlerDelegate onError) {
				CommandAttribute attrCommand = method.GetCustomAttribute<CommandAttribute>();
				if (attrCommand is null) {
					throw new NullReferenceException("Cannot construct PluginCommand from method without CommandAttribute");
				}
				ArgumentsAttribute args = method.GetCustomAttribute<ArgumentsAttribute>();
				this.Command = $"/{attrCommand.Command.TrimStart('/')}";
				this.Summary = method.GetCustomAttribute<SummaryAttribute>()?.Summary ?? "";
				this.Help = method.GetCustomAttribute<HelpMessageAttribute>()?.HelpMessage ?? "";
				this.Usage = $"{this.Command} {args?.ArgumentDescription}".Trim();
				this.Aliases = method.GetCustomAttribute<AliasesAttribute>()?.Aliases ?? new string[0];
				this.MinArgs = args?.RequiredArguments ?? 0;
				this.MaxArgs = args?.MaxArguments ?? int.MaxValue;
				this.ShowInDalamud = method.GetCustomAttribute<DoNotShowInHelpAttribute>() is null;
				this.ShowInListing = method.GetCustomAttribute<HideInCommandListingAttribute>() is null;
				this.handler = Delegate.CreateDelegate(typeof(PluginCommandDelegate), instance, method) as PluginCommandDelegate;
				this.helper = printHelp;
				this.error = onError;
			}
			public void Dispatch(string command, string argline) {
				try {
					(FlagMap flags, string rawArgs) = TinyCmdsPlugin.ExtractFlags(argline);
					bool showHelp = false;
					if (flags["h"]) {
						this.helper(null, command, flags, ref showHelp);
						return;
					}
					this.handler(command, rawArgs, flags, ref showHelp);
					if (showHelp)
						this.helper(null, command, flags, ref showHelp);
				}
				catch (Exception e) {
					while (e is not null) {
						this.error(
							$"{e.GetType().Name}: {e.Message}\n",
							ChatColour.QUIET,
							$"at {e.TargetSite.DeclaringType.FullName} in {e.TargetSite.DeclaringType.Assembly}",
							ChatColour.RESET
						);
						e = e.InnerException;
					}
				}
			}
		}
	}
}
