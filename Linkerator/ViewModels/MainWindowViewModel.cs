/*
Copyright © 2025 dontstopmeow <public-static@hotmail.com>

This file is part of Linkerator.

Linkerator is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

Linkerator is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with Linkerator. If not, see <https://www.gnu.org/licenses/>.
*/
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linkerator.Evaluation;
using Linkerator.Models;
using Linkerator.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Linkerator.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private readonly IFolderDialogService _dialogService;
        private readonly IFolderMonitorService _monitorService;
        private readonly ISymlinkInteractor _symlinkInteractor;
        private CancellationTokenSource? _monitoringDebounceCts;
        private string _lastMonitoredPath;
        private readonly SetupConfig _setupConfig;
        public string SourcePath { get; init; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SetupAllowed))]
        private string setupAtPath;
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SetupAllowed))]
        private SetupPlatformEntry selectedPlatform;
        public bool SetupAllowed => SelectedPlatform != _plaftormPlaceholder && Directory.Exists(SetupAtPath) && (SetupAtPath != SourcePath) && (IsNotSubdirectory(SourcePath, SetupAtPath));

        public ObservableCollection<SetupPlatformEntry> AvailablePlatforms { get; } = new();

        public ObservableCollection<EntryRowViewModel> EntryRows { get; } = [];

        private readonly SetupPlatformEntry _plaftormPlaceholder = new SetupPlatformEntry() { Name = "Select platform..." };

        public MainWindowViewModel(IFolderDialogService dialogService, IFolderMonitorService monitorService, ISymlinkInteractor symlinkInteractor)
        {
            setupAtPath = _lastMonitoredPath = String.Empty;
            SourcePath = String.Empty;
            selectedPlatform = _plaftormPlaceholder;
            _dialogService = dialogService;
            _monitorService = monitorService;
            _symlinkInteractor = symlinkInteractor;
            _setupConfig = LoadConfig();
            AvailablePlatforms.Add(_plaftormPlaceholder);

            if (_setupConfig.PlatformEntries.Any())
            {
                foreach (var platform in _setupConfig.PlatformEntries)
                    AvailablePlatforms.Add(platform);

                SourcePath = Path.GetFullPath(_setupConfig.SourcePath);
            }
        }

        [RelayCommand]
        private async Task BrowseAsync()
        {
            var result = await _dialogService.OpenFolderPickerAsync();
            if (string.IsNullOrEmpty(result))
                return;

            SetupAtPath = result;
        }

        [RelayCommand]
        private void Setup()
        {
            if (string.IsNullOrWhiteSpace(SetupAtPath) || !SetupAllowed)
                return;

            foreach (var row in EntryRows)
            {
                var entry = row.EntryRow;
                bool entrySourceValid = (entry.Source.Type & (EntryType.File | EntryType.Folder)) != EntryType.None;
                if (!entrySourceValid || entry.Target.Type != EntryType.Missing)
                    continue;

                string? enclosingPath = Path.GetDirectoryName(
                    entry.Target.FullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                bool written = false;
                if (!string.IsNullOrEmpty(enclosingPath) && !Directory.Exists(enclosingPath))
                {
                    Directory.CreateDirectory(enclosingPath);
                    written = true;
                }
                File.AppendAllText("log.txt", $"Writing enclosing path ({written}): '{enclosingPath}' for path \n{entry.Source.FullPath}\n\n");

                _symlinkInteractor.CreateSymbolicLink(entry.Target.FullPath, entry.Source.FullPath, entry.Source.Type == EntryType.Folder);
            }
        }

        partial void OnSelectedPlatformChanged(SetupPlatformEntry value)
        {
            RefreshEntries();
        }

        partial void OnSetupAtPathChanged(string value)
        {
            DebounceSetupMonitoring(value);
        }

        private void DebounceSetupMonitoring(string newPath)
        {
            _monitoringDebounceCts?.Cancel();
            _monitoringDebounceCts = new CancellationTokenSource();
            var token = _monitoringDebounceCts.Token;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500, token);
                    if (!token.IsCancellationRequested)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            FinalizeMonitoringForPath(newPath);
                        });
                    }
                }
                catch (TaskCanceledException) { }
            });
        }

        public void FinalizeMonitoringForPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                _monitorService.StopMonitoring();
                _lastMonitoredPath = string.Empty;
                return;
            }

            if (path == _lastMonitoredPath)
                return;

            _monitorService.StartMonitoring(path, RefreshEntries);
            _lastMonitoredPath = path;

            RefreshEntries();
        }

        private SetupConfig LoadConfig()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "config.xml");
            if (!File.Exists(path))
                throw new FileNotFoundException("Cant't find config.xml!");

            var serializer = new XmlSerializer(typeof(SetupConfig));
            using var stream = File.OpenRead(path);
            var config = (SetupConfig?)serializer.Deserialize(stream);

            if (config == null)
                throw new FileLoadException("Config file is incorrect!");
            return config;
        }

        private void RefreshEntries()
        {
            Dispatcher.UIThread.Post((Action)(() =>
            {
                EntryRows.Clear();

                if (SelectedPlatform == _plaftormPlaceholder)
                    return;

                if (!Directory.Exists(SetupAtPath))
                    return;

                foreach (var rule in SelectedPlatform.MirrorRules)
                {
                    var destinationPath = Path.Combine(SetupAtPath, rule.Destination);
                    var originPath = Path.GetFullPath(Path.Combine(_setupConfig.SourcePath, rule.Origin));

                    var source = CreateEntry(originPath, rule.Origin);
                    var target = CreateEntry(destinationPath, rule.Destination);

                    var row = new EntryRow(source, target);
                    var visual = RowStateEvaluator.Evaluate(row);
                    EntryRows.Add(new EntryRowViewModel(row, visual));
                }
            }));
        }

        private SetupEntry CreateEntry(string path, string name)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return new SetupEntry(name, path, EntryType.Missing);

            var attr = File.GetAttributes(path);
            var isSymlink = _symlinkInteractor.IsSymbolicLink(path);

            if (isSymlink)
            {
                try
                {
                    string? target = _symlinkInteractor.GetSymlinkTarget(path);
                    return new SymlinkEntry(name, path, target ?? string.Empty);
                }
                catch
                {
                    return new SetupEntry(name, path, EntryType.Symlink);
                }
            }

            return attr.HasFlag(FileAttributes.Directory)
                ? new SetupEntry(name, path, EntryType.Folder)
                : new SetupEntry(name, path, EntryType.File);
        }

        public static bool IsNotSubdirectory(string parentPath, string childPath)
        {
            string fullParent = Path.GetFullPath(parentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            string fullChild = Path.GetFullPath(childPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fullParent = fullParent.ToLowerInvariant();
                fullChild = fullChild.ToLowerInvariant();
            }

            // Ensure trailing separator to prevent false positives
            fullParent += Path.DirectorySeparatorChar;

            return !fullChild.StartsWith(fullParent, StringComparison.Ordinal);
        }
    }
}