/*
Copyright © 2025 dontstopmeow <public-static@hotmail.com>

This file is part of Linkerator.

Linkerator is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

Linkerator is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with Linkerator. If not, see <https://www.gnu.org/licenses/>.
*/
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Linkerator.Models;

namespace Linkerator.ViewModels
{
    public partial class EntryRowViewModel : ObservableObject
    {
        public EntryRow EntryRow { get; }

        [ObservableProperty]
        private IBrush sourceBackground;

        [ObservableProperty]
        private IBrush targetBackground;

        [ObservableProperty]
        private string indicator;

        [ObservableProperty]
        private string? sourceTooltip;

        [ObservableProperty]
        private string? targetTooltip;

        public EntryRowViewModel(EntryRow entryRow, RowVisualModel visual)
        {
            EntryRow = entryRow;
            SourceBackground = visual.SourceBrush;
            TargetBackground = visual.TargetBrush;
            Indicator = visual.IndicatorSymbol;
            SourceTooltip = visual.SourceTooltip;
            TargetTooltip = visual.TargetTooltip;
        }
    }
}
