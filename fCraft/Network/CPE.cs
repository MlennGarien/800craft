// Part of FemtoCraft | Copyright 2012-2013 Matvei Stefarov <me@matvei.org> | See LICENSE.txt
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using System;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace fCraft {
    public sealed partial class Player {
        const string CustomBlocksExtName = "CustomBlocks";
        const int CustomBlocksExtVersion = 1;
        const string BlockPermissionsExtName = "SetBlockPermissions";
        const int BlockPermissionsExtVersion = 1;
        const byte CustomBlocksLevel = 1;
        const string SelectionBoxExtName = "SelectionBoxExt";
        const int SelectionBoxExtVersion = 1;

        // Note: if more levels are added, change UsesCustomBlocks from bool to int
        bool UsesCustomBlocks { get; set; }
        public bool SupportsBlockPermissions { get; set; }
        public bool SelectionBoxExt { get; set; }
        string ClientName { get; set; }

        bool NegotiateProtocolExtension() {
            this.reader = new PacketReader( this.stream );
            // write our ExtInfo and ExtEntry packets
            writer.Write( Packet.MakeExtInfo( 2 ).Data );
            writer.Write( Packet.MakeExtEntry( CustomBlocksExtName, CustomBlocksExtVersion ).Data );
            writer.Write( Packet.MakeExtEntry( BlockPermissionsExtName, BlockPermissionsExtVersion ).Data );

            Logger.Log( LogType.SystemActivity, "Sent ExtInfo and entry packets" );

            // Expect ExtInfo reply from the client
            OpCode extInfoReply = ( OpCode )reader.ReadByte();
            Logger.Log( LogType.SystemActivity, "Expected: {0} / Received: {1}", OpCode.ExtInfo, extInfoReply );
            if ( extInfoReply != OpCode.ExtInfo ) {
                Logger.Log( LogType.Warning, "Player {0} from {1}: Unexpected ExtInfo reply ({2})", Name, IP, extInfoReply );
                return false;
            }
            //read EXT_INFO
            ClientName = reader.ReadString();
            int expectedEntries = reader.ReadInt16();

            // wait for client to send its ExtEntries
            bool sendCustomBlockPacket = false;
            List<string> clientExts = new List<string>();
            for ( int i = 0; i < expectedEntries; i++ ) {
                // Expect ExtEntry replies (0 or more)
                OpCode extEntryReply = ( OpCode )reader.ReadByte();
                Logger.Log( LogType.SystemActivity, "Expected: {0} / Received: {1}", OpCode.ExtEntry, extEntryReply );
                if ( extEntryReply != OpCode.ExtEntry ) {
                    Logger.Log( LogType.Warning, "Player {0} from {1}: Unexpected ExtEntry reply ({2})", Name, IP, extEntryReply );
                    return false;
                }
                string extName = reader.ReadString();
                int extVersion = reader.ReadInt32();
                if ( extName == CustomBlocksExtName && extVersion == CustomBlocksExtVersion ) {
                    // Hooray, client supports custom blocks! We still need to check support level.
                    sendCustomBlockPacket = true;
                    clientExts.Add( extName + " " + extVersion );
                } else if ( extName == BlockPermissionsExtName && extVersion == BlockPermissionsExtVersion ) {
                    SupportsBlockPermissions = true;
                    clientExts.Add( extName + " " + extVersion );
                } else if ( extName == SelectionBoxExtName && extVersion == SelectionBoxExtVersion ) {
                    SelectionBoxExt = true;
                }
            }

            // log client's capabilities
            if ( clientExts.Count > 0 ) {
                Logger.Log( LogType.SystemActivity, "Player {0} is using \"{1}\", supporting: {2}",
                            Name,
                            ClientName,
                            clientExts.JoinToString( ", " ) );
            }

            if ( sendCustomBlockPacket ) {
                // if client also supports CustomBlockSupportLevel, figure out what level to use

                // Send CustomBlockSupportLevel
                writer.Write( Packet.MakeCustomBlockSupportLevel( CustomBlocksLevel ).Data );

                // Expect CustomBlockSupportLevel reply
                OpCode customBlockSupportLevelReply = ( OpCode )reader.ReadByte();
                Logger.Log( LogType.SystemActivity, "Expected: {0} / Received: {1}", OpCode.CustomBlocks, customBlockSupportLevelReply );
                if ( customBlockSupportLevelReply != OpCode.CustomBlocks ) {
                    Logger.Log( LogType.Warning, "Player {0} from {1}: Unexpected CustomBlockSupportLevel reply ({2})",
                                       Name,
                                       IP,
                                       customBlockSupportLevelReply );
                    return false;
                }
                byte clientLevel = reader.ReadByte();
                UsesCustomBlocks = ( clientLevel >= CustomBlocksLevel );
            }
            this.reader = new BinaryReader( this.stream );
            return true;
        }

        // For non-extended players, use appropriate substitution
        void ProcessOutgoingSetBlock( ref Packet packet ) {
            if ( packet.Data[7] > ( byte )Map.MaxLegalBlockType && !UsesCustomBlocks ) {
                packet.Data[7] = ( byte )Map.GetFallbackBlock( ( Block )packet.Data[7] );
            }
        }


        public void SendBlockPermissions() {
            Send( Packet.MakeSetBlockPermission( Block.Water, Can( Permission.PlaceWater ), true ) );
            Send( Packet.MakeSetBlockPermission( Block.StillWater, Can( Permission.PlaceWater ), true ) );
            Send( Packet.MakeSetBlockPermission( Block.Lava, Can( Permission.PlaceLava ), true ) );
            Send( Packet.MakeSetBlockPermission( Block.StillLava, Can( Permission.PlaceLava ), true ) );
            Send( Packet.MakeSetBlockPermission( Block.Admincrete, Can( Permission.PlaceAdmincrete ), Can( Permission.DeleteAdmincrete ) ) );
            Send( Packet.MakeSetBlockPermission( Block.Grass, Can( Permission.PlaceGrass ), true ) );
        }
    }


    partial struct Packet {
        [Pure]
        public static Packet MakeExtInfo( short extCount ) {
            String VersionString = "800Craft " + Updater.CurrentRelease.VersionString;
            Logger.Log( LogType.SystemActivity, "Send: ExtInfo({0},{1})", VersionString, extCount );

            Packet packet = new Packet( OpCode.ExtInfo );
            Encoding.ASCII.GetBytes( VersionString.PadRight( 64 ), 0, 64, packet.Data, 1 );
            ToNetOrder( extCount, packet.Data, 65 );
            return packet;
        }

        [Pure]
        public static Packet MakeExtEntry( string name, int version ) {
            Logger.Log( LogType.SystemActivity, "Send: ExtEntry({0},{1})", name, version );
            Packet packet = new Packet( OpCode.ExtEntry );
            Encoding.ASCII.GetBytes( name.PadRight( 64 ), 0, 64, packet.Data, 1 );
            ToNetOrder( version, packet.Data, 65 );
            return packet;
        }

        public static Packet MakeAddSelectionBox( byte ID, string Label, short StartX, short StartY, short StartZ, short EndX, short EndY, short EndZ, short R, short G, short B, short A ) {
            Logger.Log( LogType.SystemActivity, "Send: MakeAddSelectionBox({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11})",
                ID, Label, StartX, StartY, StartZ, EndX, EndY, EndZ, R, G, B, A );
            Packet packet = new Packet( OpCode.SelectionCuboid );
            packet.Data[1] = ID;
            Encoding.ASCII.GetBytes( Label.PadRight( 64 ), 0, 64, packet.Data, 2 );
            ToNetOrder( StartX, packet.Data, 66 );
            ToNetOrder( StartY, packet.Data, 68 );
            ToNetOrder( StartZ, packet.Data, 70 );
            ToNetOrder( EndX, packet.Data, 72 );
            ToNetOrder( EndY, packet.Data, 74 );
            ToNetOrder( EndZ, packet.Data, 76 );
            ToNetOrder( R, packet.Data, 78 );
            ToNetOrder( G, packet.Data, 80 );
            ToNetOrder( B, packet.Data, 82 );
            ToNetOrder( A, packet.Data, 84 );
            return packet;
        }

        [Pure]
        public static Packet MakeCustomBlockSupportLevel( byte level ) {
            Logger.Log( LogType.SystemActivity, "Send: CustomBlockSupportLevel({0})", level );
            Packet packet = new Packet( OpCode.CustomBlocks );
            packet.Data[1] = level;
            return packet;
        }

        [Pure]
        public static Packet MakeSetBlockPermission( Block block, bool canPlace, bool canDelete ) {
            Packet packet = new Packet( OpCode.SetBlockPermissions );
            packet.Data[1] = ( byte )block;
            packet.Data[2] = ( byte )( canPlace ? 1 : 0 );
            packet.Data[3] = ( byte )( canDelete ? 1 : 0 );
            return packet;
        }
        static void ToNetOrder( short number, [NotNull] byte[] arr, int offset ) {
            if ( arr == null )
                throw new Exception( "arr" );
            arr[offset] = ( byte )( ( number & 0xff00 ) >> 8 );
            arr[offset + 1] = ( byte )( number & 0x00ff );
        }


        static void ToNetOrder( int number, [NotNull] byte[] arr, int offset ) {
            if ( arr == null )
                throw new ArgumentNullException( "arr" );
            arr[offset] = ( byte )( ( number & 0xff000000 ) >> 24 );
            arr[offset + 1] = ( byte )( ( number & 0x00ff0000 ) >> 16 );
            arr[offset + 2] = ( byte )( ( number & 0x0000ff00 ) >> 8 );
            arr[offset + 3] = ( byte )( number & 0x000000ff );
        }
    }


    public sealed partial class Map {
        public const Block MaxCustomBlockType = Block.StoneBrick;
        readonly static Block[] FallbackBlocks = new Block[256];


        static void DefineFallbackBlocks() {
            for ( int i = 0; i <= ( int )Block.Obsidian; i++ ) {
                FallbackBlocks[i] = ( Block )i;
            }
            FallbackBlocks[( int )Block.CobbleSlab] = Block.Stair;
            FallbackBlocks[( int )Block.Rope] = Block.BrownMushroom;
            FallbackBlocks[( int )Block.Sandstone] = Block.Sand;
            FallbackBlocks[( int )Block.Snow] = Block.Air;
            FallbackBlocks[( int )Block.Fire] = Block.StillLava;
            FallbackBlocks[( int )Block.LightPink] = Block.Pink;
            FallbackBlocks[( int )Block.DarkGreen] = Block.Green;
            FallbackBlocks[( int )Block.Brown] = Block.Dirt;
            FallbackBlocks[( int )Block.DarkBlue] = Block.Blue;
            FallbackBlocks[( int )Block.Turquoise] = Block.Cyan;
            FallbackBlocks[( int )Block.Ice] = Block.Glass;
            FallbackBlocks[( int )Block.Tile] = Block.Iron;
            FallbackBlocks[( int )Block.Magma] = Block.Obsidian;
            FallbackBlocks[( int )Block.Pillar] = Block.White;
            FallbackBlocks[( int )Block.Crate] = Block.Wood;
            FallbackBlocks[( int )Block.StoneBrick] = Block.Stone;
        }


        public static Block GetFallbackBlock( Block block ) {
            return FallbackBlocks[( int )block];
        }

        public const Block MaxLegalBlockType = Block.Obsidian;
        public unsafe byte[] GetFallbackMap() {
            byte[] translatedBlocks = ( byte[] )Blocks.Clone();
            int volume = translatedBlocks.Length;
            fixed ( byte* ptr = translatedBlocks ) {
                for ( int i = 0; i < volume; i++ ) {
                    byte block = ptr[i];
                    if ( block > ( byte )MaxLegalBlockType ) {
                        ptr[i] = ( byte )FallbackBlocks[block];
                    }
                }
            }
            return translatedBlocks;
        }
    }
}