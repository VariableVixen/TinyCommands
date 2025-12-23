using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

using VisibilityFlags = FFXIVClientStructs.FFXIV.Client.Game.Object.VisibilityFlags;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Lumina.Excel.Sheets;

using VariableVixen.TinyCmds.Attributes;

using VariableVixen.TinyCmds.Chat;
using VariableVixen.TinyCmds.Utils;

using CSGO = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject; // Client Structs Game Object, not Counter Strike Global Offensive :P

namespace VariableVixen.TinyCmds.Commands;

[Command("/whereis", "/locate", "/find")]
[Summary("Find nearby objects, players, and NPCs by name")]
[Arguments("partial name...")]
[HelpText(
	"This command locates a game object (player, npc, etc) by name in your current zone."
	+ " All found objects are printed to chat with their full name and a location flag for where they were found."
	+ " It uses case-insensitive partial matching, so that something like 'alph' will match Alphinaud and also Alpha.",
	"",
	"By default, objects are sorted alphabetically, but you can use the -d flag to instead sort by distance from you."
	+ " By default, sorts are in ascending order (a-z or closest first). To reverse the sort order, pass the -i flag.",
	"",
	"If you pass the -f flag, the first result IN THE SORT ORDER USED will be flagged on your map automatically."
	+ " The -s flag can only be used with -f, and will silence the result list. To silence ALL messages including failure to find anything, use -S.",
	"",
	"By default, only \"real\" results are returned, meaning things you can see and target yourself. To show \"ghosts\", use -a."
	+ " By default, only NPCs, players, companions, and \"event objects\" are shown. Generally, this is all that most people care about. To show all types, use -A."
	+ " In general, you shouldn't need either of these, unless you're trying to find something like an aether current or a quest NPC for a quest you aren't doing."
)]
public class LocateGameObjectCommand: PluginCommand {
	protected override unsafe void Execute(string? command, string rawArguments, FlagMap flags, bool verbose, bool dryRun, ref bool showHelp) {
		if (string.IsNullOrWhiteSpace(rawArguments)) {
			ChatUtil.ShowPrefixedError("You need to provide a (partial) name filter.");
			showHelp = true;
			return;
		}

		VisibilityFlags invisibleFlags = VisibilityFlags.Model | VisibilityFlags.Nameplate;

		string needle = rawArguments.Trim();
		Vector3 here = Plugin.Objects.LocalPlayer!.Position;
		IEnumerable<(string name, Vector3 position, float distance)> found = Plugin.Objects
			.Where(o =>
				(flags['A'] || o.ObjectKind is ObjectKind.BattleNpc or ObjectKind.Player or ObjectKind.EventNpc or ObjectKind.EventObj or ObjectKind.Companion)
				&& (flags['a'] || ((CSGO*)o.Address)->GetIsTargetable() || (((CSGO*)o.Address)->RenderFlags & invisibleFlags) != invisibleFlags || ((CSGO*)o.Address)->DrawObject is not null)
				&& o.Name.TextValue.Contains(needle, StringComparison.OrdinalIgnoreCase)
			)
			.Select(o => (o.Name.TextValue.Trim(), o.Position, Vector3.Distance(o.Position, here)));

		if (!found.Any()) {
			if (!(flags['f'] && flags['S']))
				ChatUtil.ShowPrefixedError(ChatColour.HIGHLIGHT_FAILED, "No results found for ", ChatColour.RESET, ChatColour.CONDITION_FAILED, rawArguments.Trim(), ChatColour.RESET);
			return;
		}

		found = flags['d']
			? found.OrderBy(o => o.distance)
			: found.OrderBy(o => o.name.ToLower());
		if (flags['i'])
			found = found.Reverse();

		uint zone = Plugin.Client.TerritoryType;
		Map map = Plugin.Data.GetExcelSheet<TerritoryType>()?.GetRowOrDefault(zone)?.Map.Value ?? throw new NullReferenceException("Cannot find map ID");
		uint mapId = map.RowId;
		int count = found.Count();

		if (flags['f']) {
			try {
				AgentMap* agentMap = AgentMap.Instance();
				Vector3 pos = found.First().position;
				agentMap->SetFlagMapMarker(zone, mapId, pos);
			}
			catch (Exception e) {
				Plugin.Log.Error($"Failed to set map marker quietly: {e}");
				Plugin.Log.Information("Falling back to loud mode! [excessive screaming]");
				Vector2 mapped = Plugin.WorldToMap(found.First().position, map);
				MapLinkPayload pl = new(zone, mapId, mapped.X, mapped.Y);
				Plugin.Gui.OpenMapWithMapLink(pl);
			}
			if (flags['s'])
				return;
		}

		SeStringBuilder msg = new SeStringBuilder()
			.AddUiForeground((ushort)ChatColour.PREFIX)
			.AddText($"[{Plugin.Prefix}] ")
			.AddUiForegroundOff()
			.AddUiForeground((ushort)ChatColour.CONDITION_PASSED)
			.AddText("Found ")
			.AddUiForegroundOff()
			.AddUiForeground((ushort)ChatColour.HIGHLIGHT_PASSED)
			.AddText(count.ToString())
			.AddUiForegroundOff()
			.AddUiForeground((ushort)ChatColour.CONDITION_PASSED)
			.AddText($" entit{(count == 1 ? "y" : "ies")} matching ")
			.AddUiForegroundOff()
			.AddUiForeground((ushort)ChatColour.HIGHLIGHT_PASSED)
			.AddText(rawArguments.Trim())
			.AddUiForegroundOff();

		foreach ((string name, Vector3 position, float distance) in found) {
			Vector2 mapped = Plugin.WorldToMap(position, map);
			msg = msg
				.AddText("\n")
				.AddUiForeground((ushort)ChatColour.HIGHLIGHT_PASSED)
				.AddText(name)
				.AddUiForegroundOff()
				.AddUiForeground((ushort)ChatColour.QUIET)
				.AddText(" (")
				.AddUiForegroundOff()
				.AddUiForeground((ushort)ChatColour.HIGHLIGHT)
				.AddText(distance.ToString("0.0"))
				.AddUiForegroundOff()
				.AddUiForeground((ushort)ChatColour.QUIET)
				.AddText("y): ")
				.AddUiForegroundOff()
				// Taken from https://github.com/Ottermandias/GatherBuddy/blob/39a7947d47bd4b2f266715b6bbe3cf4b3b3af298/GatherBuddy/Plugin/Communicator.cs#L34
				.AddUiForeground(0x0225)
				.AddUiGlow(0x0226)
				.Add(new MapLinkPayload(zone, mapId, mapped.X, mapped.Y))
				.AddUiForeground(500)
				.AddUiGlow(501)
				.AddText($"{(char)SeIconChar.LinkMarker}")
				.AddUiGlowOff()
				.AddUiForegroundOff()
				.AddText($"({mapped.X:0.0}, {mapped.Y:0.0})")
				.Add(RawPayload.LinkTerminator)
				.AddUiGlowOff()
				.AddUiForegroundOff();
		}

		SeString built = msg.AddUiForegroundOff().BuiltString;
#if DEBUG
		Plugin.Log.Information($"{built.Encode().LongLength} bytes:\n{built.TextValue}");
#endif
		Plugin.Chat.Print(built);
	}
}
