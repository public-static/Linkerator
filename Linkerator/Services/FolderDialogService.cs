/*
Copyright © 2025 dontstopmeow <public-static@hotmail.com>

This file is part of Linkerator.

Linkerator is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

Linkerator is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with Linkerator. If not, see <https://www.gnu.org/licenses/>.
*/
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using Linkerator.Services.Interfaces;

namespace Linkerator.Services
{
    internal class FolderDialogService : IFolderDialogService
    {
        public async Task<string?> OpenFolderPickerAsync()
        {
            // Get the TopLevel from the current application lifetime.
            var topLevel = GetTopLevel();
            if (topLevel == null)
            {
                // Handle the case where TopLevel is not available
                return null;
            }

            // Start async operation to open the dialog.
            var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Setup Folder",
                AllowMultiple = false
            });

            if (folders.Count >= 1)
            {
                // We got a folder. Use the Path property of the IStorageFolder
                // and get its local path.
                return folders[0].Path.LocalPath;
            }

            // User cancelled the dialog
            return null;
        }

        private TopLevel? GetTopLevel()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return desktop.MainWindow;
            }

            if (Application.Current?.ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            {
                // This is used for mobile platforms
                var visualRoot = singleView.MainView;
                if (visualRoot is TopLevel topLevel)
                {
                    return topLevel;
                }
            }

            return null;
        }
    }

}
