using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Text.SeStringHandling.Payloads;

using FFXIVClientStructs.FFXIV.Client.Game;

using Lumina.Excel.Sheets;

using VariableVixen.TinyCmds.Attributes;

using VariableVixen.TinyCmds.Chat;
using VariableVixen.TinyCmds.Utils;

namespace VariableVixen.TinyCmds.Commands;

[Command("/useitem", "/use", "/item")]
[Summary("Use an item from your inventory by numeric ID or by name (case-insensitive full match)")]
[HelpText(
	"This command attempts to use an item from your inventory, including key items. It requires the item ID, which you can find using something like SimpleTweaks."
)]
public unsafe class UseItem: PluginCommand {
	private static Dictionary<uint, string> items = null!, keyItems = null!;

	protected override void Initialise() {
		Plugin.Interop.InitializeFromAttributes(this);
		items = Plugin.Data.GetExcelSheet<Item>()!
			.Where(i => i.ItemAction.RowId > 0)
			.ToDictionary(i => i.RowId, i => i.Name.ToString().ToLower());
		keyItems = Plugin.Data.GetExcelSheet<EventItem>()!
			.Where(i => i.Action.RowId > 0)
			.ToDictionary(i => i.RowId, i => i.Name.ToString().ToLower());
	}

	protected override void Execute(string? command, string rawArguments, FlagMap flags, bool verbose, bool dryRun, ref bool showHelp) {
		InventoryManager* inv = InventoryManager.Instance();
		if (inv is null)
			throw new InvalidOperationException("Cannot find InventoryManager instance");

		string target = rawArguments.ToLower().Trim();
		if (!uint.TryParse(target, out uint itemId)) {
			ChatUtil.ShowPrefixedError("This command requires an item ID");
			return;
		}

		ActionType type = ActionType.None;
		if (items.TryGetValue(itemId, out string? name))
			type = ActionType.Item;
		else if (keyItems.TryGetValue(itemId, out name))
			type = ActionType.KeyItem;

		if (type is ActionType.None) {
			ChatUtil.ShowPrefixedError($"Cannot find item for ID {itemId}");
		}
		else if (dryRun) {
			ChatUtil.ShowPrefixedMessage(
				"Attempting to use ",
				new UIForegroundPayload(14),
				name!,
				new UIForegroundPayload(0),
				" (item ID ",
				new UIForegroundPayload(14),
				itemId,
				new UIForegroundPayload(0),
				")"
			);
		}
		else {
			this.use(itemId, type);
		}
	}

	private void use(uint id, ActionType type) {
		InventoryManager* inv = InventoryManager.Instance();
		ActionManager* actions = ActionManager.Instance();
		if (inv is null) {
			Plugin.Log.Error("can't retrieve InventoryManager instance");
			return;
		}
		if (actions is null) {
			Plugin.Log.Error("can't retrieve ActionManager instance");
			return;
		}

		if (id == 0 || inv->GetInventoryItemCount(id) == 0)
			return;

		actions->UseAction(type, id, 0xE000_0000, 0xFFFF);
	}

}
