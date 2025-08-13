/*
Copyright © 2025 dontstopmeow <public-static@hotmail.com>

This file is part of Linkerator.

Linkerator is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

Linkerator is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with Linkerator. If not, see <https://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Linkerator.Models
{
    [XmlRoot("SetupConfig")]
    public class SetupConfig
    {
        [XmlElement("OriginFolderPath")]
        public string SourcePath { get; set; } = String.Empty;

        [XmlArray("Platforms")]
        [XmlArrayItem("Platform")]
        public List<SetupPlatformEntry> PlatformEntries { get; set; } = [];
    }

    public class SetupPlatformEntry
    {
        [XmlElement("Name")]
        public string Name { get; set; } = string.Empty;

        [XmlArray("MirrorRules")]
        [XmlArrayItem("MirrorRule")]
        public List<MirrorRule> MirrorRules { get; set; } = [];
    }

    public class MirrorRule
    {
        [XmlElement("Origin")]
        public string Origin { get; set; } = string.Empty;

        [XmlElement("Destination")]
        public string Destination {  get; set; } = string.Empty;
    }
}
