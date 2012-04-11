// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace fCraft {

    /// <summary> Describes attributes and metadata of a configuration key. </summary>
    [AttributeUsage( AttributeTargets.Field )]
    public class ConfigKeyAttribute : DescriptionAttribute {
        protected ConfigKeyAttribute( ConfigSection section, [NotNull] Type valueType, object defaultValue, [NotNull] string description )
            : base( description ) {
            if( valueType == null ) throw new ArgumentNullException( "valueType" );
            if( description == null ) throw new ArgumentNullException( "description" );
            ValueType = valueType;
            DefaultValue = defaultValue;
            Section = section;
            NotBlank = false;
        }
        public Type ValueType { get; protected set; }
        public object DefaultValue { get; protected set; }
        public ConfigSection Section { get; private set; }
        // ReSharper disable MemberCanBeProtected.Global
        public bool NotBlank { get; set; }
        // ReSharper restore MemberCanBeProtected.Global
        public ConfigKey Key { get; internal set; }


        public bool TryValidate( string value ) {
            try {
                Validate( value );
                return true;
            } catch( ArgumentException ) {
                return false;
            }
        }


        public virtual void Validate( [NotNull] string value ) {
            if( value == null ) throw new ArgumentNullException( "value" );
            if( NotBlank && value.Length == 0 ) {
                throw new FormatException( "Value cannot be blank or null." );
            }
        }


        [DebuggerStepThrough]
        public virtual string Process( string value ) {
            return value;
        }
    }


    internal sealed class StringKeyAttribute : ConfigKeyAttribute {
        public const int NoLengthRestriction = -1;
        public StringKeyAttribute( ConfigSection section, object defaultValue, string description )
            : base( section, typeof( string ), defaultValue, description ) {
            MinLength = NoLengthRestriction;
            MaxLength = NoLengthRestriction;
            Regex = null;
        }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public Regex Regex { get; set; }
        public bool RestrictedChars { get; set; }


        public override void Validate( string value ) {
            base.Validate( value );
            if( MinLength != NoLengthRestriction && value.Length < MinLength ) {
                throw new FormatException( String.Format( "Value string is too short; expected at least {0} characters.",
                                                          MinLength ) );
            }
            if( MaxLength != NoLengthRestriction && value.Length > MaxLength ) {
                throw new FormatException( String.Format( "Value string too long; expected at most {0} characters.",
                                                          MaxLength ) );
            }
            if( RestrictedChars && Chat.ContainsInvalidChars( value ) ) {
                throw new FormatException( String.Format( "Value contains restricted characters." ) );
            }
            if( Regex != null && !Regex.IsMatch( value ) ) {
                throw new FormatException( String.Format( "Value does not match the expected format: /{0}/.",
                                                          Regex ) );
            }
        }
    }


    internal sealed class IntKeyAttribute : ConfigKeyAttribute {
        public IntKeyAttribute( ConfigSection section, int defaultValue, string description )
            : base( section, typeof( int ), defaultValue, description ) {
            MinValue = int.MinValue;
            MaxValue = int.MaxValue;
            PowerOfTwo = false;
            MultipleOf = 0;
            ValidValues = null;
            InvalidValues = null;
        }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public bool PowerOfTwo { get; set; }
        public int MultipleOf { get; set; }
        public int[] ValidValues { get; set; }
        public int[] InvalidValues { get; set; }
        public bool AlwaysAllowZero { get; set; }


        public override void Validate( string value ) {
            base.Validate( value );
            int parsedValue;
            if( !Int32.TryParse( value, out parsedValue ) ) {
                throw new FormatException( "Value cannot be parsed as an integer." );
            }

            if( AlwaysAllowZero && parsedValue == 0 ) {
                return;
            }

            if( MinValue != int.MinValue && parsedValue < MinValue ) {
                throw new FormatException( String.Format( "Value is too low ({0}); expected at least {1}.", parsedValue, MinValue ) );
            }

            if( MaxValue != int.MaxValue && parsedValue > MaxValue ) {
                throw new FormatException( String.Format( "Value is too high ({0}); expected at most {1}.", parsedValue, MaxValue ) );
            }

            if( MultipleOf != 0 && (parsedValue % MultipleOf != 0) ) {
                throw new FormatException( String.Format( "Value ({0}) is not a multiple of {1}.", parsedValue, MultipleOf ) );
            }
            if( PowerOfTwo ) {
                bool found = false;
                for( int i = 0; i < 31; i++ ) {
                    if( parsedValue == (1 << i) ) {
                        found = true;
                        break;
                    }
                }
                if( !found && parsedValue != 0 ) {
                    throw new FormatException( String.Format( "Value ({0}) is not a power of two.", parsedValue ) );
                }
            }
            if( ValidValues != null ) {
                if( !ValidValues.Any( t => parsedValue == t ) ) {
                    throw new FormatException( String.Format( "Value ({0}) is not on the list of valid values.", parsedValue ) );
                }
            }
            if( InvalidValues != null ) {
                if( !InvalidValues.All( t => parsedValue != t ) ) {
                    throw new FormatException( String.Format( "Value ({0}) is on the list of invalid values.", parsedValue ) );
                }
            }
        }
    }


    internal sealed class RankKeyAttribute : ConfigKeyAttribute {
        public RankKeyAttribute( ConfigSection section, BlankValueMeaning blankMeaning, string description )
            : base( section, typeof( Rank ), "", description ) {
            CanBeLowest = true;
            CanBeHighest = true;
            BlankMeaning = blankMeaning;
            NotBlank = false;
        }
        public bool CanBeLowest { get; set; }
        public bool CanBeHighest { get; set; }
        public BlankValueMeaning BlankMeaning { get; set; }


        public override void Validate( string value ) {
            base.Validate( value );

            Rank rank;
            if( value.Length == 0 ) {
                rank = GetBlankValueSubstitute();
                if( rank == null ) return; // ranks must not have loaded yet; can't validate
            } else {
                rank = Rank.Parse( value );
                if( rank == null ) {
                    throw new FormatException( "Value cannot be parsed as a rank." );
                }
            }
            if( !CanBeLowest && rank == RankManager.LowestRank ) {
                throw new FormatException( "Value may not be the lowest rank." );
            }
            if( !CanBeHighest && rank == RankManager.HighestRank ) {
                throw new FormatException( "Value may not be the highest rank." );
            }
        }


        public override string Process( string value ) {
            if( value.Length == 0 ) {
                Rank defaultRank = GetBlankValueSubstitute();
                if( defaultRank == null ) {
                    return "";
                } else {
                    return defaultRank.FullName;
                }
            } else {
                return value;
            }
        }


        Rank GetBlankValueSubstitute() {
            switch( BlankMeaning ) {
                case BlankValueMeaning.DefaultRank:
                    return RankManager.DefaultRank;
                case BlankValueMeaning.HighestRank:
                    return RankManager.HighestRank;
                case BlankValueMeaning.LowestRank:
                    return RankManager.LowestRank;
                case BlankValueMeaning.Invalid:
                    throw new FormatException( "Value may not be blank." );
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public enum BlankValueMeaning {
            Invalid,
            LowestRank,
            DefaultRank,
            HighestRank
        }
    }


    internal sealed class BoolKeyAttribute : ConfigKeyAttribute {
        public BoolKeyAttribute( ConfigSection section, bool defaultValue, string description )
            : base( section, typeof( bool ), defaultValue, description ) {
        }


        public override void Validate( string value ) {
            base.Validate( value );
            bool test;
            if( !Boolean.TryParse( value, out test ) ) {
                throw new FormatException( "Value cannot be parsed as a boolean." );
            }
        }

        public override string Process( string value ) {
            if( value.Length == 0 ) {
                return DefaultValue.ToString();
            } else {
                return value;
            }
        }
    }


    internal sealed class IPKeyAttribute : ConfigKeyAttribute {
        public IPKeyAttribute( ConfigSection section, BlankValueMeaning defaultMeaning, string description )
            : base( section, typeof( IPAddress ), "", description ) {
            BlankMeaning = defaultMeaning;
            switch( BlankMeaning ) {
                case BlankValueMeaning.Any:
                    DefaultValue = IPAddress.Any;
                    break;
                case BlankValueMeaning.Loopback:
                    DefaultValue = IPAddress.Loopback;
                    break;
                default:
                    DefaultValue = IPAddress.None;
                    break;
            }
        }

        public bool NotAny { get; set; }
        public bool NotNone { get; set; }
        public bool NotLAN { get; set; }
        public bool NotLoopback { get; set; }
        public BlankValueMeaning BlankMeaning { get; set; }


        public override void Validate( string value ) {
            base.Validate( value );

            IPAddress test;
            if( value.Length == 0 ) {
                test = GetBlankValueSubstitute();
            } else if( !IPAddress.TryParse( value, out test ) ) {
                throw new FormatException( "Value cannot be parsed as an IP Address." );
            }
            if( NotAny && test.Equals( IPAddress.Any ) ) {
                throw new FormatException( String.Format( "Value cannot be {0}", IPAddress.Any ) );
            }
            if( NotNone && test.Equals( IPAddress.None ) ) {
                throw new FormatException( String.Format( "Value cannot be {0}", IPAddress.None ) );
            }
            if( NotLAN && test.IsLAN() ) {
                throw new FormatException( "Value cannot be a LAN address." );
            }
            if( NotLoopback && IPAddress.IsLoopback( test ) ) {
                throw new FormatException( "Value cannot be a loopback address." );
            }
        }


        IPAddress GetBlankValueSubstitute() {
            switch( BlankMeaning ) {
                case BlankValueMeaning.Any:
                    return IPAddress.Any;
                case BlankValueMeaning.Loopback:
                    return IPAddress.Loopback;
                case BlankValueMeaning.None:
                    return IPAddress.None;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        public override string Process( string value ) {
            if( value.Length == 0 ) {
                return GetBlankValueSubstitute().ToString();
            } else {
                return value;
            }
        }


        public enum BlankValueMeaning {
            Any,
            Loopback,
            None
        }
    }


    internal sealed class ColorKeyAttribute : ConfigKeyAttribute {
        public ColorKeyAttribute( ConfigSection section, string defaultColor, string description )
            : base( section, typeof( string ), Color.GetName( defaultColor ), description ) {
        }


        public override void Validate( string value ) {
            base.Validate( value );
            string parsedValue = Color.Parse( value );
            if( parsedValue == null ) {
                throw new FormatException( "Value cannot be parsed as a color." );
            } else if( parsedValue.Length == 0 && NotBlank ) {
                throw new FormatException( "Value may not represent absence of color." );
            }
        }
    }


    internal sealed class EnumKeyAttribute : ConfigKeyAttribute {
        public EnumKeyAttribute( ConfigSection section, object defaultValue, string description )
            : base( section, defaultValue.GetType(), defaultValue, description ) {
            ValueType = defaultValue.GetType();
        }


        public override void Validate( string value ) {
            base.Validate( value );
            if( !NotBlank && String.IsNullOrEmpty( value ) ) return;
            try {
                Enum.Parse( ValueType, value, true );
            } catch( ArgumentException ) {
                string message = String.Format( "Could not parse value as {0}. Valid values are: {1}",
                                                ValueType.Name,
                                                Enum.GetNames(ValueType).JoinToString() );
                throw new FormatException( message );
            }
        }

        public override string Process( string value ) {
            if( value.Length == 0 ) {
                return DefaultValue.ToString();
            } else {
                return value;
            }
        }
    }
}