using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace VariableVixen.TinyCmds;

public static class TimeSpec {
	/// <summary>
	/// Matches digits followed by `h`, `m`, or `s`, with the final set of digits not requiring a suffix. Only one group.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("GeneratedRegex", "SYSLIB1045:Convert to 'GeneratedRegexAttribute'.", Justification = "User's culture is relevant")]
	public static readonly Regex Matcher = new(@"^((?:\d+[hms]?)+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
	public static bool TryParse(string spec, out uint hours, out uint minutes, out uint seconds) {
		hours = 0;
		minutes = 0;
		seconds = 0;
		try {
			Match match = Matcher.Match(spec.ToLower());
			if (match.Success) {
				foreach (string piece in match.Groups[1].Captures.Cast<Capture>().Select(c => c.Value)) { // apparently it's `object`s for some reason???
					switch (piece[^1]) {
						case 'h':
							hours += uint.Parse(piece[..^1]);
							break;
						case 'm':
							minutes += uint.Parse(piece[..^1]);
							break;
						case 's':
							seconds += uint.Parse(piece[..^1]);
							break;
						default:
							minutes += uint.Parse(piece);
							break;
					}
				}
				return true;
			}
			return false;
		}
		catch {
			return false;
		}
	}
	public static uint RawHours(string spec) => TryParse(spec, out uint hours, out _, out _) ? hours : 0;
	public static uint RawMinutes(string spec) => TryParse(spec, out uint hours, out uint minutes, out _) ? (hours * 60) + minutes : 0;
	public static uint RawSeconds(string spec) => TryParse(spec, out uint hours, out uint minutes, out uint seconds) ? (((hours * 60) + minutes) * 60) + seconds : 0;

	public static void Normalise(ref uint hours, ref uint minutes, ref uint seconds) {
		if (seconds >= 60) {
			minutes += (uint)Math.Floor(seconds / 60f);
			seconds %= 60;
		}
		if (minutes >= 60) {
			hours += (uint)Math.Floor(minutes / 60f);
			minutes %= 60;
		}
	}

	public static string ClockDisplay(uint hours, uint minutes, uint seconds) {
		Normalise(ref hours, ref minutes, ref seconds);
		string display = string.Empty;
		if (hours > 0)
			display = $"{hours}:";
		return $"{display}{minutes:D2}:{seconds:D2}";
	}
	public static string HMS(uint hours, uint minutes, uint seconds, bool forceSeconds) {
		Normalise(ref hours, ref minutes, ref seconds);
		string display = string.Empty;
		if (hours > 0)
			display = $"{hours}h";
		display += $"{minutes}m";
		if (forceSeconds || seconds > 0 || minutes == 0)
			display += $"{seconds}s";
		return display;
	}
}
