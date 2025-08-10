using System;
using System.Linq;

using Dalamud.Game.ClientState.Conditions;

using FFXIVClientStructs.FFXIV.Client.Game.Character;

using VariableVixen.TinyCmds.Attributes;

using VariableVixen.TinyCmds.Chat;
using VariableVixen.TinyCmds.Utils;

using PC = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;

namespace VariableVixen.TinyCmds.Commands.Conditional;

[Command("/ifmount")]
[Arguments("'-n'?", "mount IDs to match against", "command to run...?")]
[Summary("Run a chat command (or directly send a message) only when using a specific mount")]
[HelpText(
	"This command's test is whether or not your current mount ID is one of the given set."
	+ " Use the numeric ID, and if you want to check against more than one, separate them with commas but NOT spaces."
	+ " If you pass the -n flag, the match will be inverted so the command runs only when you AREN'T using one of the given mounts."
	+ " If you are not mounted, your effective mount ID is 0, which can be used for the check.",
	"",
	"Using -g will print your current mount ID, to make it easier to find the one you want."
)]

public class ConditionalMount: BaseConditionalCommand {
	private const string CurrentMountLabel = "Your current mount ID is ";
	protected override unsafe bool TryExecute(string? command, string rawArguments, FlagMap flags, bool verbose, bool dryRun, ref bool showHelp) {
		string arg = rawArguments ?? string.Empty;
		PC* player = (PC*)Plugin.Client.LocalPlayer!.Address; // LocalPlayer is guaranteed to be non-null by BaseConditionalCommand
		Assert(player is not null, "failed to acquire CS LocalPlayer");
		MountContainer? mount = Plugin.Conditions[ConditionFlag.Mounted] || Plugin.Conditions[ConditionFlag.RidingPillion] ? player->Mount : null;
		ushort mountId = mount?.MountId ?? 0;

		if (flags['g']) {
			ChatUtil.ShowPrefixedMessage($"{CurrentMountLabel} {mountId}");
			return false;
		}

		string wantedMountIds = arg.Split()[0];

		if (string.IsNullOrEmpty(wantedMountIds)) {
			showHelp = true;
			return false;
		}

		string cmd = arg.Contains(' ')
			? arg[(wantedMountIds.Length + 1)..]
			: string.Empty;
		bool invert = flags["n"];
		bool match = wantedMountIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Contains(mountId.ToString());

		if (match ^ invert) {

			if (cmd.Length > 0) {
				ChatUtil.SendChatlineToServer(cmd, dryRun || verbose, dryRun);
			}
			else {
				ChatUtil.ShowPrefixedMessage(
					ChatColour.CONDITION_PASSED,
					CurrentMountLabel,
					ChatGlow.CONDITION_PASSED,
					mountId,
					ChatGlow.RESET,
					ChatColour.RESET
				);
			}

			return true;
		}

		if (cmd.Length < 1) {
			ChatUtil.ShowPrefixedMessage(
				ChatColour.CONDITION_FAILED,
				CurrentMountLabel,
				ChatGlow.CONDITION_FAILED,
				mountId,
				ChatGlow.RESET,
				ChatColour.RESET
			);
		}

		return false;
	}
}
