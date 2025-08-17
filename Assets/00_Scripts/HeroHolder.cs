using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class HeroHolder : NetworkBehaviour
{
    [SerializeField] private Hero _spawnHero;
    public string HolderName;
    public List<Hero> Heros = new();

    void Start()
    {
        var collider = gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(Spawner.xValue, Spawner.yValue);
    }

    public void SpawnHeroHolder(HeroStatData data)
    {
        if(Heros.Count == 0)
        {
            HolderName = data.heroName;
        }

        // 클라들이 중복된 요청 서버에게 보내는 것을 처리하기 위해 임시처리.
        // 서버에서 holder 생성 -> hero 생성을 한번에 처리 하도록 수정 필요.
        if(IsHost)
        {
            CS_SpawnHero_ServerRpc(NetworkManager.Singleton.LocalClientId, data);
        }
    }

    private void SpawnHero(ulong clientId, HeroStatData data)
    {
        if (!IsServer)
            return;

        var go = Instantiate(_spawnHero);        
        Heros.Add(go);
        

        NetworkObject netObj = go.GetComponent<NetworkObject>();
        netObj.Spawn();

        netObj.transform.parent = this.transform;

        ClientHeroSpawnClientRpc(netObj.NetworkObjectId, clientId, data);
    }

    #region RPC

    [ServerRpc(RequireOwnership = false)] 
    private void CS_SpawnHero_ServerRpc(ulong clientId, HeroStatData data)
    {
        SpawnHero(clientId, data);
    }

    [ClientRpc]
    private void ClientHeroSpawnClientRpc(ulong netObjId, ulong clientid, HeroStatData data)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjId, out NetworkObject heroNetObj))
        {
            heroNetObj.transform.localPosition = Vector3.zero;
            heroNetObj.GetComponent<Hero>().Initdata(data);
        }
    }

    #endregion
}
