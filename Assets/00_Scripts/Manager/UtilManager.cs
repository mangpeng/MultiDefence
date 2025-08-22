using System;
using Unity.Netcode;
using UnityEngine;

public class UtilManager
{
    public static ulong LocalID => NetworkManager.Singleton.LocalClientId;

    public static void HostAndClientMethod(Action client, Action server)
    {
        if(NetworkManager.Singleton.IsServer)
        {
            server?.Invoke();
        } else
        {
            client?.Invoke();
        }
    }

    public static bool TryGetNetworkSpawnedObject(ulong id, out NetworkObject obj)
    {
        return NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out obj);
    }

    public static bool IsClientCheck(ulong clientid)
    {
        return clientid == LocalID;
    }

    public static Color GetColorByRarity(string r)
    {
        if (r == "Common") return GetColorByRarity(Rarity.Common);
        else if(r == "Uncommon") return GetColorByRarity(Rarity.Uncommon);
        else return GetColorByRarity(Rarity.Rare);
    }

    public static Color GetColorByRarity(Rarity r)
    {
        switch (r)
        {
            case Rarity.Common: return Color.gray;
            case Rarity.Uncommon: return Color.white;
            case Rarity.Rare: return Color.blue;
            case Rarity.Hero: return Color.black;
            case Rarity.Lengendary: return Color.black;
            default: return Color.black;
        }
    }
}
