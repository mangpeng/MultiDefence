using IGN.Common.Actions;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public partial class HeroHolder
{
    public void SpawnHero(ulong clientId, HeroStatData data, string rarity)
    {
        if (!IsServer)
            return;

        if(Heros.Count >= 3)
        {
            Debug.LogError("Not enough space to spawn new here");
            return;
        }

        var go = Instantiate(_spawnHero);
        Heros.Add(go);

        NetworkObject netObj = go.GetComponent<NetworkObject>();
        netObj.Spawn();
        if(!netObj.TrySetParent(this.transform, worldPositionStays: false))
        {
            Debug.LogError("Failed to set hero's transform parent");
            return;
        }

        BC_ClientHeroSpawn_ClientRpc(netObj.NetworkObjectId, clientId, data, rarity);
    }

    #region RPC
    [ServerRpc(RequireOwnership = false)]
    private void C2S_SpawnHero_ServerRpc(ulong clientId, HeroStatData data, string rarity)
    {
        Debug.Log($"[C->S]{nameof(C2S_SpawnHero_ServerRpc)}");

        SpawnHero(clientId, data, rarity);
    }

    [ServerRpc(RequireOwnership = false)]
    public void C2S_SellHero_ServerRpc(ulong clientid, ActionContext ctx)
    {
        Debug.Log($"[C->S]{nameof(C2S_SellHero_ServerRpc)}");

        var target = Heros.Last();
        var netGo = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target.NetworkObjectId];
        BC_SellHero_ClientRpc(netGo.NetworkObjectId, clientid, ctx);
        netGo.Despawn(); // 이것도 위험해 보이는데...
    }

    #endregion
}
