// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    public sealed class MapDat : IMapConverter {

        static readonly byte[] Mapping = new byte[256];

        static MapDat() {
            Mapping[50] = (byte)Block.Air;      // torch
            Mapping[51] = (byte)Block.Lava;     // fire
            Mapping[52] = (byte)Block.Glass;    // spawner
            Mapping[53] = (byte)Block.Stair;    // wood stairs
            Mapping[54] = (byte)Block.Wood;     // chest
            Mapping[55] = (byte)Block.Air;      // redstone wire
            Mapping[56] = (byte)Block.IronOre;  // diamond ore
            Mapping[57] = (byte)Block.Aqua;     // diamond block
            Mapping[58] = (byte)Block.Log;      // workbench
            Mapping[59] = (byte)Block.Leaves;   // crops
            Mapping[60] = (byte)Block.Dirt;     // soil
            Mapping[61] = (byte)Block.Stone;    // furnace
            Mapping[62] = (byte)Block.Stone;    // burning furnance
            Mapping[63] = (byte)Block.Air;      // sign post
            Mapping[64] = (byte)Block.Air;      // wooden door
            Mapping[65] = (byte)Block.Air;      // ladder
            Mapping[66] = (byte)Block.Air;      // rails
            Mapping[67] = (byte)Block.Stair;    // cobblestone stairs
            Mapping[68] = (byte)Block.Air;      // wall sign
            Mapping[69] = (byte)Block.Air;      // lever
            Mapping[70] = (byte)Block.Air;      // pressure plate
            Mapping[71] = (byte)Block.Air;      // iron door
            Mapping[72] = (byte)Block.Air;      // wooden pressure plate
            Mapping[73] = (byte)Block.IronOre;  // redstone ore
            Mapping[74] = (byte)Block.IronOre;  // glowing redstone ore
            Mapping[75] = (byte)Block.Air;      // redstone torch (off)
            Mapping[76] = (byte)Block.Air;      // redstone torch (on)
            Mapping[77] = (byte)Block.Air;      // stone button
            Mapping[78] = (byte)Block.Air;      // snow
            Mapping[79] = (byte)Block.Glass;    // ice
            Mapping[80] = (byte)Block.White;    // snow block
            Mapping[81] = (byte)Block.Leaves;   // cactus
            Mapping[82] = (byte)Block.Gray;     // clay
            Mapping[83] = (byte)Block.Leaves;   // reed
            Mapping[84] = (byte)Block.Log;      // jukebox
            Mapping[85] = (byte)Block.Wood;     // fence
            Mapping[86] = (byte)Block.Orange;   // pumpkin
            Mapping[87] = (byte)Block.Dirt;     // netherstone
            Mapping[88] = (byte)Block.Gravel;   // slow sand
            Mapping[89] = (byte)Block.Sand;     // lightstone
            Mapping[90] = (byte)Block.Violet;   // portal
            Mapping[91] = (byte)Block.Orange;   // jack-o-lantern
            // all others default to 0/air
        }


        public string ServerName {
            get { return "Creative/Vanilla"; }
        }


        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }


        public MapFormat Format {
            get { return MapFormat.Creative; }
        }


        public bool ClaimsName( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".dat", StringComparison.OrdinalIgnoreCase ) ||
                   fileName.EndsWith( ".mine", StringComparison.OrdinalIgnoreCase );
        }


        public bool Claims( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    byte[] temp = new byte[8];
                    mapStream.Seek( -4, SeekOrigin.End );
                    mapStream.Read( temp, 0, 4 );
                    mapStream.Seek( 0, SeekOrigin.Begin );
                    int length = BitConverter.ToInt32( temp, 0 );
                    byte[] data = new byte[length];
                    using( GZipStream reader = new GZipStream( mapStream, CompressionMode.Decompress, true ) ) {
                        reader.Read( data, 0, length );
                    }

                    for( int i = 0; i < length - 1; i++ ) {
                        if( data[i] == 0xAC && data[i + 1] == 0xED ) {
                            return true;
                        }
                    }
                    return false;
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            Map map = Load( fileName );
            map.Blocks = null;
            return map;
        }


        public static byte MapBlock( byte block ) {
            return Mapping[block];
        }

        public static Block MapBlock( Block block ) {
            return (Block)Mapping[(byte)block];
        }

        public Map Load( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                byte[] temp = new byte[8];
                Map map = null;

                mapStream.Seek( -4, SeekOrigin.End );
                mapStream.Read( temp, 0, 4 );
                mapStream.Seek( 0, SeekOrigin.Begin );
                int uncompressedLength = BitConverter.ToInt32( temp, 0 );
                byte[] data = new byte[uncompressedLength];
                using( GZipStream reader = new GZipStream( mapStream, CompressionMode.Decompress, true ) ) {
                    reader.Read( data, 0, uncompressedLength );
                }

                for( int i = 0; i < uncompressedLength - 1; i++ ) {
                    if( data[i] != 0xAC || data[i + 1] != 0xED ) continue;

                    // bypassing the header crap
                    int pointer = i + 6;
                    Array.Copy( data, pointer, temp, 0, 2 );
                    pointer += IPAddress.HostToNetworkOrder( BitConverter.ToInt16( temp, 0 ) );
                    pointer += 13;

                    int headerEnd;
                    // find the end of serialization listing
                    for( headerEnd = pointer; headerEnd < data.Length - 1; headerEnd++ ) {
                        if( data[headerEnd] == 0x78 && data[headerEnd + 1] == 0x70 ) {
                            headerEnd += 2;
                            break;
                        }
                    }

                    // start parsing serialization listing
                    int offset = 0;
                    int width = 0, length = 0, height = 0;
                    Position spawn = new Position();
                    while( pointer < headerEnd ) {
                        switch( (char)data[pointer] ) {
                            case 'Z':
                                offset++;
                                break;
                            case 'F':
                            case 'I':
                                offset += 4;
                                break;
                            case 'J':
                                offset += 8;
                                break;
                        }

                        pointer += 1;
                        Array.Copy( data, pointer, temp, 0, 2 );
                        short skip = IPAddress.HostToNetworkOrder( BitConverter.ToInt16( temp, 0 ) );
                        pointer += 2;

                        // look for relevant variables
                        Array.Copy( data, headerEnd + offset - 4, temp, 0, 4 );
                        if( MemCmp( data, pointer, "width" ) ) {
                            width = (ushort)IPAddress.HostToNetworkOrder( BitConverter.ToInt32( temp, 0 ) );
                        } else if( MemCmp( data, pointer, "depth" ) ) {
                            height = (ushort)IPAddress.HostToNetworkOrder( BitConverter.ToInt32( temp, 0 ) );
                        } else if( MemCmp( data, pointer, "height" ) ) {
                            length = (ushort)IPAddress.HostToNetworkOrder( BitConverter.ToInt32( temp, 0 ) );
                        } else if( MemCmp( data, pointer, "xSpawn" ) ) {
                            spawn.X = (short)( IPAddress.HostToNetworkOrder( BitConverter.ToInt32( temp, 0 ) ) * 32 + 16 );
                        } else if( MemCmp( data, pointer, "ySpawn" ) ) {
                            spawn.Z = (short)( IPAddress.HostToNetworkOrder( BitConverter.ToInt32( temp, 0 ) ) * 32 + 16 );
                        } else if( MemCmp( data, pointer, "zSpawn" ) ) {
                            spawn.Y = (short)( IPAddress.HostToNetworkOrder( BitConverter.ToInt32( temp, 0 ) ) * 32 + 16 );
                        }

                        pointer += skip;
                    }

                    map = new Map( null, width, length, height, false ) { Spawn = spawn };

                    if( !map.ValidateHeader() ) {
                        throw new MapFormatException( "One or more of the map dimensions are invalid." );
                    }

                    // find the start of the block array
                    bool foundBlockArray = false;
                    offset = Array.IndexOf<byte>( data, 0x00, headerEnd );
                    while( offset != -1 && offset < data.Length - 2 ) {
                        if( data[offset] == 0x00 && data[offset + 1] == 0x78 && data[offset + 2] == 0x70 ) {
                            foundBlockArray = true;
                            pointer = offset + 7;
                        }
                        offset = Array.IndexOf<byte>( data, 0x00, offset + 1 );
                    }

                    // copy the block array... or fail
                    if( foundBlockArray ) {
                        map.Blocks = new byte[map.Volume];
                        Array.Copy( data, pointer, map.Blocks, 0, map.Blocks.Length );
                        map.ConvertBlockTypes( Mapping );
                    } else {
                        throw new MapFormatException( "Could not locate block array." );
                    }
                    break;
                }
                return map;
            }
        }


        public bool Save( [NotNull] Map mapToSave, [NotNull] string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            throw new NotImplementedException();
        }


        static bool MemCmp( [NotNull] IList<byte> data, int offset, [NotNull] string value ) {
            if( data == null ) throw new ArgumentNullException( "data" );
            if( value == null ) throw new ArgumentNullException( "value" );
            // ReSharper disable LoopCanBeConvertedToQuery
            for( int i = 0; i < value.Length; i++ ) {
                if( offset + i >= data.Count || data[offset + i] != value[i] ) return false;
            }
            // ReSharper restore LoopCanBeConvertedToQuery
            return true;
        }
    }
}