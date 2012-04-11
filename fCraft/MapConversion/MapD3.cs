// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    public sealed class MapD3 : IMapConverter {

        static readonly byte[] Mapping = new byte[256];

        static MapD3() {
            // 0-49 default
            Mapping[50] = (byte)Block.TNT;          // Torch
            Mapping[51] = (byte)Block.StillLava;    // Fire
            Mapping[52] = (byte)Block.Blue;         // Water Source
            Mapping[53] = (byte)Block.Red;          // Lava Source
            Mapping[54] = (byte)Block.TNT;          // Chest
            Mapping[55] = (byte)Block.TNT;          // Gear
            Mapping[56] = (byte)Block.Glass;        // Diamond Ore
            Mapping[57] = (byte)Block.Glass;        // Diamond
            Mapping[58] = (byte)Block.TNT;          // Workbench
            Mapping[59] = (byte)Block.Leaves;       // Crops
            Mapping[60] = (byte)Block.Obsidian;     // Soil
            Mapping[61] = (byte)Block.Cobblestone;        // Furnace
            Mapping[62] = (byte)Block.StillLava;    // Burning Furnace
            // 63-199 unused
            Mapping[200] = (byte)Block.Lava;        // Kill Lava
            Mapping[201] = (byte)Block.Stone;       // Kill Lava
            // 202 unused
            Mapping[203] = (byte)Block.Stair;       // Still Stair
            // 204-205 unused
            Mapping[206] = (byte)Block.Water;       // Original Water
            Mapping[207] = (byte)Block.Lava;        // Original Lava
            // 208 Invisible
            Mapping[209] = (byte)Block.Water;       // Acid
            Mapping[210] = (byte)Block.Sand;        // Still Sand
            Mapping[211] = (byte)Block.Water;       // Still Acid
            Mapping[212] = (byte)Block.RedFlower;   // Kill Rose
            Mapping[213] = (byte)Block.Gravel;      // Still Gravel
            // 214 No Entry
            Mapping[215] = (byte)Block.White;       // Snow
            Mapping[216] = (byte)Block.Lava;        // Fast Lava
            Mapping[217] = (byte)Block.White;       // Kill Glass
            // 218 Invisible Sponge
            Mapping[219] = (byte)Block.Sponge;      // Drain Sponge
            Mapping[220] = (byte)Block.Sponge;      // Super Drain Sponge
            Mapping[221] = (byte)Block.Gold;        // Spark
            Mapping[222] = (byte)Block.TNT;         // Rocket
            Mapping[223] = (byte)Block.Gold;        // Short Spark
            Mapping[224] = (byte)Block.TNT;         // Mega Rocket
            Mapping[225] = (byte)Block.Lava;        // Red Spark
            Mapping[226] = (byte)Block.TNT;         // Fire Fountain
            Mapping[227] = (byte)Block.TNT;         // Admin TNT
            Mapping[228] = (byte)Block.Iron;       // Fan
            Mapping[229] = (byte)Block.Iron;       // Door
            Mapping[230] = (byte)Block.Lava;        // Campfire
            Mapping[231] = (byte)Block.Red;         // Laser
            Mapping[232] = (byte)Block.Black;       // Ash
            // 233-234 unused
            Mapping[235] = (byte)Block.Water;       // Sea
            Mapping[236] = (byte)Block.White;       // Flasher
            // 237-243 unused
            Mapping[244] = (byte)Block.Leaves;      // Vines
            Mapping[245] = (byte)Block.Lava;        // Flamethrower
            // 246 unused
            Mapping[247] = (byte)Block.Iron;       // Cannon
            Mapping[248] = (byte)Block.Obsidian;    // Blob
            // all others default to 0/air
        }


        public string ServerName {
            get { return "D3"; }
        }


        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.D3; }
        }


        public bool ClaimsName( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".map", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                        BinaryReader bs = new BinaryReader( gs );
                        int formatVersion = IPAddress.NetworkToHostOrder( bs.ReadInt32() );
                        return (formatVersion == 1000 || formatVersion == 1010 || formatVersion == 1020 ||
                                formatVersion == 1030 || formatVersion == 1040 || formatVersion == 1050);
                    }
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                return LoadHeaderInternal( mapStream );
            }
        }


        static Map LoadHeaderInternal( Stream stream ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );
            // Setup a GZipStream to decompress and read the map file
            using( GZipStream gs = new GZipStream( stream, CompressionMode.Decompress, true ) ) {
                BinaryReader bs = new BinaryReader( gs );

                int formatVersion = IPAddress.NetworkToHostOrder( bs.ReadInt32() );

                // Read in the map dimesions
                int width = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                int length = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                int height = IPAddress.NetworkToHostOrder( bs.ReadInt16() );

                Map map = new Map( null, width, length, height, false );

                Position spawn = new Position();

                switch( formatVersion ) {
                    case 1000:
                    case 1010:
                        break;
                    case 1020:
                        spawn.X = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        spawn.Y = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        spawn.Z = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        map.Spawn = spawn;
                        break;
                    //case 1030:
                    //case 1040:
                    //case 1050:
                    default:
                        spawn.X = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        spawn.Y = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        spawn.Z = IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        spawn.R = (byte)IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        spawn.L = (byte)IPAddress.NetworkToHostOrder( bs.ReadInt16() );
                        map.Spawn = spawn;
                        break;
                }

                return map;
            }
        }


        public Map Load( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {

                Map map = LoadHeaderInternal( mapStream );

                if( !map.ValidateHeader() ) {
                    throw new MapFormatException( "One or more of the map dimensions are invalid." );
                }

                // Read in the map data
                map.Blocks = new byte[map.Volume];
                mapStream.Read( map.Blocks, 0, map.Blocks.Length );
                map.ConvertBlockTypes( Mapping );

                return map;
            }
        }


        public bool Save( [NotNull] Map mapToSave, [NotNull] string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.Create( fileName ) ) {
                using( GZipStream gs = new GZipStream( mapStream, CompressionMode.Compress ) ) {
                    BinaryWriter bs = new BinaryWriter( gs );

                    // Write the magic number
                    bs.Write( IPAddress.HostToNetworkOrder( 1050 ) );
                    bs.Write( (byte)0 );
                    bs.Write( (byte)0 );

                    // Write the map dimensions
                    bs.Write( IPAddress.NetworkToHostOrder( mapToSave.Width ) );
                    bs.Write( IPAddress.NetworkToHostOrder( mapToSave.Length ) );
                    bs.Write( IPAddress.NetworkToHostOrder( mapToSave.Height ) );

                    // Write the map data
                    bs.Write( mapToSave.Blocks, 0, mapToSave.Blocks.Length );

                    bs.Close();
                    return true;
                }
            }
        }
    }
}