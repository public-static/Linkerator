/*
Copyright © 2025 dontstopmeow <public-static@hotmail.com>

This file is part of Linkerator.

Linkerator is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

Linkerator is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with Linkerator. If not, see <https://www.gnu.org/licenses/>.
*/
using Avalonia.Media;

namespace Linkerator.ViewModels
{
    public class RowVisualModel
    {
        public IBrush SourceBrush { get; init; }
        public IBrush TargetBrush { get; init; }
        public string IndicatorSymbol { get; init; } = string.Empty;
        public string? SourceTooltip { get; init; }
        public string? TargetTooltip { get; init; }

        public RowVisualModel(
            IBrush sourceBrush,
            IBrush targetBrush,
            string indicatorSymbol = "",
            string? sourceTooltip = null,
            string? targetTooltip = null)
        {
            SourceBrush = sourceBrush;
            TargetBrush = targetBrush;
            IndicatorSymbol = indicatorSymbol;
            SourceTooltip = sourceTooltip;
            TargetTooltip = targetTooltip;
        }
    }
}
