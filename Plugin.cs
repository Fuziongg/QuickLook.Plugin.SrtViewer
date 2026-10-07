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

using QuickLook.Common.Plugin;
using System;
using System.IO;
using System.Windows;

namespace QuickLook.Plugin.SrtViewer;

public sealed class Plugin : IViewer
{
    // above the built-in TextViewer (-5), which would otherwise show the raw file
    public int Priority => 0;

    public void Init()
    {
    }

    public bool CanHandle(string path) =>
        !Directory.Exists(path) && path.EndsWith(".srt", StringComparison.OrdinalIgnoreCase);

    public void Prepare(string path, ContextObject context)
    {
        context.PreferredSize = new Size(860, 620);
    }

    public void View(string path, ContextObject context)
    {
        var text = SrtParser.Read(path, out var encoding);
        var cues = SrtParser.Parse(text);

        context.Theme = Themes.Dark;
        context.Title = Path.GetFileName(path);
        context.ViewerContent = new SrtPanel(cues, encoding);
        context.IsBusy = false;
    }

    public void Cleanup()
    {
    }
}
