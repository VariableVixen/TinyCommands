using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace VariableVixen.TinyCmds.Ui;

internal class CommandListWindow: HelpWindow {
	private readonly Plugin plugin;

	public CommandListWindow(Plugin core) : base("Command List", Plugin.PluginName, "command listing", "A list of all commands in the plugin", "") {
		this.SizeConstraints = new() {
			MinimumSize = new(300, 100),
			MaximumSize = new(450, 550),
		};
		this.plugin = core;
	}

	public override void Draw() {
		this.DrawHeader();

		foreach (PluginCommand cmd in Plugin.CommandManager.Commands) {
			if (!cmd.ShowInListing)
				continue;

			if (this.plugin.helpWindows.TryGetValue(cmd.CommandComparable, out Window? wnd)) {
				ImGui.AlignTextToFramePadding();
				ImGui.PushFont(UiBuilder.IconFont);
				bool getHelp = ImGui.Button($"{FontAwesomeIcon.Question.ToIconString()}###OpenHelp_{cmd.InternalName}");
				ImGui.PopFont();
				if (getHelp) {
					Plugin.Log.Information($"Opening help window for {cmd.InternalName}");
					wnd.IsOpen = true;
				}
				ImGui.SameLine();
				ImGui.Text(cmd.Usage);
			}
		}
	}

}
