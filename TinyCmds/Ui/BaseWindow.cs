using System.Diagnostics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;

namespace VariableVixen.TinyCmds.Ui;

internal abstract class BaseWindow: Window {
	private const ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;

	protected BaseWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None) : base(name, WindowFlags | flags) {
		this.RespectCloseHotkey = true;
		this.IsOpen = false;
		this.TitleBarButtons = [
			new() {
				Priority = 0,
				Icon = FontAwesomeIcon.Heart,
				IconOffset = new(2, 1),
				Click = _ => Process.Start(new ProcessStartInfo("https://ko-fi.com/V7V7IK9UU") { UseShellExecute = true }),
				ShowTooltip = () => {
					ImGui.BeginTooltip();
					ImGui.TextUnformatted("Support me on ko-fi");
					ImGui.EndTooltip();
				},
			},
			new() {
				Priority = 1,
				Icon = FontAwesomeIcon.Code,
				IconOffset = new(1, 1),
				Click = _ => Process.Start(new ProcessStartInfo("https://github.com/VariableVixen/TinyCommands") { UseShellExecute = true }),
				ShowTooltip = () => {
					ImGui.BeginTooltip();
					ImGui.TextUnformatted("Browse the github repo");
					ImGui.EndTooltip();
				},
			},
		];
		this.AllowClickthrough = true;
		this.AllowPinning = true;
	}
}
