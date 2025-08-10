using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace VariableVixen.TinyCmds.Chat;

public static class ChatUtil {

	internal static List<Payload> GenerateMessagePayloads(params object[] payloads) {
		return payloads
			.Where(e => e is not null)
			.SelectMany(e => {
				return e switch {
					Payload pl => [pl],
					string st => [new TextPayload(st)],
					ChatColour ck => [new UIForegroundPayload((ushort)ck)],
					ChatGlow ck => [new UIGlowPayload((ushort)ck)],
					SeString se => se.Payloads.ToArray(),
					IEnumerable<Payload> en => en.ToArray(),
					object o => [new TextPayload(o.ToString() ?? "")],
					_ => [
						new UIForegroundPayload((ushort)ChatColour.ERROR),
						new TextPayload($"[internal error in {Plugin.PluginName}]"),
						new UIForegroundPayload((ushort)ChatColour.RESET),
					],
				};
			})
			.ToList();
	}

	#region Chatlog functions
	public static void ShowMessage(params object[] payloads) {
		if (payloads.Length < 1 || !payloads.Any(e => e is not null))
			return;
		Plugin.Chat.Print(new SeString(GenerateMessagePayloads(payloads)));
	}
	public static void ShowPrefixedMessage(params object[] payloads) {
		if (payloads.Length < 1 || !payloads.Any(e => e is not null))
			return;
		List<object> plList = [
			ChatColour.PREFIX,
			$"[{Plugin.Prefix}] ",
			ChatColour.RESET,
			.. payloads,
		];
		ShowMessage(plList.ToArray());
	}
	public static void ShowError(params object[] payloads) {
		if (payloads.Length < 1 || !payloads.Any(e => e is not null))
			return;
		Plugin.Chat.PrintError(new SeString(GenerateMessagePayloads(payloads)));
	}
	public static void ShowPrefixedError(params object[] payloads) {
		if (payloads.Length < 1 || !payloads.Any(e => e is not null))
			return;
		List<object> plList = [
			ChatColour.PREFIX,
			$"[{Plugin.Prefix}] ",
			ChatColour.RESET,
			.. payloads,
		];
		ShowError(plList.ToArray());
	}

	[Conditional("DEBUG")]
	public static void Debug(string message, params object[] args) {
		ShowPrefixedMessage(
			string.Format(message, args),
			ChatColour.RESET
		);
	}
	#endregion

	public static void SendChatlineToServer(string line, bool displayInChatlog = false, bool dryRun = false) {
		string content = Plugin.ServerChat.SanitiseText(line);
		Plugin.Log.Information("{0} [{1}]",
			dryRun ? "!>" : ">>",
			content);

		if (displayInChatlog || dryRun)
			ShowPrefixedMessage(ChatColour.DEBUG, content, ChatColour.RESET);
		if (!dryRun)
			Plugin.ServerChat.SendMessage(content);
	}
}
