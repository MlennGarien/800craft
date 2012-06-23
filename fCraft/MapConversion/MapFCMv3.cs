// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> fCraft map format converter, for format version #3 (2011).
    /// Soon to be obsoleted by FCMv4. </summary>
    public sealed class MapFCMv3 : IMapConverterEx {
        public const int Identifier = 0x0FC2AF40;
        public const byte Revision = 13;

    	private Dictionary<string, IConverterExtension> _extensions=new Dictionary<string, IConverterExtension>();

        public string ServerName {
            get { return "fCraft"; }
        }


        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.FCMv3; }
        }


        public bool ClaimsName( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".fcm", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                try {
                    BinaryReader reader = new BinaryReader( mapStream );
                    int id = reader.ReadInt32();
                    int rev = reader.ReadByte();
                    return (id == Identifier && rev == Revision);
                } catch( Exception ) {
                    return false;
                }
            }
        }


        public Map LoadHeader( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                BinaryReader reader = new BinaryReader( mapStream );

                Map map = LoadHeaderInternal( reader );

                // skip the index
                int layerCount = reader.ReadByte();
                mapStream.Seek( 25 * layerCount, SeekOrigin.Current );

                // read metadata
                int metaCount = reader.ReadInt32();

                using( DeflateStream ds = new DeflateStream( mapStream, CompressionMode.Decompress ) ) {
                    BinaryReader br = new BinaryReader( ds );
                    for( int i = 0; i < metaCount; i++ ) {
                        string group = ReadLengthPrefixedString( br ).ToLowerInvariant();
                        string key = ReadLengthPrefixedString( br ).ToLowerInvariant();
                        string newValue = ReadLengthPrefixedString( br );

                        string oldValue;
						
						IConverterExtension ex;
						if (_extensions.TryGetValue(group, out ex))
						{
							ex.Deserialize(group, key, newValue, map);
						}
						else
						{
							if (map.Metadata.TryGetValue(key, group, out oldValue) && oldValue != newValue)
							{
								Logger.Log(LogType.Warning,
										"MapFCMv3.LoadHeader: Duplicate metadata entry found for [{0}].[{1}]. " +
										"Old value (overwritten): \"{2}\". New value: \"{3}\"",
										group, key, oldValue, newValue);
							}
							map.Metadata[group, key] = newValue;
						}
                    }
                }

                return map;
            }
        }


        public Map Load( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                BinaryReader reader = new BinaryReader( mapStream );

                Map map = LoadHeaderInternal( reader );

                // read the layer index
                int layerCount = reader.ReadByte();
                if( layerCount < 1 ) {
                    throw new MapFormatException( "No data layers found." );
                }
                mapStream.Seek( 25 * layerCount, SeekOrigin.Current );

                // read metadata
                int metaSize = reader.ReadInt32();

                using( DeflateStream ds = new DeflateStream( mapStream, CompressionMode.Decompress ) ) {
                    BinaryReader br = new BinaryReader( ds );
                    for( int i = 0; i < metaSize; i++ ) {
                        string group = ReadLengthPrefixedString( br ).ToLowerInvariant();
                        string key = ReadLengthPrefixedString( br ).ToLowerInvariant();
                        string newValue = ReadLengthPrefixedString( br );

                        string oldValue;

                    	IConverterExtension ex;
						if (_extensions.TryGetValue(group, out ex))
						{
							ex.Deserialize(group, key, newValue, map);
						}
						else 
						{
							if( map.Metadata.TryGetValue( key, group, out oldValue ) && oldValue != newValue ) 
							{
								Logger.Log( LogType.Warning,
                                        "MapFCMv3.LoadHeader: Duplicate metadata entry found for [{0}].[{1}]. " +
                                        "Old value (overwritten): \"{2}\". New value: \"{3}\"",
                                        group, key, oldValue, newValue );
							}
							map.Metadata[group, key] = newValue;
						}
                        
                    }
                    map.Blocks = new byte[map.Volume];
                    ds.Read( map.Blocks, 0, map.Blocks.Length );
                    map.RemoveUnknownBlocktypes();
                }
                return map;
            }
        }


        static Map LoadHeaderInternal( [NotNull] BinaryReader reader ) {
            if( reader == null ) throw new ArgumentNullException( "reader" );
            if( reader.ReadInt32() != Identifier || reader.ReadByte() != Revision ) {
                throw new MapFormatException();
            }

            // read dimensions
            int width = reader.ReadInt16();
            int height = reader.ReadInt16();
            int length = reader.ReadInt16();

            // ReSharper disable UseObjectOrCollectionInitializer
            Map map = new Map( null, width, length, height, false );
            // ReSharper restore UseObjectOrCollectionInitializer

            // read spawn
            map.Spawn = new Position {
                X = (short)reader.ReadInt32(),
                Z = (short)reader.ReadInt32(),
                Y = (short)reader.ReadInt32(),
                R = reader.ReadByte(),
                L = reader.ReadByte()
            };


            // read modification/creation times
            map.DateModified = DateTimeUtil.ToDateTimeLegacy( reader.ReadUInt32() );
            map.DateCreated = DateTimeUtil.ToDateTimeLegacy( reader.ReadUInt32() );

            // read UUID
            map.Guid = new Guid( reader.ReadBytes( 16 ) );
            return map;
        }


        public bool Save( [NotNull] Map mapToSave, [NotNull] string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.Create( fileName ) ) {
                BinaryWriter writer = new BinaryWriter( mapStream );

                writer.Write( Identifier );
                writer.Write( Revision );

                writer.Write( (short)mapToSave.Width );
                writer.Write( (short)mapToSave.Height );
                writer.Write( (short)mapToSave.Length );

                writer.Write( (int)mapToSave.Spawn.X );
                writer.Write( (int)mapToSave.Spawn.Z );
                writer.Write( (int)mapToSave.Spawn.Y );

                writer.Write( mapToSave.Spawn.R );
                writer.Write( mapToSave.Spawn.L );

                mapToSave.DateModified = DateTime.UtcNow;
                writer.Write( (uint)mapToSave.DateModified.ToUnixTimeLegacy() );
                writer.Write( (uint)mapToSave.DateCreated.ToUnixTimeLegacy() );

                writer.Write( mapToSave.Guid.ToByteArray() );

                writer.Write( (byte)1 ); // layer count

                // skip over index and metacount
                long indexOffset = mapStream.Position;
                writer.Seek( 29, SeekOrigin.Current );

                byte[] blocksCache = mapToSave.Blocks;
                int metaCount, compressedLength;
                long offset;
                using( DeflateStream ds = new DeflateStream( mapStream, CompressionMode.Compress, true ) ) {
                    using( BufferedStream bs = new BufferedStream( ds ) ) {
                        // write metadata
                        metaCount = WriteMetadata( bs, mapToSave );
                        offset = mapStream.Position; // inaccurate, but who cares
                        bs.Write( blocksCache, 0, blocksCache.Length );
                        compressedLength = (int)(mapStream.Position - offset);
                    }
                }

                // come back to write the index
                writer.BaseStream.Seek( indexOffset, SeekOrigin.Begin );

                writer.Write( (byte)0 );            // data layer type (Blocks)
                writer.Write( offset );             // offset, in bytes, from start of stream
                writer.Write( compressedLength );   // compressed length, in bytes
                writer.Write( 0 );                  // general purpose field
                writer.Write( 1 );                  // element size
                writer.Write( blocksCache.Length ); // element count

                writer.Write( metaCount );
                return true;
            }
        }


        static string ReadLengthPrefixedString( [NotNull] BinaryReader reader ) {
            if( reader == null ) throw new ArgumentNullException( "reader" );
            int length = reader.ReadUInt16();
            byte[] stringData = reader.ReadBytes( length );
            return Encoding.ASCII.GetString( stringData );
        }

		private static void WriteLengthPrefixedString( [NotNull] BinaryWriter writer, [NotNull] string str ) {
            if( writer == null ) throw new ArgumentNullException( "writer" );
            if( str == null ) throw new ArgumentNullException( "str" );
            if( str.Length > ushort.MaxValue ) throw new ArgumentException( "String is too long.", "str" );
            byte[] stringData = Encoding.ASCII.GetBytes( str );
            writer.Write( (ushort)stringData.Length );
            writer.Write( stringData );
        }

        private int WriteMetadata( [NotNull] Stream stream, [NotNull] Map map )
        {
        	if (stream == null) throw new ArgumentNullException("stream");
        	if (map == null) throw new ArgumentNullException("map");
        	BinaryWriter writer = new BinaryWriter(stream);
        	int metaCount = 0;
        	lock (map.Metadata.SyncRoot)
        	{
        		foreach (var entry in map.Metadata)
        		{
        			WriteMetadataEntry(entry.Group, entry.Key, entry.Value, writer);
        			metaCount++;
        		}
        	}

        	//extensions
			if (_extensions.Count > 0)
			{
				metaCount += _extensions.Values.Sum(ex => ex.Serialize(map, stream, this));
			}
        	return metaCount;
        }


		public void WriteMetadataEntry(string group, string key, string value, BinaryWriter writer)
		{
			WriteLengthPrefixedString(writer, group);
			WriteLengthPrefixedString(writer, key);
			WriteLengthPrefixedString(writer, value);
		}

		public IMapConverterEx AddExtension(IConverterExtension ex)
		{
			//NullPtEx is not checked since this extensions are added once on runup and any exceptions here are programming errors
			foreach (string s in ex.AcceptedGroups)
				_extensions.Add(s, ex);
			return this; //to be able to add multiple extensions in one line: converter.AddExtension(e1).AddExtension(e2);
		}
    }
}