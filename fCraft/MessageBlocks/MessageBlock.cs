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
using fCraft.Drawing;
using System.Threading;
using System.Runtime.Serialization;

namespace fCraft {
    public class MessageBlock {
        public String Name { get; set; }
        public String Creator { get; set; }
        public DateTime Created { get; set; }
        public String World { get; set; }
        public Vector3I AffectedBlock { get; set; }
        public MessageBlockRange Range { get; set; }
        public String Message { get; set; }

        public MessageBlock () {
            //empty
        }

        public MessageBlock ( String world, Vector3I affectedBlock, String Name, String Creator, String Message ) {
            this.World = world;
            this.AffectedBlock = affectedBlock;
            this.Range = MessageBlock.CalculateRange( this );
            this.Name = Name;
            this.Creator = Creator;
            this.Created = DateTime.UtcNow;
            this.Message = Message;
        }

        public static MessageBlockRange CalculateRange ( MessageBlock MessageBlock ) {
            MessageBlockRange range = new MessageBlockRange( 0, 0, 0, 0, 0, 0 );
            range.Xmin = MessageBlock.AffectedBlock.X;
            range.Xmax = MessageBlock.AffectedBlock.X;
            range.Ymin = MessageBlock.AffectedBlock.Y;
            range.Ymax = MessageBlock.AffectedBlock.Y;
            range.Zmin = MessageBlock.AffectedBlock.Z;
            range.Zmax = MessageBlock.AffectedBlock.Z;
            return range;
        }

        public bool IsInRange ( Player player ) {
            if ( ( player.Position.X / 32 ) <= Range.Xmax + 1 && ( player.Position.X / 32 ) >= Range.Xmin - 1 ) {
                if ( ( player.Position.Y / 32 ) <= Range.Ymax + 1 && ( player.Position.Y / 32 ) >= Range.Ymin - 1 ) {
                    if ( ( ( player.Position.Z / 32 ) - 1 ) <= Range.Zmax + 1 && ( ( player.Position.Z / 32 ) - 1 ) >= Range.Zmin - 1 ) {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsInRange ( Vector3I vector ) {
            if ( vector.X <= Range.Xmax && vector.X >= Range.Xmin ) {
                if ( vector.Y <= Range.Ymax && vector.Y >= Range.Ymin ) {
                    if ( vector.Z <= Range.Zmax && vector.Z >= Range.Zmin ) {
                        return true;
                    }
                }
            }

            return false;
        }

        public String GetMessage () {
            if ( this.Message == null ) return "";
            if ( this.Message.Length < 1 ) return "";
            string SortedMessage = Color.ReplacePercentCodes( Message );
            SortedMessage = Chat.ReplaceEmoteKeywords( SortedMessage );
            return String.Format( "MessageBlock: {0}{1}", Color.Green, SortedMessage );
        }

        public static String GenerateName ( World world ) {
            if ( world.Map.MessageBlocks != null ) {
                if ( world.Map.MessageBlocks.Count > 0 ) {
                    bool found = false;

                    while ( !found ) {
                        bool taken = false;

                        foreach ( MessageBlock MessageBlock in world.Map.MessageBlocks ) {
                            if ( MessageBlock.Name.Equals( "MessageBlock" + world.Map.MessageBlockID ) ) {
                                taken = true;
                                break;
                            }
                        }

                        if ( !taken ) {
                            found = true;
                        } else {
                            world.Map.MessageBlockID++;
                        }
                    }

                    return "MessageBlock" + world.Map.MessageBlockID;
                }
            }

            return "MessageBlock1";
        }

        public static bool DoesNameExist ( World world, String name ) {
            if ( world.Map.MessageBlocks != null ) {
                if ( world.Map.MessageBlocks.Count > 0 ) {
                    foreach ( MessageBlock MessageBlock in world.Map.MessageBlocks ) {
                        if ( MessageBlock.Name.Equals( name ) ) {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void Remove ( Player requester ) {
            NormalBrush brush = new NormalBrush( Block.Air, Block.Air );
            DrawOperation removeOperation = new CuboidDrawOperation( requester );
            removeOperation.AnnounceCompletion = false;
            removeOperation.Brush = brush;
            removeOperation.Context = BlockChangeContext.Drawn;

            if ( this.AffectedBlock == null ) {
                this.AffectedBlock = new Vector3I( Range.Xmin, Range.Ymin, Range.Zmin );
            }

            if ( !removeOperation.Prepare( new[] { this.AffectedBlock } ) ) {
                throw new Exception( "Unable to remove MessageBlock." );
            }

            removeOperation.Begin();

            lock ( requester.World.Map.MessageBlocks.SyncRoot ) {
                requester.World.Map.MessageBlocks.Remove( this );
            }
        }

        public string Serialize () {
            SerializedData data = new SerializedData( this );
            DataContractSerializer serializer = new DataContractSerializer( typeof( SerializedData ) );
            System.IO.MemoryStream s = new System.IO.MemoryStream();
            serializer.WriteObject( s, data );
            return Convert.ToBase64String( s.ToArray() );
        }

        public static MessageBlock Deserialize ( string name, string sdata, Map map ) {
            byte[] bdata = Convert.FromBase64String( sdata );
            MessageBlock MessageBlock = new MessageBlock();
            DataContractSerializer serializer = new DataContractSerializer( typeof( SerializedData ) );
            System.IO.MemoryStream s = new System.IO.MemoryStream( bdata );
            SerializedData data = ( SerializedData )serializer.ReadObject( s );

            data.UpdateMessageBlock( MessageBlock );
            return MessageBlock;
        }
        [DataContract]
        private class SerializedData {
            [DataMember]
            public String Name;
            [DataMember]
            public String Creator;
            [DataMember]
            public DateTime Created;
            [DataMember]
            public String World;
            [DataMember]
            public Vector3I AffectedBlock;
            [DataMember]
            public int XMin;
            [DataMember]
            public int XMax;
            [DataMember]
            public int YMin;
            [DataMember]
            public int YMax;
            [DataMember]
            public int ZMin;
            [DataMember]
            public int ZMax;
            [DataMember]
            public String Message;

            public SerializedData ( MessageBlock MessageBlock ) {
                lock ( MessageBlock ) {
                    Name = MessageBlock.Name;
                    Creator = MessageBlock.Creator;
                    Created = MessageBlock.Created;
                    World = MessageBlock.World;
                    AffectedBlock = MessageBlock.AffectedBlock;
                    XMin = MessageBlock.Range.Xmin;
                    XMax = MessageBlock.Range.Xmax;
                    YMin = MessageBlock.Range.Ymin;
                    YMax = MessageBlock.Range.Ymax;
                    ZMin = MessageBlock.Range.Zmin;
                    ZMax = MessageBlock.Range.Zmax;
                    Message = MessageBlock.Message;
                }
            }

            public void UpdateMessageBlock ( MessageBlock MessageBlock ) {
                MessageBlock.Name = Name;
                MessageBlock.Creator = Creator;
                MessageBlock.Created = Created;
                MessageBlock.World = World;
                MessageBlock.AffectedBlock = AffectedBlock;
                MessageBlock.Range = new MessageBlockRange( XMin, XMax, YMin, YMax, ZMin, ZMax );
                MessageBlock.Message = Message;
            }
        }
    }
}
