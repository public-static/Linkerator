/*
Copyright © 2025 dontstopmeow <public-static@hotmail.com>

This file is part of Linkerator.

Linkerator is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

Linkerator is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with Linkerator. If not, see <https://www.gnu.org/licenses/>.
*/

using Linkerator.Services.Interfaces;

namespace Linkerator.MacOS.Services
{
    public class UnixSymlinkInteractor : ISymlinkInteractor
    {

        public bool CreateSymbolicLink(string linkPath, string targetPath, bool isDirectory)
        {
            if (string.IsNullOrWhiteSpace(linkPath)) throw new ArgumentNullException(nameof(linkPath));
            if (string.IsNullOrWhiteSpace(targetPath)) throw new ArgumentNullException(nameof(targetPath));

            try
            {
                if (File.Exists(linkPath) || Directory.Exists(linkPath))
                    return false;

                if (isDirectory)
                    Directory.CreateSymbolicLink(linkPath, targetPath);
                else
                    File.CreateSymbolicLink(linkPath, targetPath);

                return true;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public string? GetSymlinkTarget(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

            try
            {
                var fileInfo = new FileInfo(path);
                var link = fileInfo.LinkTarget;

                if (!string.IsNullOrEmpty(link))
                    return link;

                var di = new DirectoryInfo(path);
                link = di.LinkTarget;
                return string.IsNullOrEmpty(link) ? null : link;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool IsSymbolicLink(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

            try
            {
                var fi = new FileInfo(path);
                if (!string.IsNullOrEmpty(fi.LinkTarget))
                    return true;

                var di = new DirectoryInfo(path);
                if (!string.IsNullOrEmpty(di.LinkTarget))
                    return true;

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
