// Copyright © 2026 Fuziongg
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace QuickLook.Plugin.SrtViewer;

public sealed class Cue
{
    public int Number { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public string Text { get; set; }

    public string StartText => Format(Start);
    public string EndText => Format(End);

    private static string Format(TimeSpan t) =>
        $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00},{t.Milliseconds:000}";
}

public static class SrtParser
{
    private static readonly Regex Timing = new(
        @"(\d+):(\d{1,2}):(\d{1,2})[,.](\d{1,3})\s*-->\s*(\d+):(\d{1,2}):(\d{1,2})[,.](\d{1,3})",
        RegexOptions.Compiled);

    public static List<Cue> Parse(string text)
    {
        var cues = new List<Cue>();
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var m = Timing.Match(lines[i]);
            if (!m.Success) continue;

            // the number line is optional in the wild, so fall back to position
            var number = cues.Count + 1;
            if (i > 0 && int.TryParse(lines[i - 1].Trim(), out var n)) number = n;

            var body = new StringBuilder();
            var j = i + 1;
            for (; j < lines.Length && lines[j].Trim().Length > 0; j++)
            {
                if (body.Length > 0) body.Append('\n');
                body.Append(lines[j].TrimEnd());
            }

            cues.Add(new Cue
            {
                Number = number,
                Start = Time(m, 1),
                End = Time(m, 5),
                Text = body.ToString(),
            });
            i = j - 1;
        }

        return cues;
    }

    private static TimeSpan Time(Match m, int g)
    {
        var ms = m.Groups[g + 3].Value.PadRight(3, '0');
        return new TimeSpan(0, int.Parse(m.Groups[g].Value), int.Parse(m.Groups[g + 1].Value),
            int.Parse(m.Groups[g + 2].Value), int.Parse(ms));
    }

    public static string Read(string path, out Encoding encoding)
    {
        var bytes = File.ReadAllBytes(path);

        var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
                     || bytes.Length >= 2 && (bytes[0] == 0xFF && bytes[1] == 0xFE || bytes[0] == 0xFE && bytes[1] == 0xFF);
        if (hasBom)
        {
            using var reader = new StreamReader(new MemoryStream(bytes), true);
            var s = reader.ReadToEnd();
            encoding = reader.CurrentEncoding;
            return s;
        }

        try
        {
            encoding = new UTF8Encoding(false, true);
            return encoding.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            // most non-UTF-8 subtitles out there are Windows-1252
            encoding = Encoding.GetEncoding(1252);
            return encoding.GetString(bytes);
        }
    }
}
