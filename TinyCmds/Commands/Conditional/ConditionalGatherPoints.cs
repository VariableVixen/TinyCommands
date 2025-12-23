using Dalamud.Game.ClientState.Objects.SubKinds;

using VariableVixen.TinyCmds.Attributes;

using VariableVixen.TinyCmds.Chat;
using VariableVixen.TinyCmds.Utils;

namespace VariableVixen.TinyCmds.Commands.Conditional;

[Command("/ifgp", "/gp", "/whengp")]
[Arguments("flags", "GP to compare?", "command to run...?")]
[Summary("Run a chat command (or directly send a message) only if GP meets condition")]
[HelpText(
	"This command's test uses numeric inequality conditions against your GP."
	+ " There are three possible tests: at least (-g), less than (-l), and a simple at capacity (-c)."
	+ " If using -g or -l, the first argument should be a number to compare against. If using -c, ALL arguments are the command to run when your GP passes the check."
)]
public class ConditionalGatherPoints: BaseConditionalCommand {
	protected override bool TryExecute(string? command, string rawArguments, FlagMap flags, bool inverted, bool verbose, bool dryRun, ref bool showHelp) {
		string arg = rawArguments ?? string.Empty;
		IPlayerCharacter player = Plugin.Objects.LocalPlayer!;
		uint gp = player.CurrentGp;
		if (player.MaxGp < 1) { // presumably not a gathering job, or maybe they have none unlocked - is MaxGp >0 when DoL is unlocked but current job is different?
			ChatUtil.Debug("You have no GP");
			return false;
		}

		bool condition;
		if (flags['c']) {
			condition = player.CurrentGp >= player.MaxGp;
		}
		else {
			string num = arg.Split()[0];
			string cmd = arg[num.Length..].Trim();
			if (!int.TryParse(num, out int compareTo)) {
				ChatUtil.ShowPrefixedError($"Couldn't parse \"{num}\" as an integer");
				return false;
			}
			if (flags['g']) {
				condition = player.CurrentGp >= compareTo;
			}
			else if (flags['l']) {
				condition = player.CurrentGp < compareTo;
			}
			else {
				ChatUtil.ShowPrefixedError("Expected one of -c, -g, or -l, but found none");
				return false;
			}
		}

		if (condition ^ inverted)
			ChatUtil.SendChatlineToServer(arg, dryRun || verbose, dryRun);

		return condition ^ inverted;
	}
}
