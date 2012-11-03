//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

//Copyright (C) <2012> Glenn Mariën (http://project-vanilla.com)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using ServiceStack.Text;
using System.Collections;
using System.Runtime.Serialization;
using fCraft.MapConversion;

namespace fCraft.Portals
{
    public class PortalSerialization : IConverterExtension
    {
        private static readonly object SaveLoadLock = new object();
        private static List<string> _group = new List<string> { "portal" };
		public IEnumerable<string> AcceptedGroups { get { return _group; } }

        public int Serialize ( Map map, Stream stream, IMapConverterEx converter ) {
            BinaryWriter writer = new BinaryWriter( stream );
            int count = 0;
            if ( map.Portals != null ) {
                if ( map.Portals.Count >= 1 ) {
                    foreach ( Portal portal in map.Portals ) {
                        converter.WriteMetadataEntry( _group[0], portal.Name, portal.Serialize(), writer );
                        ++count;
                    }
                }
            }
            return count;
        }

        public void Deserialize(string group, string key, string value, Map map)
        {
            try
			{
				Portal portal = Portal.Deserialize(key, value, map);
                if ( map.Portals == null ) map.Portals = new ArrayList();
                if ( map.Portals.Count >= 1 ) {
                    if ( map.Portals.Contains( key ) ) {
                       Logger.Log( LogType.Error, "Map loading warning: duplicate portal name found: " + key + ", ignored" );
                       return;
                    }
                }
				map.Portals.Add(portal);
			}
			catch (Exception ex)
			{
			    Logger.Log(LogType.Error, "Portal.Deserialize: Error deserializing portal {0}: {1}", key, ex);
			}
        }
    }
}
