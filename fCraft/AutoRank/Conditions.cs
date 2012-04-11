// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;

namespace fCraft.AutoRank {

    /// <summary> Base class for all AutoRank conditions. </summary>
    public abstract class Condition {
        public abstract bool Eval( PlayerInfo info );

        public static Condition Parse( XElement el ) {
            switch( el.Name.ToString() ) {
                case "AND":
                    return new ConditionAND( el );
                case "OR":
                    return new ConditionOR( el );
                case "NOR":
                    return new ConditionNOR( el );
                case "NAND":
                    return new ConditionNAND( el );
                case "ConditionIntRange":
                    return new ConditionIntRange( el );
                case "ConditionRankChangeType":
                    return new ConditionRankChangeType( el );
                case "ConditionPreviousRank":
                    return new ConditionPreviousRank( el );
                default:
                    throw new FormatException();
            }
        }

        public abstract XElement Serialize();
    }


    /// <summary> Class for checking ranges of countable PlayerInfo fields (see ConditionField enum). </summary>
    public sealed class ConditionIntRange : Condition {
        public ConditionField Field { get; set; }
        public ComparisonOp Comparison { get; set; }
        public int Value { get; set; }

        public ConditionIntRange() {
            Comparison = ComparisonOp.Eq;
        }

        public ConditionIntRange( [NotNull] XElement el )
            : this() {
            // ReSharper disable PossibleNullReferenceException
            if( el == null ) throw new ArgumentNullException( "el" );
            Field = (ConditionField)Enum.Parse( typeof( ConditionField ), el.Attribute( "field" ).Value, true );
            Value = Int32.Parse( el.Attribute( "val" ).Value );
            if( el.Attribute( "op" ) != null ) {
                Comparison = (ComparisonOp)Enum.Parse( typeof( ComparisonOp ), el.Attribute( "op" ).Value, true );
            }
            // ReSharper restore PossibleNullReferenceException
        }


        public ConditionIntRange( ConditionField field, ComparisonOp comparison, int value ) {
            Field = field;
            Comparison = comparison;
            Value = value;
        }

        public override bool Eval( [NotNull] PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            long givenValue;
            switch( Field ) {
                case ConditionField.TimeSinceFirstLogin:
                    givenValue = (int)info.TimeSinceFirstLogin.TotalSeconds;
                    break;
                case ConditionField.TimeSinceLastLogin:
                    givenValue = (int)info.TimeSinceLastLogin.TotalSeconds;
                    break;
                case ConditionField.LastSeen:
                    givenValue = (int)info.TimeSinceLastSeen.TotalSeconds;
                    break;
                case ConditionField.BlocksBuilt:
                    givenValue = info.BlocksBuilt;
                    break;
                case ConditionField.BlocksDeleted:
                    givenValue = info.BlocksDeleted;
                    break;
                case ConditionField.BlocksChanged:
                    givenValue = info.BlocksBuilt + info.BlocksDeleted;
                    break;
                case ConditionField.BlocksDrawn:
                    givenValue = info.BlocksDrawn;
                    break;
                case ConditionField.TimesVisited:
                    givenValue = info.TimesVisited;
                    break;
                case ConditionField.MessagesWritten:
                    givenValue = info.MessagesWritten;
                    break;
                case ConditionField.TimesKicked:
                    givenValue = info.TimesKicked;
                    break;
                case ConditionField.TotalTime:
                    givenValue = (int)info.TotalTime.TotalSeconds;
                    break;
                case ConditionField.TimeSinceRankChange:
                    givenValue = (int)info.TimeSinceRankChange.TotalSeconds;
                    break;
                case ConditionField.TimeSinceLastKick:
                    givenValue = (int)info.TimeSinceLastKick.TotalSeconds;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch( Comparison ) {
                case ComparisonOp.Lt:
                    return (givenValue < Value);
                case ComparisonOp.Lte:
                    return (givenValue <= Value);
                case ComparisonOp.Gte:
                    return (givenValue >= Value);
                case ComparisonOp.Gt:
                    return (givenValue > Value);
                case ComparisonOp.Eq:
                    return (givenValue == Value);
                case ComparisonOp.Neq:
                    return (givenValue != Value);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionIntRange" );
            el.Add( new XAttribute( "field", Field ) );
            el.Add( new XAttribute( "val", Value ) );
            el.Add( new XAttribute( "op", Comparison ) );
            return el;
        }

        public override string ToString() {
            return String.Format( "ConditionIntRange( {0} {1} {2} )",
                                  Field,
                                  Comparison,
                                  Value );
        }
    }


    /// <summary> Checks what caused player's last rank change (see RankChangeType enum). </summary>
    public sealed class ConditionRankChangeType : Condition {
        public RankChangeType Type { get; set; }

        public ConditionRankChangeType( RankChangeType type ) {
            Type = type;
        }

        public ConditionRankChangeType( [NotNull] XElement el ) {
            if( el == null ) throw new ArgumentNullException( "el" );
            // ReSharper disable PossibleNullReferenceException
            Type = (RankChangeType)Enum.Parse( typeof( RankChangeType ), el.Attribute( "val" ).Value, true );
            // ReSharper restore PossibleNullReferenceException
        }

        public override bool Eval( [NotNull] PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            return (info.RankChangeType == Type);
        }

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionRankChangeType" );
            el.Add( new XAttribute( "val", Type.ToString() ) );
            return el;
        }
    }


    /// <summary> Checks what rank the player held previously. </summary>
    public sealed class ConditionPreviousRank : Condition {
        public Rank Rank { get; set; }
        public ComparisonOp Comparison { get; set; }

        public ConditionPreviousRank( [NotNull] Rank rank, ComparisonOp comparison ) {
            if( rank == null ) throw new ArgumentNullException( "rank" );
            if( !Enum.IsDefined( typeof( ComparisonOp ), comparison ) ) {
                throw new ArgumentOutOfRangeException( "comparison", "Unknown comparison type" );
            }
            Rank = rank;
            Comparison = comparison;
        }

        public ConditionPreviousRank( [NotNull] XElement el ) {
            // ReSharper disable PossibleNullReferenceException
            if( el == null ) throw new ArgumentNullException( "el" );
            Rank = Rank.Parse( el.Attribute( "val" ).Value );
            Comparison = (ComparisonOp)Enum.Parse( typeof( ComparisonOp ), el.Attribute( "op" ).Value, true );
            // ReSharper restore PossibleNullReferenceException
        }

        public override bool Eval( [NotNull] PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            Rank prevRank = info.PreviousRank ?? info.Rank;
            switch( Comparison ) {
                case ComparisonOp.Lt:
                    return (prevRank < Rank);
                case ComparisonOp.Lte:
                    return (prevRank <= Rank);
                case ComparisonOp.Gte:
                    return (prevRank >= Rank);
                case ComparisonOp.Gt:
                    return (prevRank > Rank);
                case ComparisonOp.Eq:
                    return (prevRank == Rank);
                case ComparisonOp.Neq:
                    return (prevRank != Rank);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override XElement Serialize() {
            XElement el = new XElement( "ConditionPreviousRank" );
            el.Add( new XAttribute( "val", Rank.FullName ) );
            el.Add( new XAttribute( "op", Comparison.ToString() ) );
            return el;
        }
    }


    #region Condition Sets

    /// <summary> Base class for condition sets/combinations. </summary>
    public class ConditionSet : Condition {
        protected ConditionSet() {
            Conditions = new List<Condition>();
        }

        public List<Condition> Conditions { get; private set; }

        protected ConditionSet( [NotNull] IEnumerable<Condition> conditions ) {
            if( conditions == null ) throw new ArgumentNullException( "conditions" );
            Conditions = conditions.ToList();
        }

        protected ConditionSet( [NotNull] XContainer el )
            : this() {
            if( el == null ) throw new ArgumentNullException( "el" );
            foreach( XElement cel in el.Elements() ) {
                Add( Parse( cel ) );
            }
        }

        public override bool Eval( PlayerInfo info ) {
            throw new NotImplementedException();
        }

        public void Add( [NotNull] Condition condition ) {
            if( condition == null ) throw new ArgumentNullException( "condition" );
            Conditions.Add( condition );
        }

        public override XElement Serialize() {
            throw new NotImplementedException();
        }
    }


    /// <summary> Logical AND - true if ALL conditions are true. </summary>
    public sealed class ConditionAND : ConditionSet {
        public ConditionAND() { }
        public ConditionAND( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionAND( XContainer el ) : base( el ) { }

        public override bool Eval( PlayerInfo info ) {
            return Conditions == null || Conditions.All( t => t.Eval( info ) );
        }


        public override XElement Serialize() {
            XElement el = new XElement( "AND" );
            foreach( Condition cond in Conditions ) {
                el.Add( cond.Serialize() );
            }
            return el;
        }
    }


    /// <summary> Logical AND - true if NOT ALL of the conditions are true. </summary>
    public sealed class ConditionNAND : ConditionSet {
        public ConditionNAND() { }
        public ConditionNAND( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionNAND( XContainer el ) : base( el ) { }

        public override bool Eval( [NotNull] PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            return Conditions == null || Conditions.Any( t => !t.Eval( info ) );
        }


        public override XElement Serialize() {
            XElement el = new XElement( "NAND" );
            foreach( Condition cond in Conditions ) {
                el.Add( cond.Serialize() );
            }
            return el;
        }
    }


    /// <summary> Logical AND - true if ANY of the conditions are true. </summary>
    public sealed class ConditionOR : ConditionSet {
        public ConditionOR() { }
        public ConditionOR( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionOR( XContainer el ) : base( el ) { }

        public override bool Eval( [NotNull] PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            return Conditions == null || Conditions.Any( t => t.Eval( info ) );
        }


        public override XElement Serialize() {
            XElement el = new XElement( "OR" );
            foreach( Condition cond in Conditions ) {
                el.Add( cond.Serialize() );
            }
            return el;
        }
    }


    /// <summary> Logical AND - true if NONE of the conditions are true. </summary>
    public sealed class ConditionNOR : ConditionSet {
        public ConditionNOR() { }
        public ConditionNOR( IEnumerable<Condition> conditions ) : base( conditions ) { }
        public ConditionNOR( XContainer el ) : base( el ) { }

        public override bool Eval( [NotNull] PlayerInfo info ) {
            if( info == null ) throw new ArgumentNullException( "info" );
            return Conditions == null || Conditions.All( t => !t.Eval( info ) );
        }


        public override XElement Serialize() {
            XElement el = new XElement( "NOR" );
            foreach( Condition cond in Conditions ) {
                el.Add( cond.Serialize() );
            }
            return el;
        }
    }

    #endregion
}