// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace fCraft {

    /// <summary> Static class with definitions of Minecraft color codes,
    /// parsers, converters, and utilities. </summary>
    public static class Color {
        public const string Black = "&0",
                            Navy = "&1",
                            Green = "&2",
                            Teal = "&3",
                            Maroon = "&4",
                            Purple = "&5",
                            Olive = "&6",
                            Silver = "&7",
                            Gray = "&8",
                            Blue = "&9",
                            Lime = "&a",
                            Aqua = "&b",
                            Red = "&c",
                            Magenta = "&d",
                            Yellow = "&e",
                            White = "&f";

        // User-defined color assignments. Set by Config.ApplyConfig.
        public static string Sys, Help, Say, Announcement, PM, IRC, Me, Custom, Warning;

        // Defaults for user-defined colors.
        public const string SysDefault = Yellow,
                            HelpDefault = Lime,
                            SayDefault = Green,
                            AnnouncementDefault = Green,
                            PMDefault = Aqua,
                            IRCDefault = Purple,
                            MeDefault = Purple,
                            WarningDefault = Red,
                            CustomDefault = Yellow;

        public static readonly SortedList<char, string> ColorNames = new SortedList<char, string>{
            { '0', "black" },
            { '1', "navy" },
            { '2', "green" },
            { '3', "teal" },
            { '4', "maroon" },
            { '5', "purple" },
            { '6', "olive" },
            { '7', "silver" },
            { '8', "gray" },
            { '9', "blue" },
            { 'a', "lime" },
            { 'b', "aqua" },
            { 'c', "red" },
            { 'd', "magenta" },
            { 'e', "yellow" },
            { 'f', "white" }
        };


        /// <summary> Gets color name for hex color code. </summary>
        /// <param name="code"> Hexadecimal color code (between '0' and 'f'). </param>
        /// <returns> Lowercase color name. </returns>
        [CanBeNull]
        public static string GetName( char code ) {
            code = Char.ToLower( code );
            if( IsValidColorCode( code ) ) {
                return ColorNames[code];
            }
            string color = Parse( code );
            if( color == null ) {
                return null;
            }
            return ColorNames[color[1]];
        }


        /// <summary> Gets color name for a numeric color code. </summary>
        /// <param name="index"> Ordinal numeric color code (between 0 and 15). </param>
        /// <returns> Lowercase color name. If input is out of range, returns null. </returns>
        [CanBeNull]
        public static string GetName( int index ) {
            if( index >= 0 && index <= 15 ) {
                return ColorNames.Values[index];
            } else {
                return null;
            }
        }


        /// <summary> Gets color name for a string representation of a color. </summary>
        /// <param name="color"> Any parsable string representation of a color. </param>
        /// <returns> Lowercase color name.
        /// If input is an empty string, returns empty string.
        /// If input is null or cannot be parsed, returns null. </returns>
        [CanBeNull]
        public static string GetName( string color ) {
            if( color == null ) {
                return null;
            } else if( color.Length == 0 ) {
                return "";
            } else {
                string parsedColor = Parse( color );
                if( parsedColor == null ) {
                    return null;
                } else {
                    return GetName( parsedColor[1] );
                }
            }
        }



        /// <summary> Parses a string to a format readable by Minecraft clients. 
        /// an accept color names and color codes (with or without the ampersand). </summary>
        /// <param name="code"> Color code character. </param>
        /// <returns> Two-character color string, readable by Minecraft client.
        /// If input is null or cannot be parsed, returns null. </returns>
        [CanBeNull]
        public static string Parse( char code ) {
            code = Char.ToLower( code );
            if( IsValidColorCode( code ) ) {
                return "&" + code;
            } else {
                switch( code ) {
                    case 's': return Sys;
                    case 'y': return Say;
                    case 'p': return PM;
                    case 'r': return Announcement;
                    case 'h': return Help;
                    case 'w': return Warning;
                    case 'm': return Me;
                    case 'i': return IRC;
                    default:
                        return null;
                }
            }
        }


        /// <summary> Parses a numeric color code to a string readable by Minecraft clients </summary>
        /// <param name="index"> Ordinal numeric color code (between 0 and 15). </param>
        /// <returns> Two-character color string, readable by Minecraft client.
        /// If input cannot be parsed, returns null. </returns>
        [CanBeNull]
        public static string Parse( int index ) {
            if( index >= 0 && index <= 15 ) {
                return "&" + ColorNames.Keys[index];
            } else {
                return null;
            }
        }


        /// <summary> Parses a string to a format readable by Minecraft clients. 
        /// an accept color names and color codes (with or without the ampersand). </summary>
        /// <param name="color"> Ordinal numeric color code (between 0 and 15). </param>
        /// <returns> Two-character color string, readable by Minecraft client.
        /// If input is an empty string, returns empty string.
        /// If input is null or cannot be parsed, returns null. </returns>
        [CanBeNull]
        public static string Parse( string color ) {
            if( color == null ) {
                return null;
            }
            color = color.ToLower();
            switch( color.Length ) {
                case 2:
                    if( color[0] == '&' && IsValidColorCode( color[1] ) ) {
                        return color;
                    }
                    break;

                case 1:
                    return Parse( color[0] );

                case 0:
                    return "";
            }
            if( ColorNames.ContainsValue( color ) ) {
                return "&" + ColorNames.Keys[ColorNames.IndexOfValue( color )];
            } else {
                return null;
            }
        }


        public static int ParseToIndex( [NotNull] string color ) {
            if( color == null ) throw new ArgumentNullException( "color" );
            color = color.ToLower();
            if( color.Length == 2 && color[0] == '&' ) {
                if( ColorNames.ContainsKey( color[1] ) ) {
                    return ColorNames.IndexOfKey( color[1] );
                } else {
                    switch( color ) {
                        case "&s": return ColorNames.IndexOfKey( Sys[1] );
                        case "&y": return ColorNames.IndexOfKey( Say[1] );
                        case "&p": return ColorNames.IndexOfKey( PM[1] );
                        case "&r": return ColorNames.IndexOfKey( Announcement[1] );
                        case "&h": return ColorNames.IndexOfKey( Help[1] );
                        case "&w": return ColorNames.IndexOfKey( Warning[1] );
                        case "&m": return ColorNames.IndexOfKey( Me[1] );
                        case "&i": return ColorNames.IndexOfKey( IRC[1] );
                        default: return 15;
                    }
                }
            } else if( ColorNames.ContainsValue( color ) ) {
                return ColorNames.IndexOfValue( color );
            } else {
                return 15; // white
            }
        }



        /// <summary>
        /// Checks whether a color code is valid (checks if it's hexadecimal char).
        /// </summary>
        /// <returns>True is char is valid, otherwise false</returns>
        public static bool IsValidColorCode( char code ) {
            return (code >= '0' && code <= '9') || (code >= 'a' && code <= 'f') || (code >= 'A' && code <= 'F');
        }

        public static void ReplacePercentCodes( [NotNull] StringBuilder sb ) {
            if( sb == null ) throw new ArgumentNullException( "sb" );
            sb.Replace( "%0", "&0" );
            sb.Replace( "%1", "&1" );
            sb.Replace( "%2", "&2" );
            sb.Replace( "%3", "&3" );
            sb.Replace( "%4", "&4" );
            sb.Replace( "%5", "&5" );
            sb.Replace( "%6", "&6" );
            sb.Replace( "%7", "&7" );
            sb.Replace( "%8", "&8" );
            sb.Replace( "%9", "&9" );
            sb.Replace( "%a", "&a" );
            sb.Replace( "%b", "&b" );
            sb.Replace( "%c", "&c" );
            sb.Replace( "%d", "&d" );
            sb.Replace( "%e", "&e" );
            sb.Replace( "%f", "&f" );
            sb.Replace( "%A", "&a" );
            sb.Replace( "%B", "&b" );
            sb.Replace( "%C", "&c" );
            sb.Replace( "%D", "&d" );
            sb.Replace( "%E", "&e" );
            sb.Replace( "%F", "&f" );
        }

        public static string ReplacePercentCodes( [NotNull] string message ) {
            if( message == null ) throw new ArgumentNullException( "message" );
            StringBuilder sb = new StringBuilder( message );
            ReplacePercentCodes( sb );
            return sb.ToString();
        }


        public static string SubstituteSpecialColors( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( input );
            for( int i = sb.Length - 1; i > 0; i-- ) {
                if( sb[i - 1] == '&' ) {
                    switch( Char.ToLower( sb[i] ) ) {
                        case 's': sb[i] = Sys[1]; break;
                        case 'y': sb[i] = Say[1]; break;
                        case 'p': sb[i] = PM[1]; break;
                        case 'r': sb[i] = Announcement[1]; break;
                        case 'h': sb[i] = Help[1]; break;
                        case 'w': sb[i] = Warning[1]; break;
                        case 'm': sb[i] = Me[1]; break;
                        case 'i': sb[i] = IRC[1]; break;
                        default:
                            if( IsValidColorCode( sb[i] ) ) {
                                continue;
                            } else {
                                sb.Remove( i - 1, 1 );
                            }
                            break;
                    }
                }
            }
            return sb.ToString();
        }


        /// <summary> Strips all ampersand color codes, and unescapes doubled-up ampersands. </summary>
        public static string StripColors( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            if( input.IndexOf( '&' ) == -1 ) {
                return input;
            } else {
                StringBuilder output = new StringBuilder( input.Length );
                for( int i = 0; i < input.Length; i++ ) {
                    if( input[i] == '&' ) {
                        if( i == input.Length - 1 ) {
                            break;
                        } else if( input[i + 1] == '&' ) {
                            output.Append( '&' );
                        }
                        i++;
                    } else {
                        output.Append( input[i] );
                    }
                }
                return output.ToString();
            }
        }


        #region IRC Colors

        public const string IRCReset = "\u0003\u000f";
        public const string IRCBold = "\u0002";

        static readonly Dictionary<string, IRCColor> MinecraftToIRCColors = new Dictionary<string, IRCColor> {
            { White, IRCColor.White },
            { Black, IRCColor.Black },
            { Navy, IRCColor.Navy },
            { Green, IRCColor.Green },
            { Red, IRCColor.Red },
            { Maroon, IRCColor.Maroon },
            { Purple, IRCColor.Purple },
            { Olive, IRCColor.Olive },
            { Yellow, IRCColor.Yellow },
            { Lime, IRCColor.Lime },
            { Teal, IRCColor.Teal },
            { Aqua, IRCColor.Aqua },
            { Blue, IRCColor.Blue },
            { Magenta, IRCColor.Magenta },
            { Gray, IRCColor.Gray },
            { Silver, IRCColor.Silver },
        };


        public static string EscapeAmpersands( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            return input.Replace( "&", "&&" );
        }


        public static string ToIRCColorCodes( [NotNull] string input ) {
            if( input == null ) throw new ArgumentNullException( "input" );
            StringBuilder sb = new StringBuilder( SubstituteSpecialColors( input ) );

            foreach( KeyValuePair<string, IRCColor> code in MinecraftToIRCColors ) {
                sb.Replace( code.Key, '\u0003' + ((int)code.Value).ToString().PadLeft( 2, '0' ) );
            }
            return sb.ToString();
        }

        #endregion
    }


    enum IRCColor {
        White = 0,
        Black,
        Navy,
        Green,
        Red,
        Maroon,
        Purple,
        Olive,
        Yellow,
        Lime,
        Teal,
        Aqua,
        Blue,
        Magenta,
        Gray,
        Silver
    }
}