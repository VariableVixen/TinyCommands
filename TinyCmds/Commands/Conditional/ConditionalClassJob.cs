using System.Linq;

using Lumina.Excel.Sheets;

using VariableVixen.TinyCmds.Attributes;

using VariableVixen.TinyCmds.Chat;
using VariableVixen.TinyCmds.Utils;

namespace VariableVixen.TinyCmds.Commands.Conditional;

[Command("/ifjob", "/ifclass", "/whenjob", "/whenclass", "/job", "/class")]
[Arguments("flags?", "jobs to match against", "command to run...?")]
[Summary("Run a chat command (or directly send a message) only when playing certain classes/jobs")]
[HelpText(
	"This command's test is whether or not your current class/job is one of the given set."
	+ " Use the three-letter abbreviation, and if you want to check against more than one, separate them with commas but NOT spaces."
)]
public class ConditionalClassJob: BaseConditionalCommand {
	protected override bool TryExecute(string? command, string rawArguments, FlagMap flags, bool inverted, bool verbose, bool dryRun, ref bool showHelp) {
		string arg = rawArguments ?? string.Empty;
		ClassJob? job = Plugin.Objects.LocalPlayer!.ClassJob.ValueNullable;
		Assert(job.HasValue, "cannot load current class/job game data");
		string currentJobName = job!.Value.Abbreviation.ToString().ToUpper();
		string wantedJobNames = arg.Split()[0].ToUpper();

		if (string.IsNullOrEmpty(wantedJobNames)) {
			showHelp = true;
			return false;
		}

		string cmd = arg.Contains(' ')
			? arg[(wantedJobNames.Length + 1)..]
			: string.Empty;
		bool invert = inverted || flags["n"]; // -n is deprecated but still functional
		bool match = wantedJobNames.Split(',').Contains(currentJobName);

		if (match ^ invert) {
			if (cmd.Length > 0) {
				ChatUtil.SendChatlineToServer(cmd, verbose || dryRun, dryRun);
			}
			else {
				ChatUtil.ShowPrefixedMessage(
					ChatColour.CONDITION_PASSED,
					"You are currently a ",
					ChatGlow.CONDITION_PASSED,
					currentJobName,
					ChatGlow.RESET,
					ChatColour.RESET
				);
			}

			return true;
		}

		if (cmd.Length < 1) {
			ChatUtil.ShowPrefixedMessage(
				ChatColour.CONDITION_FAILED,
				"You are currently a ",
				ChatGlow.CONDITION_FAILED,
				currentJobName,
				ChatGlow.RESET,
				ChatColour.RESET
			);
		}

		return false;
	}
}
