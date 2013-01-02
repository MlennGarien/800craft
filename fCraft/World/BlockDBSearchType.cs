// Copyright 2009-2013 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary> Describes what kind of results should BlockDB.Lookup return. </summary>
    public enum BlockDBSearchType {
        /// <summary> All BlockDB Entries (even those that have been overriden) are returned,
        /// possibly multiple entries per coordinate. </summary>
        ReturnAll,

        /// <summary> Only one newest entry is returned for each coordinate. </summary>
        ReturnNewest,

        /// <summary> Only one oldest entry is returned for each coordinate. </summary>
        ReturnOldest
    }
}