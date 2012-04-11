// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary> Enumerates the recognized command-line switches/arguments.
    /// Args are parsed in Server.InitLibrary </summary>
    public enum ArgKey {
        /// <summary> Working path (directory) that fCraft should use.
        /// If the path is relative, it's computed against the location of fCraft.dll </summary>
        Path,

        /// <summary> Path (directory) where the log files should be placed.
        /// If the path is relative, it's computed against the working path. </summary>
        LogPath,

        /// <summary> Path (directory) where the map files should be loaded from/saved to.
        /// If the path is relative, it's computed against the working path. </summary>
        MapPath,

        /// <summary> Path (file) of the configuration file.
        /// If the path is relative, it's computed against the working path. </summary>
        Config,

        /// <summary> If NoRestart flag is present, fCraft will shutdown instead of restarting.
        /// Useful if you are using an auto-restart script/process monitor of some sort. </summary>
        NoRestart,

        /// <summary> If ExitOnCrash flag is present, fCraft frontends will exit
        /// at once in the event of an unrecoverable crash, instead of showing a message. </summary>
        ExitOnCrash,

        /// <summary> Disables all logging. </summary>
        NoLog,

        /// <summary> Disables colors in CLI frontends. </summary>
        NoConsoleColor
    };
}
