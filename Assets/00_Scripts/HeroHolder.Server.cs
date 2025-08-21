using System.Linq;
using Unity.Netcode;
using UnityEngine;

public partial class HeroHolder
{
    #region RPC
    [ServerRpc(RequireOwnership = false)]
    private void C2S_SpawnHero_ServerRpc(ulong clientId, HeroStatData data, string rarity)
    {
        Debug.Log($"[C->S]{nameof(C2S_SpawnHero_ServerRpc)}");

        SpawnHero(clientId, data, rarity);
    }

    [ServerRpc(RequireOwnership = false)]
    public void C2S_SellHero_ServerRpc(ulong clientid)
    {
        Debug.Log($"[C->S]{nameof(C2S_SellHero_ServerRpc)}");

        var target = Heros.Last();
        var netGo = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target.NetworkObjectId];
        BC_SellHero_ClientRpc(netGo.NetworkObjectId, clientid);
        netGo.Despawn(); // 이것도 위험해 보이는데...
    }

    [ServerRpc(RequireOwnership = false)]
    public void C2S_DestroyHeroHolder_ServerRpc(ulong clientId)
    {
        Debug.Log($"[C->S]{nameof(C2S_DestroyHeroHolder_ServerRpc)}");
        
        if(UtilManager.TryGetNetworkSpawnedObject(NetworkObjectId, out NetworkObject netObjHolder))
        {
            BC_DestroyHeroHolder_ClientRpc(clientId);
            netObjHolder.Despawn();
            return;
        }

        Debug.LogError("Failed to destroy hereHolder");
    }
    #endregion
}
