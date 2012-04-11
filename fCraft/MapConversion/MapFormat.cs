// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>

namespace fCraft.MapConversion {
    /// <summary> An enumeration of map formats supported by fCraft. </summary>
    public enum MapFormat {
        /// <summary> Unidentified map. </summary>
        Unknown,

        /// <summary> Current map format used by fCraft. </summary>
        FCMv3,

        /// <summary> Current map format used by fCraft. </summary>
        FCMv2,

        /// <summary> Map format used by MCSharp and its forks (MCZall/MCLawl). Initial support added by Tyler/TkTech. </summary>
        MCSharp,

        /// <summary> Map format used by MinerCPP and LuaCraft. Initial support added by Tyler/TkTech. </summary>
        MinerCPP,

        /// <summary> Map format used by Myne and its derivatives (MyneCraft/iCraft). </summary>
        Myne,

        /// <summary> Map format used by Mojang's classic and survivaltest. </summary>
        Creative,

        /// <summary> Map format used by Mojang's indev. </summary>
        NBT,

        /// <summary> Map format used by JTE's server. </summary>
        JTE,

        /// <summary> Map foramt used by D3 server. </summary>
        D3,

        /// <summary> Format used by Opticraft v0.2+. Support contributed by Jared/LgZ-optical. </summary>
        Opticraft,

        /// <summary> Universal map format, planned for future use by fCraft. Currently unsupported. </summary>
        FCMv4
    }


    /// <summary> Type of map storage (file or folder-based). </summary>
    public enum MapStorageType {
        SingleFile,
        Directory
    }
}
