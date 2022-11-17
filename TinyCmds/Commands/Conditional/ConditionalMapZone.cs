namespace PrincessRTFM.TinyCmds.Commands.Conditional;

using System;
using System.Linq;

using PrincessRTFM.TinyCmds.Attributes;
using PrincessRTFM.TinyCmds.Chat;
using PrincessRTFM.TinyCmds.Utils;

[Command("/ifzone")]
[Arguments("'-n'?", "zone IDs to match against", "command to run...?")]
[Summary("Run a chat command (or directly send a message) only when in a certain map zone")]
[Aliases("/ifmap", "/ifmapzone")]
[HelpMessage(
	"This command's test is whether or not your current zone ID is one of the given set."
	+ " Use the numeric ID, and if you want to check against more than one, separate them with commas but NOT spaces."
	+ " If you pass the -n flag, the match will be inverted so the command runs only when you AREN'T in one of the given zones.",
	"",
	"Using -g will print your current zone ID, to make it easier to find the one you want."
)]
public class ConditionalMapZone: BaseConditionalCommand {
	protected override bool TryExecute(string? command, string args, FlagMap flags, bool verbose, bool dryRun, ref bool showHelp) {
		string arg = args ?? string.Empty;
		ushort territory = Plugin.client.TerritoryType;

		if (territory == 0) {
			ChatUtil.ShowPrefixedError("Cannot identify current area");
			return false;
		}

		if (flags['g']) {
			ChatUtil.ShowPrefixedMessage($"You are in map zone {territory}");
			return false;
		}

		string wantedMapZones = arg.Split()[0];

		if (string.IsNullOrEmpty(wantedMapZones)) {
			showHelp = true;
			return false;
		}

		string cmd = arg.Contains(' ')
			? arg[(wantedMapZones.Length + 1)..]
			: string.Empty;
		bool invert = flags["n"];
		bool match = wantedMapZones.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(territory.ToString());

		if (match ^ invert) {

			if (cmd.Length > 0) {
				ChatUtil.SendChatlineToServer(cmd, dryRun || verbose, dryRun);
			}
			else {
				ChatUtil.ShowPrefixedMessage(
					ChatColour.CONDITION_PASSED,
					"You are currently in zone ",
					ChatGlow.CONDITION_PASSED,
					territory,
					ChatGlow.RESET,
					ChatColour.RESET
				);
			}

			return true;
		}

		if (cmd.Length < 1) {
			ChatUtil.ShowPrefixedMessage(
				ChatColour.CONDITION_FAILED,
				"You are currently in zone ",
				ChatGlow.CONDITION_FAILED,
				territory,
				ChatGlow.RESET,
				ChatColour.RESET
			);
		}

		return false;
	}
}
