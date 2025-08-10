using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using VariableVixen.TinyCmds.Attributes;

using VariableVixen.TinyCmds.Chat;
using VariableVixen.TinyCmds.Internal;
using VariableVixen.TinyCmds.Utils;

namespace VariableVixen.TinyCmds.Commands;

[Command("/execute", "/exec", "/eval", "/evaluate")]
[Summary("Evaluate placeholders in the given command, then run that new command")]
[HelpText(
	"When provided with an input line, this command will evaluate certain placeholders, then execute the new input line.",
	"",
	"If you use one of :@#$%^&*+ as a flag, only placeholders that start with that character will be replaced, as in <:target>."
	+ " This is to allow passing actual placeholders in the event that a command has only partial support.",
	"",
	"All placeholders that identify players or NPCs are supported, as are <class>/<job>. No others are evaluated, due to a lack of consistent/sensible formatting."
	+ " There are also a number of non-vanilla placeholders that are evaluated, which can only be used with this command:",
	"- classname/jobname work like <class>/<job> but only output your (full) class/job name, without level. The suffix \".short\" will output the abbreviated form.",
	"- level/lvl will output only your level as a number, without anything else. The \".short\" suffix will disable zero-padding.",
	"- year, month, day, weekday, yearday, hour, min/minute, sec/second, ms, and am/pm act as you would expect",
	"- month, day, and yearday can take \".0\" as a suffix to be zero-based (January/Sunday is 0, not 1) if desired.",
	"- hour can take \".12\" as a suffix to use 12-hour time if desired. It can also take \".short\" to not zero-pad (6 -> 06) if desired.",
	"",
	"All date/time placeholders can be suffixed with \".utc\" to use UTC/GMT instead of local time."
	+ " All placeholders that output text (instead of numbers) can take the suffixes \".upper\" and \".lower\" to specify their case."
	+ " Multiple suffixes can be used in any order, such that \"hour.12.short\" and \"hour.short.12\" will both work the same.",
	"",
	"As with all commands that send an input line to the server, the -? flag will display the input line before it's run, and the -! flag will display the input line without running it."
)]
public class EvaluatePlaceholders: PluginCommand {
	private readonly string[] leaders = [
		":",
		"@",
		"#",
		"$",
		"%",
		"^",
		"&",
		"*",
		"+",
	];

	private static unsafe string readObjName(GameObject* obj)
		=> obj is null ? "" : obj->NameString;

	private static unsafe string replace(string p) {
		string[] parts = p.ToLower().Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		string flag = parts[0];
		HashSet<string> mods = parts.Skip(1).ToHashSet();
		string value = flag.Trim().ToLower() switch {
			// Custom placeholders - time
			"year"
				=> (mods.Contains("utc") ? DateTime.UtcNow : DateTime.Now).Year.ToString(),
			"month"
				=> (
					(mods.Contains("utc") ? DateTime.UtcNow : DateTime.Now).Month
					+ (mods.Contains("0") ? 0 : 1)
				).ToString("D2"),
			"day"
				=> (
					(mods.Contains("utc") ? DateTime.UtcNow : DateTime.Now).Day
					+ (mods.Contains("0") ? 0 : 1)
				).ToString("D2"),
			"weekday"
				=> (mods.Contains("utc") ? DateTime.UtcNow : DateTime.Now).DayOfWeek.ToString(),
			"yearday"
				=> (
					(mods.Contains("utc") ? DateTime.UtcNow : DateTime.Now).DayOfYear
					+ (mods.Contains("0") ? 0 : 1)
				).ToString("D3"),
			"hour"
				=> (

						((mods.Contains("utc") ? DateTime.UtcNow : DateTime.Now).Hour
						% (mods.Contains("12") ? 12 : 24))

					+ (mods.Contains("12") ? 1 : 0)
				).ToString(mods.Contains("short") ? "D" : "D2"),
			"min" or "minute"
				=> (mods.Contains("utc") ? DateTime.UtcNow : DateTime.Now).Minute.ToString("D2"),
			"sec" or "second"
				=> (mods.Contains("utc") ? DateTime.UtcNow : DateTime.Now).Second.ToString("D2"),
			"ms" or "milli" or "millisec" or "millisecond" or "milliseconds"
				=> (mods.Contains("utc") ? DateTime.UtcNow : DateTime.Now).Millisecond.ToString("D3"),
			"am" or "pm"
				=> (mods.Contains("utc") ? DateTime.UtcNow : DateTime.Now).Hour < 12 ? "am" : "pm",
			// Custom placeholders - personal stats
			"classname" or "jobname"
				=> Plugin.Client.LocalPlayer is null
					? ""
					: mods.Contains("short")
						? Plugin.Client.LocalPlayer.ClassJob.Value.Abbreviation.ToString()
						: Plugin.Client.LocalPlayer.ClassJob.Value.Name.ToString(),
			"level" or "lvl"
				=> Plugin.Client.LocalPlayer is null
					? ""
					: Plugin.Client.LocalPlayer.Level.ToString(mods.Contains("short") ? "D" : "D2"),
			// Vanilla placeholders - targets
			"t" or "target"
			or "tt" or "t2t"
			or "me" or "0"
			or "r" or "reply" // TODO: confirm if this one works or not
			or "1" or "2" or "3" or "4" or "5" or "6" or "7" or "8"
			or "f" or "focus"
			or "lt" or "lasttarget"
			or "le" or "lastenemy"
			or "la" or "lastattacker"
			or "c" or "comp" or "b" or "buddy"
			or "pet"
			or "attack1" or "attack2" or "attack3" or "attack4" or "attack5"
			or "bind1" or "bind2" or "bind3"
			or "ignore1" or "ignore2"
			or "square" or "circle" or "cross" or "triangle"
			or "mo" or "mouse"
			or "e1" or "e2" or "e3" or "e4" or "e5"
				=> readObjName(Framework.Instance()->UIModule->GetPronounModule()->ResolvePlaceholder($"<{p}>", 1, 0)),
			// Vanilla placeholders - other
			"class" or "job"
				=> Plugin.Client.LocalPlayer is null ? "" : Plugin.Client.LocalPlayer.ClassJob.Value.Name.ToString() + "(" + Plugin.Client.LocalPlayer.Level + ")",
			// Not a placeholder
			_ => p,
		};
		return mods.Contains("upper")
			? value.ToUpper()
			: mods.Contains("lower")
			? value.ToLower()
			: value;
	}

	protected override unsafe void Execute(string? command, string rawArguments, FlagMap flags, bool verbose, bool dryRun, ref bool showHelp) {
		string lead = "";
		foreach (string leader in this.leaders) {
			if (flags[leader]) {
				lead = leader;
				break;
			}
		}

		Stopwatch timer = new();
		PronounModule* pronouns = Framework.Instance()->UIModule->GetPronounModule();
		string input = rawArguments;
		int pos1 = input.IndexOf('<');
		int pos2 = input.IndexOf('>');

		Assert(pronouns is not null, "failed to load PronounModule");

		timer.Start();
		while (pos1 >= 0 && pos2 > pos1) {
			if (timer.ElapsedMilliseconds >= 1000) // If you manage to hardlock this loop somehow, at least it won't last forever. If a legit string exceeds this, something's wrong anyway.
				break;

			string pt = input[(pos1 + 1)..pos2];
			if (string.IsNullOrEmpty(lead) || pt.StartsWith(lead)) {
				string p = pt[lead.Length..];
				string r = replace(p);

				if (r != p)
					input = input[0..pos1] + r + input[(pos2 + 1)..];

				pos2 = pos1 + r.Length;
				Logger.Info($"\"{p}\" -> \"{r}\": ({pos1}, {pos2}) {input}");
			}

			if (pos2 + 3 > input.Length)
				break;

			pos1 = input.IndexOf('<', pos2);
			pos2 = input.IndexOf('>', pos1 + 1);
			Logger.Info($"Next scan resolved ({pos1}, {pos2})");
		}

		ChatUtil.SendChatlineToServer(input, verbose || dryRun, dryRun);
	}
}
