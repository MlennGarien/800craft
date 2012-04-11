// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary> ConfigKey section/category. </summary>
    public enum ConfigSection {

        /// <summary> General server configuration (name, port, default rank, etc). </summary>
        General,

        /// <summary> Chat-related configuration (colors, whether to announce certain events, etc). </summary>
        Chat,

        /// <summary> World-related configuration (main world, default build rank, map folder, etc). </summary>
        Worlds,

        /// <summary> Security-related configuration (name verification, connection limit per IP, anti-spam, etc). </summary>
        Security,

        /// <summary> Saving- and backup-related configuration (save interval, backup intervals, etc). </summary>
        SavingAndBackup,

        /// <summary> Logging-related configuration (what events to log, how to store log files, etc). </summary>
        Logging,

        /// <summary> IRC-related configuration (network, channel list, bot nick, etc). </summary>
        IRC,

        /// <summary> Advanced configuration (performance adjustments, protocol tweaks, experimental features, etc). </summary>
        Advanced
    }
}