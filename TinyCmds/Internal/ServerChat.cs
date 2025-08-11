using System;
using System.Runtime.InteropServices;

using Dalamud.Game;

using FFXIVClientStructs.FFXIV.Client.System.String;

using FFXIVClientStructs.FFXIV.Client.UI;

namespace VariableVixen.TinyCmds.Internal;

internal class ServerChat {

	public unsafe void SendMessage(string message) {
		UIModule* uiModule = UIModule.Instance();
		if (uiModule is null)
			throw new InvalidOperationException("Could not access UIModule instance", new NullReferenceException("Framework instance returned null for UIModule"));

		if (string.IsNullOrWhiteSpace(message))
			throw new ArgumentException("message is empty", nameof(message));

		using Utf8String utf8 = new(message);

		if (utf8.Length > 500)
			throw new ArgumentException("message is longer than 500 bytes", nameof(message));

		uiModule->ProcessChatBoxEntry(&utf8);
	}

}
