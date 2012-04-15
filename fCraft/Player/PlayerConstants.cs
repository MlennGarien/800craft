// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;

// This file condenses some of the player-related enumerations
namespace fCraft {
    /// <summary> List of possible reasons for players leaving the server. </summary>
    public enum LeaveReason : byte {
        /// <summary> Unknown leave reason (default) </summary>
        Unknown = 0x00,

        /// <summary> Client exited normally </summary>
        ClientQuit = 0x01,

        /// <summary> Client reconnected before old session timed out, or connected from another IP. </summary>
        ClientReconnect = 0x03,

        /// <summary> Manual or miscellaneous kick </summary>
        Kick = 0x10,

        /// <summary> Kicked for being idle/AFK. </summary>
        IdleKick = 0x11,

        /// <summary> Invalid characters in a message </summary>
        InvalidMessageKick = 0x12,

        /// <summary> Attempted to place invalid blocktype </summary>
        [Obsolete]
        InvalidSetTileKick = 0x13,

        /// <summary> Unknown opcode or packet </summary>
        InvalidOpcodeKick = 0x14,

        /// <summary> Triggered antigrief / block spam </summary>
        BlockSpamKick = 0x15,

        /// <summary> Kicked for message spam (after warnings) </summary>
        MessageSpamKick = 0x16,

        /// <summary> Banned directly by name </summary>
        Ban = 0x20,

        /// <summary> Banned indirectly by /BanIP </summary>
        BanIP = 0x21,

        /// <summary> Banned indirectly by /BanAll </summary>
        BanAll = 0x22,


        /// <summary> Server-side error (uncaught exception in session's thread) </summary>
        ServerError = 0x30,

        /// <summary> Server is shutting down </summary>
        ServerShutdown = 0x31,

        /// <summary> Server was full or became full </summary>
        ServerFull = 0x32,

        /// <summary> World was full (forced join failed) </summary>
        WorldFull = 0x33,


        /// <summary> Login failed due to protocol violation/mismatch (e.g. SMP client) </summary>
        ProtocolViolation = 0x41,

        /// <summary> Login failed due to unverified player name </summary>
        UnverifiedName = 0x42,

        /// <summary> Login denied for some other reason </summary>
        LoginFailed = 0x43,

        /// <summary> When a player ragequits from the server </summary>
        RageQuit = 0x44,
    }


    /// <summary> Mode of player name verification. </summary>
    public enum NameVerificationMode {
        /// <summary> Player names are not checked.
        /// Any connecting player can assume any identity. </summary>
        Never,

        /// <summary> Security balanced with usability.
        /// If normal verification fails, an additional check is done:
        /// If player has previously verified for his current IP and has connected at least twice before, he is allowed in. </summary>
        Balanced,

        /// <summary> Strict verification checks.
        /// If name cannot be verified, player is kicked and a failed login attempt is logged.
        /// Note that players connecting from localhost (127.0.0.1) are always allowed. </summary>
        Always
    }


    /// <summary> Describes the way player's rank was set. </summary>
    public enum RankChangeType : byte {
        /// <summary> Default rank (never been promoted or demoted). </summary>
        Default = 0,

        /// <summary> Promoted manually by another player or by console. </summary>
        Promoted = 1,

        /// <summary> Demoted manually by another player or by console. </summary>
        Demoted = 2,

        /// <summary> Promoted automatically (e.g. by AutoRank). </summary>
        AutoPromoted = 3,

        /// <summary> Demoted automatically (e.g. by AutoRank). </summary>
        AutoDemoted = 4
    }


    /// <summary> Bandwidth use mode.
    /// This setting affects the way player receive movement updates. </summary>
    public enum BandwidthUseMode : byte {
        /// <summary> Use server default. </summary>
        Default = 0,

        /// <summary> Very low bandwidth (choppy player movement, players pop-in/pop-out in the distance). </summary>
        VeryLow = 1,

        /// <summary> Lower bandwidth use (less choppy, pop-in distance is further). </summary>
        Low = 2,

        /// <summary> Normal mode (pretty much no choppiness, pop-in only noticeable when teleporting). </summary>
        Normal = 3,

        /// <summary> High bandwidth use (pretty much no choppiness, pop-in only noticeable when teleporting on large maps). </summary>
        High = 4,

        /// <summary> Very high bandwidth use (no choppiness at all, no pop-in). </summary>
        VeryHigh = 5
    }


    /// <summary> A list of possible results of Player.CanPlace() permission test. </summary>
    public enum CanPlaceResult {

        /// <summary> Block may be placed/changed. </summary>
        Allowed,

        /// <summary> Block was out of bounds in the given map. </summary>
        OutOfBounds,

        /// <summary> Player was not allowed to place or replace blocks of this particular blocktype. </summary>
        BlocktypeDenied,

        /// <summary> Player was not allowed to build on this particular world. </summary>
        WorldDenied,

        /// <summary> Player was not allowed to build in this particular zone.
        /// Use World.Map.FindDeniedZone() to find the specific zone. </summary>
        ZoneDenied,

        /// <summary> Player's rank is not allowed to build or delete in general. </summary>
        RankDenied,

        /// <summary> A plugin callback cancelled block placement/deletion.
        /// To keep player's copy of the map in sync, he will be resent the old blocktype at that location. </summary>
        PluginDenied,

        /// <summary> A plugin callback cancelled block placement/deletion.
        /// A copy of the old block will not be sent to the player (he may go out of sync). </summary>
        PluginDeniedNoUpdate,
        
        Revert
    }


    public enum WorldChangeReason {
        /// <summary> First world that the player joins upon entering the server (main). </summary>
        FirstWorld,

        /// <summary> Rejoining the same world (e.g. after /wflush or /wload). </summary>
        Rejoin,

        /// <summary> Manually by typing /join. </summary>
        ManualJoin,

        /// <summary> Teleporting to a player on another map. </summary>
        Tp,

        /// <summary> Bring brought by a player on another map. Also used by /bringall, /wbring, and /setspawn. </summary>
        Bring,

        /// <summary> Following the /spectate target to another world. </summary>
        SpectateTargetJoined,

        /// <summary> Previous world was removed with /wunload. </summary>
        WorldRemoved,

        /// <summary> Previous world's access permissions changed, and player was forced to main. </summary>
        PermissionChanged,

        /// <summary> Player entered a portal. </summary>
        Portal
    }


    public enum BanStatus : byte {
        /// <summary> Player is not banned. </summary>
        NotBanned,

        /// <summary> Player cannot be banned or IP-banned. </summary>
        IPBanExempt,

        /// <summary> Player is banned. </summary>
        Banned
    }


    public enum ClickAction : byte {
        /// <summary> Deleting a block (left-click in Minecraft). </summary>
        Delete = 0,

        /// <summary> Building a block (right-click in Minecraft). </summary>
        Build = 1
    }


    public enum SessionState {
        /// <summary> There is no session associated with this player (e.g. Console). </summary>
        Offline,

        /// <summary> Player is in the middle of the login sequence. </summary>
        Connecting,

        /// <summary> Player has logged in, and is loading the first world. </summary>
        LoadingMain,

        /// <summary> Player is fully connected and online. </summary>
        Online,

        /// <summary> Player was kicked, and is about to be disconnected. </summary>
        PendingDisconnect,

        /// <summary> Session has ended - player disconnected. </summary>
        Disconnected
    }
}