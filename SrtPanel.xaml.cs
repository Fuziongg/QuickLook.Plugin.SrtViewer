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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace QuickLook.Plugin.SrtViewer;

public partial class SrtPanel : UserControl
{
    public SrtPanel(List<Cue> cues, Encoding encoding)
    {
        InitializeComponent();
        List.ItemsSource = cues;

        var length = cues.Count > 0 ? cues.Max(c => c.End) : TimeSpan.Zero;
        Summary.Text = $"{cues.Count} subtitles  ·  {(int)length.TotalHours:00}:{length.Minutes:00}:{length.Seconds:00}  ·  {EncodingName(encoding)}";
    }

    private static string EncodingName(Encoding e) => e.CodePage switch
    {
        65001 => "UTF-8",
        1200 => "UTF-16 LE",
        1201 => "UTF-16 BE",
        _ => e.WebName,
    };
}

public sealed class CueText : TextBlock
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(string), typeof(CueText),
        new PropertyMetadata(null, (d, e) => ((CueText)d).Build((string)e.NewValue)));

    // <i>, <b>, <u> are kept; <font> and {\an8}-style overrides are dropped
    private static readonly Regex Token = new(@"<\s*(/?)\s*([a-z]+)[^>]*>|\{[^}]*\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    private void Build(string text)
    {
        Inlines.Clear();
        if (string.IsNullOrEmpty(text)) return;

        bool italic = false, bold = false, underline = false;
        var pos = 0;
        foreach (Match m in Token.Matches(text))
        {
            Add(text.Substring(pos, m.Index - pos), italic, bold, underline);
            pos = m.Index + m.Length;

            var on = m.Groups[1].Value != "/";
            switch (m.Groups[2].Value.ToLowerInvariant())
            {
                case "i": italic = on; break;
                case "b": bold = on; break;
                case "u": underline = on; break;
            }
        }
        Add(text.Substring(pos), italic, bold, underline);
    }

    private void Add(string s, bool italic, bool bold, bool underline)
    {
        var parts = s.Split('\n');
        for (var k = 0; k < parts.Length; k++)
        {
            if (k > 0) Inlines.Add(new LineBreak());
            if (parts[k].Length == 0) continue;

            var run = new Run(parts[k]);
            if (italic) run.FontStyle = FontStyles.Italic;
            if (bold) run.FontWeight = FontWeights.SemiBold;
            if (underline) run.TextDecorations = System.Windows.TextDecorations.Underline;
            Inlines.Add(run);
        }
    }
}
