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
}
