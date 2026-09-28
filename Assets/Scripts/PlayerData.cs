using System;
using Unity.Netcode;
using Unity.Collections;

public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable
{
    public ulong clientId;
    public int colorID;
    //NGO 关注的是“列表中的每一项能否网络序列化”，普通 string 是引用类型，长度可变、会产生额外分配；FixedString64Bytes 是固定大小的值类型，NGO 能高效、确定地序列化它。
    public FixedString64Bytes playerName;
    public FixedString64Bytes playerId;
    public bool Equals(PlayerData other)
    {
        return clientId == other.clientId && colorID == other.colorID && playerName == other.playerName && playerId == other.playerId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref colorID);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref playerId);
    }
}