using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Dalamud.Game.Command;

using VariableVixen.TinyCmds.Attributes;

namespace VariableVixen.TinyCmds;

public delegate void PluginCommandInvocationErrorHandler(params object[] payloads);

public class PluginCommandManager: IDisposable {

	private readonly List<PluginCommand> commandList;

	internal PluginCommand[] Commands => this.commandList.ToArray();

	private readonly bool disposed = false;

	public PluginCommandInvocationErrorHandler? ErrorHandler { get; internal set; }
	public PluginCommand? HelpHandler { get; internal set; }

	public PluginCommandManager(Plugin core) {
		Type b = typeof(PluginCommand);
		Type p = core.GetType();
		this.commandList = this.GetType().Assembly.GetTypes()
			.Where(t => t.IsSubclassOf(b) && !t.IsAbstract && t.GetConstructor([]) is not null)
			.Select(t => {
				try {
					ConstructorInfo ctor = t.GetConstructor([])!;
					object instance = RuntimeHelpers.GetUninitializedObject(t);
					PropertyInfo prop = b
						.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
						.First(prop => prop.PropertyType == p);
					Plugin.Log.Information($"Injecting {p.Name} object to {t.Name}.{prop.Name}");
					prop.SetValue(instance, core);
					Plugin.Log.Information($"Invoking {t.Name}.<ctor>()");
					ctor.Invoke(instance, null);
					PluginCommand? cmd = instance as PluginCommand;
					cmd?.Setup();
					return cmd;
				}
				catch (Exception e) {
					Plugin.Log.Error(e, $"Failed to instantiate {t.Name}");
					return null;
				}
			})
			.Where(o => o is not null)
			.Cast<PluginCommand>()
			.ToList();
		this.HelpHandler = this.commandList.Find(c => c.GetType().GetCustomAttribute<PluginCommandHelpHandlerAttribute>() is not null);
	}

	internal void AddCommandHandlers() {
		foreach (PluginCommand cmd in this.Commands) {
			Plugin.Log.Information($"Registering command {cmd.InternalName} as {string.Join(", ", cmd.InvocationNames)}");
			Plugin.CmdManager.AddHandler(cmd.Command, cmd.MainCommandInfo);
			CommandInfo hidden = cmd.AliasCommandInfo;
			foreach (string alt in cmd.Aliases) {
				Plugin.CmdManager.AddHandler(alt, hidden);
			}
		}
	}

	internal void RemoveCommandHandlers() {
		foreach (PluginCommand cmd in this.Commands) {
			Plugin.Log.Information($"Unregistering command {string.Join(", ", cmd.InvocationNames)} for {cmd.InternalName}");
			Plugin.CmdManager.RemoveHandler(cmd.Command);
			foreach (string alt in cmd.Aliases) {
				Plugin.CmdManager.RemoveHandler(alt);
			}
		}
	}

	#region IDisposable Support
	protected virtual void Dispose(bool disposing) {
		if (this.disposed)
			return;

		if (disposing) {
			this.RemoveCommandHandlers();

			foreach (PluginCommand cmd in this.commandList)
				cmd.Dispose();

			this.commandList.Clear();
		}
	}
	public void Dispose() {
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}
	~PluginCommandManager() {
		this.Dispose(false);
	}
	#endregion
}
