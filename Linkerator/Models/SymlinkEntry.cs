/*
Copyright © 2025 dontstopmeow <public-static@hotmail.com>

This file is part of Linkerator.

Linkerator is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

Linkerator is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with Linkerator. If not, see <https://www.gnu.org/licenses/>.
*/
namespace Linkerator.Models
{
    public class SymlinkEntry : SetupEntry
    {
        public string TargetPath { get; }

        public SymlinkEntry(string name, string fullPath, string targetPath)
            : base(name, fullPath, EntryType.Symlink)
        {
            TargetPath = targetPath;
        }
    }
}
