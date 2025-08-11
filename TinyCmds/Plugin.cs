using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Lumina.Excel.Sheets;

using VariableVixen.TinyCmds.Chat;
using VariableVixen.TinyCmds.Internal;
using VariableVixen.TinyCmds.Ui;

using XivCommon;

namespace VariableVixen.TinyCmds;

public class Plugin: IDalamudPlugin {
	internal const uint FindFateByNamePayloadId = 1;

	public const string PluginName = "TinyCommands";
	public const string Prefix = "TinyCmds";

	private bool disposed = false;

	[PluginService] internal static IChatGui Chat { get; private set; } = null!;
	[PluginService] internal static IGameGui Gui { get; private set; } = null!;
	[PluginService] internal static IToastGui Toast { get; private set; } = null!;
	[PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
	[PluginService] internal static ISigScanner Scanner { get; private set; } = null!;
	[PluginService] internal static ICommandManager CmdManager { get; private set; } = null!;
	[PluginService] internal static IClientState Client { get; private set; } = null!;
	[PluginService] internal static ICondition Conditions { get; private set; } = null!;
	[PluginService] internal static ITargetManager Targets { get; private set; } = null!;
	[PluginService] internal static IDataManager Data { get; private set; } = null!;
	[PluginService] internal static IPartyList Party { get; private set; } = null!;
	[PluginService] internal static IObjectTable Objects { get; private set; } = null!;
	[PluginService] internal static IFateTable Fates { get; private set; } = null!;
	[PluginService] internal static IFramework Framework { get; private set; } = null!;
	[PluginService] internal static IPluginLog Log { get; private set; } = null!;
	[PluginService] internal static IGameInteropProvider Interop { get; private set; } = null!;
	internal static PluginCommandManager CommandManager { get; private set; } = null!;
	internal static XivCommonBase Common { get; private set; } = null!;
	internal static ServerChat ServerChat { get; private set; } = null!;

	private readonly WindowSystem windowSystem;
	internal readonly Dictionary<string, Window> helpWindows;

	public Plugin() {
		//Common = new(Interface);
		//ServerChat = Common.Functions.Chat;
		// XivCommon isn't updated yet, so we're ripping the chat functionality locally
		ServerChat = new();
		CommandManager = new(this) {
			ErrorHandler = ChatUtil.ShowPrefixedError
		};
		CommandManager.AddCommandHandlers();

		this.windowSystem = new(this.GetType().Namespace!);
		this.helpWindows = CommandManager.Commands.ToDictionary(cmd => cmd.CommandComparable, cmd => new HelpWindow(cmd) as Window);
		this.helpWindows.Add("<PLUGIN>", new HelpWindow(
			"Basics",
			PluginName,
			"the plugin itself",
			"Basic information about how commands work",
			$"{PluginName} uses a custom command parser that accepts single-character boolean flags starting with a hyphen."
			+ "These flags can be bundled into one argument, such that \"-va\" will set both the \"v\" and \"a\" flags, just like \"-av\" will.\n"
			+ "\n"
			+ "All commands accept \"-h\" to display their built-in help.\n"
			+ "\n"
			+ "To list all commands, you can use \"/tinycmds\", optionally with \"-a\" to also list their aliases."
			+ " Be aware that this list may be a little long. It's also not really sorted."
		));
		this.helpWindows.Add("<LIST>", new CommandListWindow(this));

		foreach (Window wnd in this.helpWindows.Values)
			this.windowSystem.AddWindow(wnd);

		PluginInterface.UiBuilder.Draw += this.windowSystem.Draw;
	}

	internal static Vector2 WorldToMap(Vector3 pos, Map zone) => WorldToMap(new Vector2(pos.X, pos.Z), zone);
	internal static Vector2 WorldToMap(Vector2 pos, Map zone) {
		Vector2 raw = MapUtil.WorldToMap(pos, zone);
		return new((int)MathF.Round(raw.X * 10, 1) / 10f, (int)MathF.Round(raw.Y * 10, 1) / 10f);
	}
	internal static Vector2 MapToWorld(Vector2 pos, Map zone) {
		MapLinkPayload maplink = new(zone.TerritoryType.Value.RowId, zone.RowId, pos.X, pos.Y);
		return new(maplink.RawX / 1000f, maplink.RawY / 1000f);
	}

	#region IDisposable Support
	protected virtual void Dispose(bool disposing) {
		if (this.disposed)
			return;
		this.disposed = true;

		if (disposing) {
			Common?.Dispose();
			CommandManager.Dispose();
			PluginInterface.UiBuilder.Draw -= this.windowSystem.Draw;
			this.windowSystem.RemoveAllWindows();
		}

	}
	public void Dispose() {
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}
	~Plugin() {
		this.Dispose(false);
	}
	#endregion
}
