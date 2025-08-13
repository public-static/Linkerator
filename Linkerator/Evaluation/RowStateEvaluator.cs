/*
Copyright © 2025 dontstopmeow <public-static@hotmail.com>

This file is part of Linkerator.

Linkerator is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

Linkerator is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with Linkerator. If not, see <https://www.gnu.org/licenses/>.
*/
using Avalonia.Media;
using System.IO;
using Linkerator.Models;
using Linkerator.ViewModels;

namespace Linkerator.Evaluation
{
    public class RowStateEvaluator
    {
        private static readonly IBrush FolderBrush = Brushes.Goldenrod;
        private static readonly IBrush FileBrush = Brushes.DarkGoldenrod;
        private static readonly IBrush MissingBrush = Brushes.LightGray;
        private static readonly IBrush SymlinkBrush = Brushes.LightSteelBlue;
        private static readonly IBrush MatchBrush = Brushes.PaleGreen;
        private static readonly IBrush MismatchBrush = Brushes.IndianRed;

        public static RowVisualModel Evaluate(EntryRow row)
        {
            var source = row.Source;
            var target = row.Target;

            var sourceBrush = GetBrush(source);
            var targetBrush = GetBrush(target);
            var indicator = string.Empty;

            var sourceTooltip = TooltipFromEntry(source);
            var targetTooltip = TooltipFromEntry(target);

            if (target is SymlinkEntry symlink)
            {
                if (source.Type == EntryType.Missing)
                {
                    targetBrush = SymlinkBrush;
                    indicator = "";
                }
                else if (PathsEqual(symlink.TargetPath, source.FullPath))
                {
                    sourceBrush = targetBrush = MatchBrush;
                    indicator = "✔";
                }
                else
                {
                    sourceBrush = targetBrush = MismatchBrush;
                    indicator = "✖";
                }
            }

            return new RowVisualModel(
                sourceBrush,
                targetBrush,
                indicator,
                sourceTooltip,
                targetTooltip);
        }

        private static IBrush GetBrush(SetupEntry entry) => entry.Type switch
        {
            EntryType.File => FileBrush,
            EntryType.Folder => FolderBrush,
            EntryType.Symlink => SymlinkBrush,
            EntryType.Missing => MissingBrush,
            _ => Brushes.Transparent
        };

        private static bool PathsEqual(string a, string b)
        {
            return Path.GetFullPath(a.TrimEnd(Path.DirectorySeparatorChar))
                .Equals(Path.GetFullPath(b.TrimEnd(Path.DirectorySeparatorChar)), System.StringComparison.OrdinalIgnoreCase);
        }

        private static string TooltipFromEntry(SetupEntry entry) => entry.Type switch
        {
            EntryType.File => "File",
            EntryType.Folder => "Folder",
            EntryType.Missing => "Missing",
            EntryType.Symlink => entry is SymlinkEntry s ? $"Symlink → {s.TargetPath}" : "Symlink",
            _ => ""
        };
    }
}
