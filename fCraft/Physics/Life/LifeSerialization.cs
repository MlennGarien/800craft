using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using fCraft.MapConversion;

namespace fCraft
{
	public class LifeSerialization : IConverterExtension
	{
		private static List<string> _group = new List<string> { "Life" };
		public IEnumerable<string> AcceptedGroups { get { return _group; } }
		
		public int Serialize(Map map, Stream stream, IMapConverterEx converter)
		{
			//BinaryWriter writer = new BinaryWriter(stream);
			//int count = 0;
			//World w = map.World;
			//Object lockObj = null == w ? new object() : w.SyncRoot;

			//IEnumerable<Life2DZone> lifes;
			//lock (lockObj)
			//{
			//    lifes = map.LifeZones.Values.ToList(); //copies the current life list under a lock
			//}
			//foreach (Life2DZone life in map.LifeZones.Values)
			//{
			//    converter.WriteMetadataEntry(_group[0], life.Name, life.Serialize(), writer);
			//    ++count;	
			//}
			//return count;
			return 0;
		}

		public void Deserialize(string group, string key, string value, Map map)
		{
			//try
			//{
			//    map.World.TryAddLife(new Life2DZone())
			//}
			//catch (Exception ex)
			//{
			//    Logger.Log(LogType.Error, "LifeSerialization.Deserialize: Error deserializing life: {0}", ex);
			//}
		}
	}
}
