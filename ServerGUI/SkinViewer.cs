using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace fCraft.ServerGUI {
    public partial class SkinViewer : Form {
        PlayerInfo player;
        Image webSkin;
        public SkinViewer ( fCraft.PlayerInfo player_ ) {
            InitializeComponent();
            player = player_;
            PlayerInfo info = player;
            GetSetSkin();
            SetPlayerInfoText( info );
        }

        void SetPlayerInfoText ( PlayerInfo info ) {
            textBox1.Text = "";
            PlayerLabel.Text = player.Name;
            SetTextRankColor( Color.GetName( player.Rank.Color ) );
            if ( info.LastIP.Equals( System.Net.IPAddress.None ) ) {
                textBox1.Text += String.Format( "About {0}&S: Never seen before.\r\n", info.ClassyName );

            } else {
                if ( info != null ) {
                    TimeSpan idle = info.PlayerObject.IdleTime;
                    if ( info.IsHidden ) {
                        if ( idle.TotalMinutes > 2 ) {
                            if ( player.Can( Permission.ViewPlayerIPs ) ) {
                                textBox1.Text += String.Format( "About {0}&S: HIDDEN from {1} (idle {2})\r\n",
                                                info.ClassyName,
                                                info.LastIP,
                                                idle.ToMiniString() );
                            } else {
                                textBox1.Text += String.Format( "About {0}&S: HIDDEN (idle {1})\r\n",
                                                info.ClassyName,
                                                idle.ToMiniString() );
                            }
                        } else {
                            if ( player.Can( Permission.ViewPlayerIPs ) ) {
                                textBox1.Text += String.Format( "About {0}&S: HIDDEN. Online from {1}\r\n",
                                                info.ClassyName,
                                                info.LastIP );
                            } else {
                                textBox1.Text += String.Format( "About {0}&S: HIDDEN.\r\n",
                                                info.ClassyName );
                            }
                        }
                    } else {
                        if ( idle.TotalMinutes > 1 ) {
                            if ( player.Can( Permission.ViewPlayerIPs ) ) {
                                textBox1.Text += String.Format( "About {0}&S: Online now from {1} (idle {2})\r\n",
                                                info.ClassyName,
                                                info.LastIP,
                                                idle.ToMiniString() );
                            } else {
                                textBox1.Text += String.Format( "About {0}&S: Online now (idle {1})\r\n",
                                                info.ClassyName,
                                                idle.ToMiniString() );
                            }
                        } else {
                            if ( player.Can( Permission.ViewPlayerIPs ) ) {
                                textBox1.Text += String.Format( "About {0}&S: Online now from {1}\r\n",
                                                info.ClassyName,
                                                info.LastIP );
                            } else {
                                textBox1.Text += String.Format( "About {0}&S: Online now.\r\n",
                                                info.ClassyName );
                            }
                        }
                    }
                } else {
                    if ( player.Can( Permission.ViewPlayerIPs ) ) {
                        if ( info.LeaveReason != LeaveReason.Unknown ) {
                            textBox1.Text += String.Format( "About {0}&S: Last seen {1} ago from {2} ({3}).\r\n",
                                            info.ClassyName,
                                            info.TimeSinceLastSeen.ToMiniString(),
                                            info.LastIP,
                                            info.LeaveReason );
                        } else {
                            textBox1.Text += String.Format( "About {0}&S: Last seen {1} ago from {2}.\r\n",
                                            info.ClassyName,
                                            info.TimeSinceLastSeen.ToMiniString(),
                                            info.LastIP );
                        }
                    } else {
                        if ( info.LeaveReason != LeaveReason.Unknown ) {
                            textBox1.Text += String.Format( "About {0}&S: Last seen {1} ago ({2}).\r\n",
                                            info.ClassyName,
                                            info.TimeSinceLastSeen.ToMiniString(),
                                            info.LeaveReason );
                        } else {
                            textBox1.Text += String.Format( "About {0}&S: Last seen {1} ago.\r\n",
                                            info.ClassyName,
                                            info.TimeSinceLastSeen.ToMiniString() );
                        }
                    }
                }
                // Show login information
                textBox1.Text += String.Format( "  Logged in {0} time(s) since {1:d MMM yyyy}.\r\n",
                                info.TimesVisited,
                                info.FirstLoginDate );
            }

            if ( info.IsFrozen ) {
                textBox1.Text += String.Format( "  Frozen {0} ago by {1}\r\n",
                                info.TimeSinceFrozen.ToMiniString(),
                                info.FrozenByClassy );
            }

            if ( info.IsMuted ) {
                textBox1.Text += String.Format( "  Muted for {0} by {1}\r\n",
                                info.TimeMutedLeft.ToMiniString(),
                                info.MutedByClassy );
                float blocks = ( ( info.BlocksBuilt + info.BlocksDrawn ) - info.BlocksDeleted );
                if ( blocks < 0 )
                    textBox1.Text += String.Format( "  &CWARNING! {0}&S has deleted more than built!\r\n", info.ClassyName );//<---- GlennMR on Au70 Galaxy
            }

            // Show ban information
            IPBanInfo ipBan = IPBanList.Get( info.LastIP );
            switch ( info.BanStatus ) {
                case BanStatus.Banned:
                    if ( ipBan != null ) {
                        textBox1.Text += String.Format( "  Account and IP are &CBANNED&S. See &H/BanInfo\r\n" );
                    } else {
                        textBox1.Text += String.Format( "  Account is &CBANNED&S. See &H/BanInfo\r\n" );
                    }
                    break;
                case BanStatus.IPBanExempt:
                    if ( ipBan != null ) {
                        textBox1.Text += String.Format( "  IP is &CBANNED&S, but account is exempt. See &H/BanInfo\r\n" );
                    } else {
                        textBox1.Text += String.Format( "  IP is not banned, and account is exempt. See &H/BanInfo\r\n" );
                    }
                    break;
                case BanStatus.NotBanned:
                    if ( ipBan != null ) {
                        textBox1.Text += String.Format( "  IP is &CBANNED&S. See &H/BanInfo\r\n" );

                    }
                    break;
            }


            if ( !info.LastIP.Equals( System.Net.IPAddress.None ) ) {
                // Show alts
                List<PlayerInfo> altNames = new List<PlayerInfo>();
                int bannedAltCount = 0;
                foreach ( PlayerInfo playerFromSameIP in PlayerDB.FindPlayers( info.LastIP ) ) {
                    if ( playerFromSameIP == info ) continue;
                    altNames.Add( playerFromSameIP );
                    if ( playerFromSameIP.IsBanned ) {
                        bannedAltCount++;
                    }
                }


                // Stats
                if ( info.BlocksDrawn > 500000000 ) {
                    textBox1.Text += String.Format( "  Built {0} and deleted {1} blocks, drew {2}M blocks, wrote {3} messages.\r\n",
                                    info.BlocksBuilt,
                                    info.BlocksDeleted,
                                    info.BlocksDrawn / 1000000,
                                    info.MessagesWritten );
                } else if ( info.BlocksDrawn > 500000 ) {
                    textBox1.Text += String.Format( "  Built {0} and deleted {1} blocks, drew {2}K blocks, wrote {3} messages.\r\n",
                                    info.BlocksBuilt,
                                    info.BlocksDeleted,
                                    info.BlocksDrawn / 1000,
                                    info.MessagesWritten );
                } else if ( info.BlocksDrawn > 0 ) {
                    textBox1.Text += String.Format( "  Built {0} and deleted {1} blocks, drew {2} blocks, wrote {3} messages.\r\n",
                                    info.BlocksBuilt,
                                    info.BlocksDeleted,
                                    info.BlocksDrawn,
                                    info.MessagesWritten );
                } else {
                    textBox1.Text += String.Format( "  Built {0} and deleted {1} blocks, wrote {2} messages.\r\n",
                                    info.BlocksBuilt,
                                    info.BlocksDeleted,
                                    info.MessagesWritten );
                }


                // More stats
                if ( info.TimesBannedOthers > 0 || info.TimesKickedOthers > 0 || info.PromoCount > 0 ) {
                    textBox1.Text += String.Format( "  Kicked {0}, Promoted {1} and banned {2} players.\r\n", info.TimesKickedOthers, info.PromoCount, info.TimesBannedOthers );
                }

                if ( info.TimesKicked > 0 ) {
                    if ( info.LastKickDate != DateTime.MinValue ) {
                        textBox1.Text += String.Format( "  Got kicked {0} times. Last kick {1} ago by {2}\r\n",
                                        info.TimesKicked,
                                        info.TimeSinceLastKick.ToMiniString(),
                                        info.LastKickByClassy );
                    } else {
                        textBox1.Text += String.Format( "  Got kicked {0} times.\r\n", info.TimesKicked );
                    }
                    if ( info.LastKickReason != null ) {
                        textBox1.Text += String.Format( "  Kick reason: {0}\r\n", info.LastKickReason );
                    }
                }


                // Promotion/demotion
                if ( info.PreviousRank == null ) {
                    if ( info.RankChangedBy == null ) {
                        textBox1.Text += String.Format( "  Rank is {0}&S (default).\r\n",
                                        info.Rank.ClassyName );
                    } else {
                        textBox1.Text += String.Format( "  Promoted to {0}&S by {1}&S {2} ago.\r\n",
                                        info.Rank.ClassyName,
                                        info.RankChangedByClassy,
                                        info.TimeSinceRankChange.ToMiniString() );
                        if ( info.RankChangeReason != null ) {
                            textBox1.Text += String.Format( "  Promotion reason: {0}\r\n", info.RankChangeReason );
                        }
                    }
                } else if ( info.PreviousRank <= info.Rank ) {
                    textBox1.Text += String.Format( "  Promoted from {0}&S to {1}&S by {2}&S {3} ago.\r\n",
                                    info.PreviousRank.ClassyName,
                                    info.Rank.ClassyName,
                                    info.RankChangedByClassy,
                                    info.TimeSinceRankChange.ToMiniString() );
                    if ( info.RankChangeReason != null ) {
                        textBox1.Text += String.Format( "  Promotion reason: {0}\r\n", info.RankChangeReason );
                    }
                } else {
                    textBox1.Text += String.Format( "  Demoted from {0}&S to {1}&S by {2}&S {3} ago.\r\n",
                                    info.PreviousRank.ClassyName,
                                    info.Rank.ClassyName,
                                    info.RankChangedByClassy,
                                    info.TimeSinceRankChange.ToMiniString() );
                    if ( info.RankChangeReason != null ) {
                        textBox1.Text += String.Format( "  Demotion reason: {0}\r\n", info.RankChangeReason );
                    }
                }

                if ( !info.LastIP.Equals( System.Net.IPAddress.None ) ) {
                    // Time on the server
                    TimeSpan totalTime = info.TotalTime;
                    if ( info != null ) {
                        totalTime = totalTime.Add( info.TimeSinceLastLogin );
                    }
                    textBox1.Text += String.Format( "  Spent a total of {0:F1} hours ({1:F1} minutes) here.\r\n",
                                    totalTime.TotalHours,
                                    totalTime.TotalMinutes );
                }
                textBox1.Text = Color.StripColors( textBox1.Text );
            }
        }
        void GetSetSkin () {
            GetSkin(); //get the skin (returns EmptySkin() if null or Exception)
            Bitmap temp = new Bitmap( 16, 32 ); //finished skin size
            Rectangle head = new Rectangle( 8, 8, 8, 8 );
            Rectangle headOverlay = new Rectangle( 40, 8, 8, 8 );
            //stitch together the skin in 2d from the front
            CopyRegionIntoImage( webSkin as Bitmap, head, temp, new Rectangle( 4, 0, 8, 8 ) );//head
            CopyRegionIntoImage( webSkin as Bitmap, headOverlay, temp, new Rectangle( 4, 0, 8, 8 ) );//headoverlay
            CopyRegionIntoImage( webSkin as Bitmap, new Rectangle( 20, 20, 8, 12 ), temp, new Rectangle( 4, 8, 8, 12 ) );//body
            CopyRegionIntoImage( webSkin as Bitmap, new Rectangle( 0, 20, 4, 12 ), temp, new Rectangle( 4, 20, 4, 12 ) );//left leg
            CopyRegionIntoImage( webSkin as Bitmap, new Rectangle( 12, 20, 4, 12 ), temp, new Rectangle( 8, 20, 4, 12 ) );//right leg
            CopyRegionIntoImage( webSkin as Bitmap, new Rectangle( 40, 20, 4, 12 ), temp, new Rectangle( 0, 8, 4, 12 ) );//left arm
            CopyRegionIntoImage( webSkin as Bitmap, new Rectangle( 52, 20, 4, 12 ), temp, new Rectangle( 12, 8, 4, 12 ) );//right arm

            //make picturebox the size of the head
            pictureBox1.Image = new Bitmap( 8, 8 );
            CopyRegionIntoImage( webSkin as Bitmap, head, pictureBox1.Image as Bitmap, new Rectangle( 0, 0, 8, 8 ) ); //copy over the head
            CopyRegionIntoImage( webSkin as Bitmap, headOverlay, pictureBox1.Image as Bitmap, new Rectangle( 0, 0, 8, 8 ) ); //and overlay
            pictureBox1.Image = MakeGrayscale( pictureBox1.Image as Bitmap ); //make it black and white
            pictureBox1.Image = ResizeBitmap( pictureBox1.Image as Bitmap, 148, 148 ); //resize to picturebox max size
            temp = ResizeBitmap( temp, 64, 128 ); //resize the stitched skin and then copy it onto the giant b/w head
            CopyRegionIntoImage( temp, 
                new Rectangle( 0, 0, temp.Width, temp.Height ), 
                pictureBox1.Image as Bitmap, 
                new Rectangle( pictureBox1.Image.Width - temp.Width, 
                    pictureBox1.Height - ( temp.Height - 8 ), 
                    temp.Width, 
                    temp.Height ) );
        }

        void SetTextRankColor ( string c ) {
            switch ( c.ToLower() ) {
                case "black":
                    PlayerLabel.ForeColor = System.Drawing.Color.Black;
                    break;
                case "navy":
                    PlayerLabel.ForeColor = System.Drawing.Color.Navy;
                    break;
                case "green":
                    PlayerLabel.ForeColor = System.Drawing.Color.Green;
                    break;
                case "teal":
                    PlayerLabel.ForeColor = System.Drawing.Color.Teal;
                    break;
                case "maroon":
                    PlayerLabel.ForeColor = System.Drawing.Color.Maroon;
                    break;
                case "purple":
                    PlayerLabel.ForeColor = System.Drawing.Color.Purple;
                    break;
                case "olive":
                    PlayerLabel.ForeColor = System.Drawing.Color.Olive;
                    break;
                case "silver":
                    PlayerLabel.ForeColor = System.Drawing.Color.Silver;
                    break;
                case "gray":
                    PlayerLabel.ForeColor = System.Drawing.Color.Gray;
                    break;
                case "blue":
                    PlayerLabel.ForeColor = System.Drawing.Color.Blue;
                    break;
                case "lime":
                    PlayerLabel.ForeColor = System.Drawing.Color.Lime;
                    break;
                case "aqua":
                    PlayerLabel.ForeColor = System.Drawing.Color.Aqua;
                    break;
                case "red":
                    PlayerLabel.ForeColor = System.Drawing.Color.Red;
                    break;
                case "magenta":
                    PlayerLabel.ForeColor = System.Drawing.Color.Magenta;
                    break;
                case "yellow":
                    PlayerLabel.ForeColor = System.Drawing.Color.Yellow;
                    break;
                case "white":
                    PlayerLabel.ForeColor = System.Drawing.Color.White;
                    break;
            }
        }

        private static Bitmap ResizeBitmap ( Bitmap sourceBMP, int width, int height ) {
            Bitmap result = new Bitmap( width, height );
            using ( Graphics g = Graphics.FromImage( result ) ) {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.DrawImage( sourceBMP, 0, 0, width, height );
            }
            return result;
        }
        private static Image EmptySkin {
            get {
                Image image = ( Image )new Bitmap( 64, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb );
                using ( Graphics graphics = Graphics.FromImage( image ) ) {
                    graphics.Clear( System.Drawing.Color.Transparent );
                    using ( Brush brush1 = ( Brush )new SolidBrush( System.Drawing.Color.DimGray ) ) {
                        using ( Brush brush2 = ( Brush )new SolidBrush( System.Drawing.Color.DarkGray ) ) {
                            using ( Brush brush3 = ( Brush )new SolidBrush( System.Drawing.Color.LightGray ) ) {
                                graphics.FillRectangle( brush1, new Rectangle( 8, 0, 8, 8 ) );
                                graphics.FillRectangle( brush1, new Rectangle( 4, 16, 4, 4 ) );
                                graphics.FillRectangle( brush1, new Rectangle( 20, 16, 8, 4 ) );
                                graphics.FillRectangle( brush1, new Rectangle( 44, 16, 4, 4 ) );
                                graphics.FillRectangle( brush2, new Rectangle( 0, 8, 8, 8 ) );
                                graphics.FillRectangle( brush2, new Rectangle( 16, 8, 8, 8 ) );
                                graphics.FillRectangle( brush2, new Rectangle( 0, 20, 4, 12 ) );
                                graphics.FillRectangle( brush2, new Rectangle( 8, 20, 4, 12 ) );
                                graphics.FillRectangle( brush2, new Rectangle( 16, 20, 4, 12 ) );
                                graphics.FillRectangle( brush2, new Rectangle( 28, 20, 4, 12 ) );
                                graphics.FillRectangle( brush2, new Rectangle( 40, 20, 4, 12 ) );
                                graphics.FillRectangle( brush2, new Rectangle( 48, 20, 4, 12 ) );
                                graphics.FillRectangle( brush3, new Rectangle( 8, 8, 8, 8 ) );
                                graphics.FillRectangle( brush3, new Rectangle( 24, 8, 8, 8 ) );
                                graphics.FillRectangle( brush3, new Rectangle( 4, 20, 4, 12 ) );
                                graphics.FillRectangle( brush3, new Rectangle( 12, 20, 4, 12 ) );
                                graphics.FillRectangle( brush3, new Rectangle( 20, 20, 8, 12 ) );
                                graphics.FillRectangle( brush3, new Rectangle( 32, 20, 8, 12 ) );
                                graphics.FillRectangle( brush3, new Rectangle( 44, 20, 4, 12 ) );
                                graphics.FillRectangle( brush3, new Rectangle( 52, 20, 4, 12 ) );
                                graphics.FillRectangle( brush1, new Rectangle( 16, 0, 8, 8 ) );
                                graphics.FillRectangle( brush1, new Rectangle( 8, 16, 4, 4 ) );
                                graphics.FillRectangle( brush1, new Rectangle( 28, 16, 8, 4 ) );
                                graphics.FillRectangle( brush1, new Rectangle( 48, 16, 4, 4 ) );
                            }
                        }
                    }
                }
                return image;
            }
        }

        private static void CopyRegionIntoImage ( Bitmap srcBitmap, Rectangle srcRegion, Bitmap destBitmap, Rectangle destRegion ) {
            using ( Graphics grD = Graphics.FromImage( destBitmap ) ) {
                grD.DrawImage( srcBitmap, destRegion, srcRegion, GraphicsUnit.Pixel );
            }
        }
        private void GetSkin () {
            System.Net.WebClient webClient = new System.Net.WebClient();
            try {
                byte[] buffer;
                buffer = webClient.DownloadData( "http://s3.amazonaws.com/MinecraftSkins/" + this.player.Name + ".png" );
                if ( this.webSkin != null )
                    this.webSkin.Dispose();
                this.webSkin = Image.FromStream( ( System.IO.Stream )new System.IO.MemoryStream( buffer ) );
            } catch {
                this.webSkin = ( Image )null;
            }
        }
        public static Bitmap MakeGrayscale ( Bitmap original ) {
            unsafe {
                //create an empty bitmap the same size as original
                Bitmap newBitmap = new Bitmap( original.Width, original.Height );

                //lock the original bitmap in memory
                System.Drawing.Imaging.BitmapData originalData = original.LockBits(
                   new Rectangle( 0, 0, original.Width, original.Height ),
                   System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb );

                //lock the new bitmap in memory
                System.Drawing.Imaging.BitmapData newData = newBitmap.LockBits(
                   new Rectangle( 0, 0, original.Width, original.Height ),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb );

                //set the number of bytes per pixel
                int pixelSize = 3;

                for ( int y = 0; y < original.Height; y++ ) {
                    //get the data from the original image
                    byte* oRow = ( byte* )originalData.Scan0 + ( y * originalData.Stride );

                    //get the data from the new image
                    byte* nRow = ( byte* )newData.Scan0 + ( y * newData.Stride );

                    for ( int x = 0; x < original.Width; x++ ) {
                        //create the grayscale version
                        byte grayScale =
                           ( byte )( ( oRow[x * pixelSize] * .11 ) + //B
                           ( oRow[x * pixelSize + 1] * .59 ) +  //G
                           ( oRow[x * pixelSize + 2] * .3 ) ); //R

                        //set the new image's pixel to the grayscale version
                        nRow[x * pixelSize] = grayScale; //B
                        nRow[x * pixelSize + 1] = grayScale; //G
                        nRow[x * pixelSize + 2] = grayScale; //R
                    }
                }

                //unlock the bitmaps
                newBitmap.UnlockBits( newData );
                original.UnlockBits( originalData );

                return newBitmap;
            }
        }
    }
}