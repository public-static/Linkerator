/*
Copyright © 2025 dontstopmeow <public-static@hotmail.com>

This file is part of Linkerator.

Linkerator is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

Linkerator is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with Linkerator. If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.IO;
using Linkerator.Services.Interfaces;

namespace Linkerator.Services
{
    public class FolderMonitorService : IFolderMonitorService
    {
        private FileSystemWatcher? _watcher;
        private Action? _onChanged;

        public void StartMonitoring(string path, Action onChanged)
        {
            StopMonitoring();

            _onChanged = onChanged;
            _watcher = new FileSystemWatcher(path)
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Attributes
            };

            _watcher.Created += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.Renamed += OnChanged;
            _watcher.Changed += OnChanged;
        }

        public void StopMonitoring()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnChanged;
                _watcher.Deleted -= OnChanged;
                _watcher.Renamed -= OnChanged;
                _watcher.Changed -= OnChanged;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        private void OnChanged(object? sender, FileSystemEventArgs e)
        {
            _onChanged?.Invoke();
        }

        public void Dispose() => StopMonitoring();
    }
}
