// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using JetBrains.Annotations;

// ReSharper disable UnusedMemberInSuper.Global
namespace fCraft.Drawing {

    /// <summary> Class that desribes a type of brush in general, and allows creating new brushes with /Brush.
    /// One instance of IBrushFactory for each type of brush is kept by the BrushManager. </summary>
    public interface IBrushFactory {
        [NotNull]
        string Name { get; }

        [NotNull]
        string Help { get; }

        /// <summary> List of aliases/alternate names for this brush. May be null. </summary>
        [CanBeNull]
        string[] Aliases { get; }

        /// <summary> Creates a new brush for a player, based on given parameters. </summary>
        /// <param name="player"> Player who will be using this brush.
        /// Errors and warnings about the brush creation should be communicated by messaging the player. </param>
        /// <param name="cmd"> Parameters passed to the /Brush command (after the brush name). </param>
        /// <returns> A newly-made brush, or null if there was some problem with parameters/permissions. </returns>
        IBrush MakeBrush( [NotNull] Player player, [NotNull] Command cmd );
    }


    /// <summary> Class that describes a configured brush, and allows creating instances for specific DrawOperations.
    /// Configuration-free brush types may combine IBrushFactory and IBrushType into one class. </summary>
    public interface IBrush {
        /// <summary> IBrushFactory associated with this brush type. </summary>
        [NotNull]
        IBrushFactory Factory { get; }

        /// <summary> A compact readable summary of brush type and configuration. </summary>
        [NotNull]
        string Description { get; }

        /// <summary> Creates an instance for this configured brush, for use with a specific DrawOperation. </summary>
        /// <param name="player"> Player who will be using this brush.
        /// Errors and warnings about the brush creation should be communicated by messaging the player. </param>
        /// <param name="cmd"> Parameters passed to the DrawOperation.
        /// If any are given, these parameters should generally replace any stored configuration. </param>
        /// <param name="op"> DrawOperation that will be using this brush. </param>
        /// <returns> A newly-made brush, or null if there was some problem with parameters/permissions. </returns>
        IBrushInstance MakeInstance( [NotNull] Player player, [NotNull] Command cmd, [NotNull] DrawOperation op );
    }


    /// <summary> Class that describes an individual instance of a configured brush.
    /// Each brush instance will only be used for one DrawOperation, so it can store state.
    /// Stateless brush types may combine IBrush and IBrushInstance into one class. </summary>
    public interface IBrushInstance {
        /// <summary> Configured brush that created this instance. </summary>
        [NotNull]
        IBrush Brush { get; }

        /// <summary> A compact readable summary of brush type, configuration, and state. </summary>
        [NotNull]
        string InstanceDescription { get; }

        /// <summary> Whether the brush is capable of providing alternate blocks (e.g. for filling hollow DrawOps).</summary>
        bool HasAlternateBlock { get; }

        /// <summary> Called when the DrawOperation starts. Should be used to verify that the brush is ready for use.
        /// Resources used by the brush should be obtained here. </summary>
        /// <param name="player"> Player who started the DrawOperation. </param>
        /// <param name="op"> DrawOperation that will be using this brush. </param>
        /// <returns> Whether this brush instance has successfully began or not. </returns>
        bool Begin( [NotNull] Player player, [NotNull] DrawOperation op );

        /// <summary> Provides the next Block type for the given DrawOperation. </summary>
        /// <returns> Block type to place, or Block.Undefined to skip. </returns>
        Block NextBlock( [NotNull] DrawOperation op );

        /// <summary> Called when the DrawOperation is done or cancelled.
        /// Resources used by the brush should be freed/disposed here. </summary>
        void End();
    }
}