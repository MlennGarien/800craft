// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>

namespace fCraft {
    /// <summary> Minecraft protocol's opcodes. </summary>
    public enum OpCode {
        Handshake = 0,
        Ping = 1,
        MapBegin = 2,
        MapChunk = 3,
        MapEnd = 4,
        SetBlockClient = 5,
        SetBlockServer = 6,
        AddEntity = 7,
        Teleport = 8,
        MoveRotate = 9,
        Move = 10,
        Rotate = 11,
        RemoveEntity = 12,
        Message = 13,
        Kick = 14,
        SetPermission = 15
    }
}