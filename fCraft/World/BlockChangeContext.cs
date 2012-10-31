// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;

namespace fCraft {
    /// <summary> Context of the block change. Multiple flags can be combined. </summary>
    [Flags]
    public enum BlockChangeContext { // Backed by Int32.
        /// <summary> Default/unknown context. </summary>
        Unknown = 0,

        /// <summary> Block was manually edited, with a click. Opposite of Drawn. </summary>
        Manual = 1,

        /// <summary> Block was edited using a drawing operation. Opposite of Manual. </summary>
        Drawn = 2,

        /// <summary> Block was replaced (using /paint, /r, /rn, /rb, or replace brush variations). </summary>
        Replaced = 4,

        /// <summary> Block was pasted (using /paste, /pastenot, /px, or /pnx) </summary>
        Pasted = 8,

        /// <summary> Block was cut (using /cut). </summary>
        Cut = 16,

        /// <summary> Undone a change previously made by same player (using /undo, /ua, or /up on self). </summary>
        UndoneSelf = 32,

        /// <summary> Undone a change previously made by another player (using /ua or /up). </summary>
        UndoneOther = 64,

        /// <summary> Block was inserted from another file (using /restore). </summary>
        Restored = 128,

        /// <summary> Block was filled (using /fill2d or /fill3d). </summary>
        Filled = 256,

        /// <summary> Redone, opposite of UndoneSelf/UndoneOther </summary>
        Redone = 512,

        /// <summary> Redone, opposite of UndoneSelf/UndoneOther </summary>
        Portal = 1024,

        /// <summary> Redone, opposite of UndoneSelf/UndoneOther </summary>
        Physics = 2048,


        /// <summary> Combination of Manual and Replaced (as used by /Paint). </summary>
        PaintedCombo = Manual | Replaced,

        /// <summary> Combination of Drawn, UndoneSelf, and Redone (as used by /Redo). </summary>
        RedoneCombo = Drawn | UndoneSelf | Redone
    }
}