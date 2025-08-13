/*
Copyright © 2025 dontstopmeow <public-static@hotmail.com>

This file is part of Linkerator.

Linkerator is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

Linkerator is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with Linkerator. If not, see <https://www.gnu.org/licenses/>.
*/
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Text;
using Linkerator.Services.Interfaces;

namespace Linkerator.Windows.Services
{
    public class WindowsSymlinkInteractor : ISymlinkInteractor
    {
        private static class FileInteraction
        {
            public enum GenericAccessRights : uint
            {
                GENERIC_ALL = 0x10000000,
                GENERIC_EXECUTE = 0x20000000,
                GENERIC_WRITE = 0x40000000,
                GENERIC_READ = 0x80000000
            }

            public enum FileShareMode : uint
            {
                NONE = 0x00,
                FILE_SHARE_DELETE = 0x04,
                FILE_SHARE_READ = 0x01,
                FILE_SHARE_WRITE = 0x02,
            }

            public enum CreationDisposition : uint
            {
                CREATE_ALWAYS = 2,
                CREATE_NEW = 1,
                OPEN_ALWAYS = 4,
                OPEN_EXISTING = 3,
                TRUNCATE_EXISTING = 5
            }

            public enum FileFlagsAndAttributes : uint // Incomplete!
            {
                FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000,
                FILE_FLAG_BACKUP_SEMANTICS = 0x02000000
            }

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern SafeFileHandle CreateFile(
                string lpFileName,
                GenericAccessRights dwDesiredAccess,
                FileShareMode dwShareMode,
                IntPtr lpSecurityAttributes,
                CreationDisposition dwCreationDisposition,
                FileFlagsAndAttributes dwFlagsAndAttributes,
                IntPtr hTemplateFile);
        }

        private static class DeviceInteraction
        {
            public const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
            public enum IOControlCodes : uint // Incomplete!
            {
                FSCTL_GET_REPARSE_POINT = 589992U,
                FSCTL_SET_REPARSE_POINT = 589988U
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct ReparseDataBuffer
            {
                public uint ReparseTag;
                public ushort ReparseDataLength;
                private ushort Reserved;
                public ushort SubstituteNameOffset;
                public ushort SubstituteNameLength;
                public ushort PrintNameOffset;
                public ushort PrintNameLength;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16368 / 2)]
                internal string PathBuffer;
            }

            public const uint ReparseDataBufferLength = 20;

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool DeviceIoControl(
                SafeFileHandle hDevice,
                IOControlCodes dwIoControlCode,
                IntPtr lpInBuffer,
                uint nInBufferSize,
                [Out] IntPtr lpOutBuffer,
                uint nOutBufferSize,
                out uint lpBytesReturned,
                [In, Out] IntPtr lpOverlapped);
        }

        private enum Win32Errors : int //Incomplete!
        {
            ERROR_NOT_A_REPARSE_POINT = 0x00001126
        }

        public bool CreateSymbolicLink(string linkPath, string targetPath, bool isDirectory)
        {
            if (Directory.Exists(linkPath))
                return false;

            if (!Directory.Exists(targetPath))
                return false;

            Directory.CreateDirectory(linkPath);

            var targetPathWithPrefix = @"\??\" + Path.GetFullPath(targetPath);
            var targetPathWithPrefixByteLength = targetPathWithPrefix.Length * 2;

            using (var handle = FileInteraction.CreateFile(
                linkPath,
                FileInteraction.GenericAccessRights.GENERIC_WRITE,
                FileInteraction.FileShareMode.FILE_SHARE_READ,
                IntPtr.Zero,
                FileInteraction.CreationDisposition.OPEN_EXISTING,
                FileInteraction.FileFlagsAndAttributes.FILE_FLAG_OPEN_REPARSE_POINT | FileInteraction.FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero))
            {
                if (Marshal.GetLastWin32Error() != 0 || handle.IsInvalid)
                    throw new IOException($"Can't open file '${linkPath}' : \n{Marshal.GetLastPInvokeErrorMessage()}");

                var reparseDataBuffer = new DeviceInteraction.ReparseDataBuffer()
                {
                    ReparseTag = DeviceInteraction.IO_REPARSE_TAG_MOUNT_POINT,
                    ReparseDataLength = (ushort)(targetPathWithPrefixByteLength + 8 + UnicodeEncoding.CharSize * 2),
                    SubstituteNameOffset = 0,
                    SubstituteNameLength = (ushort)targetPathWithPrefixByteLength,
                    PrintNameOffset = (ushort)(targetPathWithPrefixByteLength + UnicodeEncoding.CharSize),
                    PrintNameLength = 0,
                    PathBuffer = targetPathWithPrefix
                };

                var bufferSize = Marshal.SizeOf(reparseDataBuffer);
                IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    Marshal.StructureToPtr(reparseDataBuffer, buffer, false);

                    bool result = DeviceInteraction.DeviceIoControl(
                        handle,
                        DeviceInteraction.IOControlCodes.FSCTL_SET_REPARSE_POINT,
                        buffer,
                        (uint)(targetPathWithPrefixByteLength + DeviceInteraction.ReparseDataBufferLength),
                        IntPtr.Zero,
                        0,
                        out _,
                        IntPtr.Zero);

                    if (!result)
                    {
                        throw new IOException($"Failed to create junction point for '{linkPath}' -> '{targetPath}' : \n{Marshal.GetLastPInvokeErrorMessage()}");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            return true;
        }

        public string? GetSymlinkTarget(string path)
        {
            GetSymbolicLinkInternal(path, out var target);
            return target;
        }

        public bool IsSymbolicLink(string path) => GetSymbolicLinkInternal(path, out _);

        private bool GetSymbolicLinkInternal(string path, out string symlinkTarget)
        {
            symlinkTarget = string.Empty;

            if (!Directory.Exists(path))
                return false;

            var longPath = @"\\?\" + Path.GetFullPath(path);
            var attr = File.GetAttributes(longPath);
            if (!attr.HasFlag(FileAttributes.ReparsePoint))
                return false;

            using (var handle = FileInteraction.CreateFile(
                path,
                FileInteraction.GenericAccessRights.GENERIC_READ,
                FileInteraction.FileShareMode.FILE_SHARE_READ,
                IntPtr.Zero,
                FileInteraction.CreationDisposition.OPEN_EXISTING,
                FileInteraction.FileFlagsAndAttributes.FILE_FLAG_OPEN_REPARSE_POINT | FileInteraction.FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero))
            {
                if (Marshal.GetLastWin32Error() != 0 || handle.IsInvalid)
                    throw new IOException($"Can't open file '{path}' : \n{Marshal.GetLastPInvokeErrorMessage()}");

                var bufferSize = Marshal.SizeOf<DeviceInteraction.ReparseDataBuffer>();
                var buffer = Marshal.AllocHGlobal(bufferSize);

                try
                {
                    bool result = DeviceInteraction.DeviceIoControl(
                        handle,
                        DeviceInteraction.IOControlCodes.FSCTL_GET_REPARSE_POINT,
                        IntPtr.Zero,
                        0,
                        buffer,
                        (uint)bufferSize,
                        out uint _,
                        IntPtr.Zero);

                    if (!result)
                    {
                        if (Marshal.GetLastWin32Error() == (int)Win32Errors.ERROR_NOT_A_REPARSE_POINT)
                            return false;
                        else
                            throw new IOException($"Error when querying status of '{path}' : \n{Marshal.GetLastPInvokeErrorMessage()}");
                    }

                    var bufferConverted = Marshal.PtrToStructure<DeviceInteraction.ReparseDataBuffer>(buffer);

                    if (bufferConverted.ReparseTag == DeviceInteraction.IO_REPARSE_TAG_MOUNT_POINT)
                    {
                        var targetDirectory = bufferConverted.PathBuffer.Substring(bufferConverted.SubstituteNameOffset / 2, bufferConverted.SubstituteNameLength / 2);
                        if (targetDirectory.StartsWith(@"\??\"))
                            symlinkTarget = targetDirectory.Substring(4);
                        else
                            symlinkTarget= targetDirectory;
                            return true;
                    }
                    return false;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
        }
    }
}
