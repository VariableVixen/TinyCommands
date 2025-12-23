using System.Linq;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;

using VariableVixen.TinyCmds.Attributes;

using VariableVixen.TinyCmds.Chat;
using VariableVixen.TinyCmds.Utils;

namespace VariableVixen.TinyCmds.Commands.Conditional;

[Command("/ifcmd", "/ifthen", "/ifcondition", "/ifcond", "/ifstate")]
[Arguments("flags", "command to run...?")]
[Summary("Run a chat command (or directly send a message) only if a condition is met")]
[HelpText(
	"This command's test is based on game state, as described by the flags you use."
	+ " Lowercase flags require that their condition be met, uppercase flags require that their condition NOT be met. The available flags are:",
	"-t has target",
	"-f has focus",
	"-o has mouseover",
	"-c in combat",
	"-p targeting player",
	"-n targeting NPC",
	"-m targeting minion",
	"-w unmounted",
	"-s swimming",
	"-d diving",
	"-u flying",
	"-i in duty",
	"-l using fashion accessory",
	"-r weapon drawn",
	"-a in alliance",
	"-g has party members"
)]
public class ConditionalGameFlags: BaseConditionalCommand {
	protected override bool TryExecute(string? command, string rawArguments, FlagMap flags, bool inverted, bool verbose, bool dryRun, ref bool showHelp) {
		bool condition = inverted ^ conditions(flags);

		if (condition) {
			if (rawArguments.Length > 0)
				ChatUtil.SendChatlineToServer(rawArguments, dryRun || verbose, dryRun);
			else
				ChatUtil.ShowPrefixedMessage(ChatColour.CONDITION_PASSED, "All tests passed but no message was provided.", ChatColour.RESET);
		}
		else if (rawArguments.Length == 0) {
			ChatUtil.ShowPrefixedMessage(ChatColour.CONDITION_FAILED, "Tests did not pass.", ChatColour.RESET);
		}

		return condition;
	}

	private static bool flagToCondition(string flag) {
		bool invert = flag.All(char.IsUpper);
		return invert ^ (flag.ToLower() switch {
			"t" => Plugin.Targets.Target is not null,
			"p" => Plugin.Targets.Target?.ObjectKind is ObjectKind.Player,
			"n" => Plugin.Targets.Target?.ObjectKind is ObjectKind.BattleNpc or ObjectKind.EventNpc or ObjectKind.Retainer,
			"m" => Plugin.Targets.Target?.ObjectKind is ObjectKind.Companion,
			"f" => Plugin.Targets.FocusTarget is not null,
			"o" => Plugin.Targets.MouseOverTarget is not null,
			"c" => Plugin.Conditions[ConditionFlag.InCombat],
			"w" => !Plugin.Conditions[ConditionFlag.Mounted],
			"s" => Plugin.Conditions[ConditionFlag.Swimming],
			"d" => Plugin.Conditions[ConditionFlag.Diving],
			"u" => Plugin.Conditions[ConditionFlag.InFlight],
			"i" => Plugin.Conditions[ConditionFlag.BoundByDuty],
			"l" => Plugin.Conditions[ConditionFlag.UsingFashionAccessory],
			"r" => Plugin.Objects.LocalPlayer!.StatusFlags.HasFlag(StatusFlags.WeaponOut),
			"a" => Plugin.Party.IsAlliance,
			"g" => Plugin.Party.Length > 0,
			_ => !invert, // all non-test flags are treated as "passing" so they're effectively ignored
		});
	}
	private static bool conditions(FlagMap flags) => flags.Enabled.Select(flagToCondition).All(b => b);
}
