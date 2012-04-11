// Contributed by Jared Klopper. Opticraft is copyright (c) 2011-2012, Jared Klopper
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    [DataContract]
    public sealed class OpticraftMetaData {
        [DataMember]
        public int X { get; set; }
        [DataMember]
        public int Y { get; set; }
        [DataMember]
        public int Z { get; set; }
        [DataMember]
        public int SpawnX { get; set; }
        [DataMember]
        public int SpawnY { get; set; }
        [DataMember]
        public int SpawnZ { get; set; }
        [DataMember]
        public byte SpawnOrientation { get; set; }
        [DataMember]
        public byte SpawnPitch { get; set; }
        [DataMember]
        public string MinimumBuildRank { get; set; }
        [DataMember]
        public string MinimumJoinRank { get; set; }
        [DataMember]
        public bool Hidden { get; set; }
        [DataMember]
        public int CreationDate { get; set; }
    }


    public sealed class OpticraftDataStore {
        [DataMember]
        public OpticraftZone[] Zones;

    }


    public sealed class OpticraftZone {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public int X1 { get; set; }
        [DataMember]
        public int X2 { get; set; }
        [DataMember]
        public int Y1 { get; set; }
        [DataMember]
        public int Y2 { get; set; }
        [DataMember]
        public int Z1 { get; set; }
        [DataMember]
        public int Z2 { get; set; }
        [DataMember]
        public string MinimumRank { get; set; }
        [DataMember]
        public string Owner { get; set; }
        [DataMember]
        public string[] Builders;
        [DataMember]
        public string[] Excluded;
    }


    public sealed class MapOpticraft : IMapConverter {
        public string ServerName {
            get { return "Opticraft"; }
        }

        const short MapVersion = 2;

        public MapFormat Format {
            get { return MapFormat.Opticraft; }
        }

        public MapStorageType StorageType {
            get { return MapStorageType.SingleFile; }
        }


        public bool ClaimsName( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            return fileName.EndsWith( ".save", StringComparison.Ordinal );
        }


        public bool Claims( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            try {
                using( FileStream mapStream = File.OpenRead( fileName ) ) {
                    BinaryReader reader = new BinaryReader( mapStream );
                    return reader.ReadInt16() == MapVersion;
                }
            } catch( Exception ) {
                return false;
            }
        }


        public Map LoadHeader( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                return LoadMapMetadata( mapStream );
            }
        }


        static Map LoadMapMetadata( [NotNull] Stream mapStream ) {
            if( mapStream == null ) throw new ArgumentNullException( "mapStream" );
            BinaryReader reader = new BinaryReader( mapStream );
            reader.ReadInt16();
            int metaDataSize = reader.ReadInt32();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer( typeof( OpticraftMetaData ) );

            byte[] rawMetaData = new byte[metaDataSize];
            reader.Read( rawMetaData, 0, metaDataSize );
            MemoryStream memStream = new MemoryStream( rawMetaData );

            OpticraftMetaData metaData = (OpticraftMetaData)serializer.ReadObject( memStream );
            // ReSharper disable UseObjectOrCollectionInitializer
            Map mapFile = new Map( null, metaData.X, metaData.Y, metaData.Z, false );
            // ReSharper restore UseObjectOrCollectionInitializer
            mapFile.Spawn = new Position {
                X = (short)(metaData.SpawnX),
                Y = (short)(metaData.SpawnY),
                Z = (short)(metaData.SpawnZ),
                R = metaData.SpawnOrientation,
                L = metaData.SpawnPitch
            };
            return mapFile;
        }


        public Map Load( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenRead( fileName ) ) {
                BinaryReader reader = new BinaryReader( mapStream );
                // Load MetaData
                Map mapFile = LoadMapMetadata( mapStream );

                // Load the data store
                int dataBlockSize = reader.ReadInt32();
                byte[] jsonDataBlock = new byte[dataBlockSize];
                reader.Read( jsonDataBlock, 0, dataBlockSize );
                MemoryStream memStream = new MemoryStream( jsonDataBlock );
                DataContractJsonSerializer serializer = new DataContractJsonSerializer( typeof( OpticraftDataStore ) );
                OpticraftDataStore dataStore = (OpticraftDataStore)serializer.ReadObject( memStream );
                reader.ReadInt32();
                // Load Zones
                LoadZones( mapFile, dataStore );

                // Load the block store
                mapFile.Blocks = new Byte[mapFile.Volume];
                using( GZipStream decompressor = new GZipStream( mapStream, CompressionMode.Decompress ) ) {
                    decompressor.Read( mapFile.Blocks, 0, mapFile.Blocks.Length );
                }

                return mapFile;
            }
        }


        static void LoadZones( [NotNull] Map mapFile, [NotNull] OpticraftDataStore dataStore ) {
            if( mapFile == null ) throw new ArgumentNullException( "mapFile" );
            if( dataStore == null ) throw new ArgumentNullException( "dataStore" );
            if( dataStore.Zones.Length == 0 ) {
                return;
            }

            // TODO: investigate side effects
            PlayerInfo conversionPlayer = new PlayerInfo( "OpticraftConversion", RankManager.HighestRank, true, RankChangeType.AutoPromoted );
            foreach( OpticraftZone optiZone in dataStore.Zones ) {
                // Make zone
                Zone fZone = new Zone {
                    Name = optiZone.Name,
                };
                BoundingBox bBox = new BoundingBox( optiZone.X1, optiZone.Y1, optiZone.Z1, optiZone.X2, optiZone.X2, optiZone.Z2 );
                fZone.Create( bBox, conversionPlayer );

                // Min rank
                Rank minRank = Rank.Parse( optiZone.MinimumRank );
                if( minRank != null ) {
                    fZone.Controller.MinRank = minRank;
                }

                foreach( string playerName in optiZone.Builders ) {
                    // These are all lower case names
                    if( !Player.IsValidName( playerName ) ) {
                        continue;
                    }
                    PlayerInfo pInfo = PlayerDB.FindPlayerInfoExact( playerName );
                    if( pInfo != null ) {
                        fZone.Controller.Include( pInfo );
                    }
                }
                // Excluded names are not as of yet implemented in opticraft, but will be soon
                // So add compatibility for them when they arrive.
                if( optiZone.Excluded != null ) {
                    foreach( string playerName in optiZone.Excluded ) {
                        // These are all lower case names
                        if( !Player.IsValidName( playerName ) ) {
                            continue;
                        }
                        PlayerInfo pInfo = PlayerDB.FindPlayerInfoExact( playerName );
                        if( pInfo != null ) {
                            fZone.Controller.Exclude( pInfo );
                        }
                    }
                }
                mapFile.Zones.Add( fZone );
            }
        }


        public bool Save( [NotNull] Map mapToSave, [NotNull] string fileName ) {
            if( mapToSave == null ) throw new ArgumentNullException( "mapToSave" );
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            using( FileStream mapStream = File.OpenWrite( fileName ) ) {
                BinaryWriter writer = new BinaryWriter( mapStream );
                // Version
                writer.Write( MapVersion );

                MemoryStream serializationStream = new MemoryStream();
                DataContractJsonSerializer serializer = new DataContractJsonSerializer( typeof( OpticraftMetaData ) );
                // Create and serialize core meta data
                OpticraftMetaData oMetadate = new OpticraftMetaData {
                    X = mapToSave.Width,
                    Y = mapToSave.Length,
                    Z = mapToSave.Height,
                    // Spawn
                    SpawnX = mapToSave.Spawn.X,
                    SpawnY = mapToSave.Spawn.Y,
                    SpawnZ = mapToSave.Spawn.Z,
                    SpawnOrientation = mapToSave.Spawn.R,
                    SpawnPitch = mapToSave.Spawn.L
                };
                // World related values.
                if( mapToSave.World != null ) {
                    oMetadate.Hidden = mapToSave.World.IsHidden;
                    oMetadate.MinimumJoinRank = mapToSave.World.AccessSecurity.MinRank.Name;
                    oMetadate.MinimumBuildRank = mapToSave.World.BuildSecurity.MinRank.Name;
                } else {
                    oMetadate.Hidden = false;
                    oMetadate.MinimumJoinRank = oMetadate.MinimumBuildRank = "guest";
                }

                oMetadate.CreationDate = 0; // This is ctime for when the world was created. Unsure on how to extract it. Opticraft makes no use of it as of yet
                serializer.WriteObject( serializationStream, oMetadate );
                byte[] jsonMetaData = serializationStream.ToArray();
                writer.Write( jsonMetaData.Length );
                writer.Write( jsonMetaData );

                // Now create and serialize core data store (zones)
                Zone[] zoneCache = mapToSave.Zones.Cache;
                OpticraftDataStore oDataStore = new OpticraftDataStore {
                    Zones = new OpticraftZone[zoneCache.Length]
                };
                int i = 0;
                foreach( Zone zone in zoneCache ) {
                    OpticraftZone oZone = new OpticraftZone {
                        Name = zone.Name,
                        MinimumRank = zone.Controller.MinRank.Name,
                        Owner = "",
                        X1 = zone.Bounds.XMin,
                        X2 = zone.Bounds.XMax,
                        Y1 = zone.Bounds.YMin,
                        Y2 = zone.Bounds.YMax,
                        Z1 = zone.Bounds.ZMin,
                        Z2 = zone.Bounds.ZMax,
                        Builders = new string[zone.Controller.ExceptionList.Included.Length]
                    };

                    // Bounds

                    // Builders
                    int j = 0;
                    foreach( PlayerInfo pInfo in zone.Controller.ExceptionList.Included ) {
                        oZone.Builders[j++] = pInfo.Name;
                    }

                    // Excluded players
                    oZone.Excluded = new string[zone.Controller.ExceptionList.Excluded.Length];
                    j = 0;
                    foreach( PlayerInfo pInfo in zone.Controller.ExceptionList.Excluded ) {
                        oZone.Builders[j++] = pInfo.Name;
                    }
                    oDataStore.Zones[i++] = oZone;
                }
                // Serialize it
                serializationStream = new MemoryStream();
                serializer = new DataContractJsonSerializer( typeof( OpticraftDataStore ) );
                serializer.WriteObject( serializationStream, oDataStore );
                byte[] jsonDataStore = serializationStream.ToArray();
                writer.Write( jsonDataStore.Length );
                writer.Write( jsonDataStore );


                // Blocks
                MemoryStream blockStream = new MemoryStream();
                using( GZipStream zipper = new GZipStream( blockStream, CompressionMode.Compress, true ) ) {
                    zipper.Write( mapToSave.Blocks, 0, mapToSave.Blocks.Length );
                }
                byte[] compressedBlocks = blockStream.ToArray();
                writer.Write( compressedBlocks.Length );
                writer.Write( compressedBlocks );

            }
            return true;
        }
    }
}