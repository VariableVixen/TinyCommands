using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Dalamud.Hooking;

namespace VariableVixen.TinyCmds.Internal;

internal abstract class GameFunctionBase<T> where T : Delegate {
	private readonly nint addr = nint.Zero;
	public nint Address => this.addr;
	private T? function;
	public bool Valid => this.function is not null || this.Address != nint.Zero;
	public T? Delegate {
		get {
			if (this.function is not null)
				return this.function;
			if (this.Address != nint.Zero) {
				this.function = Marshal.GetDelegateForFunctionPointer<T>(this.Address);
				return this.function;
			}
			Logger.Error($"{this.GetType().Name} invocation FAILED: no pointer available");
			return null;
		}
	}
	protected GameFunctionBase(string sig, int offset = 0) {
		if (Plugin.Scanner.TryScanText(sig, out this.addr)) {
			this.addr += offset;
			ulong totalOffset = (ulong)this.Address.ToInt64() - (ulong)Plugin.Scanner.Module.BaseAddress.ToInt64();
			Logger.Info($"{this.GetType().Name} loaded; address = 0x{this.Address.ToInt64():X16}, base memory offset = 0x{totalOffset:X16}");
		}
		else {
			Logger.Warning($"{this.GetType().Name} FAILED, could not find address from signature: ${sig.ToUpper()}");
		}
	}
	[SuppressMessage("Reliability", "CA2020:Prevent from behavioral change", Justification = "If that explodes, we SHOULD be throwing")]
	protected GameFunctionBase(nint address, int offset = 0) {
		this.addr = address + offset;
		ulong totalOffset = (ulong)this.Address.ToInt64() - (ulong)Plugin.Scanner.Module.BaseAddress.ToInt64();
		Logger.Info($"{this.GetType().Name} loaded; address = 0x{this.Address.ToInt64():X16}, base memory offset = 0x{totalOffset:X16}");
	}

	public dynamic? Invoke(params dynamic[] parameters)
		=> this.Delegate?.DynamicInvoke(parameters);

	public Hook<T> Hook(T handler) => Plugin.Interop.HookFromAddress(this.Address, handler);
}
