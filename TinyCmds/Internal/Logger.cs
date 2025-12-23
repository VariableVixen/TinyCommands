using System;
using System.Diagnostics;
using System.Threading;

namespace VariableVixen.TinyCmds.Internal;

internal static class Logger {
	private static string msgPrefix {
		get {
			string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
			Thread runner = Thread.CurrentThread;
			string threadDesc = $"{runner.Name ?? "unknown thread"}#{runner.ManagedThreadId}";
			return $"[{timestamp}/{threadDesc}]";
		}
	}
	[Conditional("DEBUG")]
	internal static void Debug(string tmpl, params object[] args) => Plugin.Log.Debug($"{msgPrefix} {string.Format(tmpl, args)}");
	[Conditional("DEBUG")]
	internal static void Debug(Exception ex, string tmpl, params object[] args) => Plugin.Log.Debug(ex, $"{msgPrefix} {string.Format(tmpl, args)}");

	[Conditional("DEBUG")]
	internal static void Verbose(string tmpl, params object[] args) => Plugin.Log.Verbose($"{msgPrefix} {string.Format(tmpl, args)}");
	[Conditional("DEBUG")]
	internal static void Verbose(Exception ex, string tmpl, params object[] args) => Plugin.Log.Verbose(ex, $"{msgPrefix} {string.Format(tmpl, args)}");

	internal static void Info(string tmpl, params object[] args) => Plugin.Log.Information($"{msgPrefix} {string.Format(tmpl, args)}");
	internal static void Info(Exception ex, string tmpl, params object[] args) => Plugin.Log.Information(ex, $"{msgPrefix} {string.Format(tmpl, args)}");

	internal static void Warning(string tmpl, params object[] args) => Plugin.Log.Warning($"{msgPrefix} {string.Format(tmpl, args)}");
	internal static void Warning(Exception ex, string tmpl, params object[] args) => Plugin.Log.Warning(ex, $"{msgPrefix} {string.Format(tmpl, args)}");

	internal static void Error(string tmpl, params object[] args) => Plugin.Log.Error($"{msgPrefix} {string.Format(tmpl, args)}");
	internal static void Error(Exception ex, string tmpl, params object[] args) => Plugin.Log.Error(ex, $"{msgPrefix} {string.Format(tmpl, args)}");

	internal static void Fatal(string tmpl, params object[] args) => Plugin.Log.Fatal($"{msgPrefix} {string.Format(tmpl, args)}");
	internal static void Fatal(Exception ex, string tmpl, params object[] args) => Plugin.Log.Fatal(ex, $"{msgPrefix} {string.Format(tmpl, args)}");

}
