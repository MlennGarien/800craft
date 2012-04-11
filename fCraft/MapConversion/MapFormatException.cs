// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using JetBrains.Annotations;

namespace fCraft.MapConversion {
    /// <summary> Exception caused by problems with the map file's incorrect format or structure. </summary>
    public sealed class MapFormatException : Exception {
        public MapFormatException() { }
        public MapFormatException( [NotNull] string message ) : base( message ) { }
    }
}
