using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class HeroHolder : NetworkBehaviour
{
    [SerializeField] private Hero _spawnHero;
    [SerializeField] private Transform circleRange;

    public string HolderName;
    public List<Hero> Heros = new();
    public HeroStatData heroData;

    void Start()
    {
        var collider = gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(Spawner.xValue, Spawner.yValue);
    }

    public void ShowRange()
    {
        circleRange.localScale = new Vector3(heroData.heroRange * 2, heroData.heroRange * 2, 1);
        //circleRange.gameObject.SetActive(true);
    }

    public void HideRange()
    {
        circleRange.localScale = Vector3.zero;
        //circleRange.gameObject.SetActive(false);
    }

    public void SpawnHeroHolder(HeroStatData data)
    {
        if(Heros.Count == 0)
        {
            HolderName = data.heroName;
        }

        // Ŭ����� �ߺ��� ��û �������� ������ ���� ó���ϱ� ���� �ӽ�ó��.
        // �������� holder ���� -> hero ������ �ѹ��� ó�� �ϵ��� ���� �ʿ�.
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
            heroData = data;

            var parent = heroNetObj.transform.parent;
            int siblingCount = parent.childCount;

            // range ������Ʈ ������ �⺻���� �ڽ� ������ 1
            if (siblingCount == 2)
            {
                parent.GetChild(1).transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
                parent.GetChild(1).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 3;
            } else if(siblingCount == 3)
            {
                parent.GetChild(1).transform.localPosition = new Vector3(-0.1f, 0.0f, 0.0f);
                parent.GetChild(1).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 3;
                parent.GetChild(2).transform.localPosition = new Vector3(0.1f, 0.0f, 0.0f);
                parent.GetChild(2).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 4;
            } else if(siblingCount == 4)
            {
                parent.GetChild(1).transform.localPosition = new Vector3(-0.1f, 0.05f, 0.0f);
                parent.GetChild(1).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 3;
                parent.GetChild(2).transform.localPosition = new Vector3(0.1f, 0.05f, 0.0f);
                parent.GetChild(2).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 4;
                parent.GetChild(3).transform.localPosition = new Vector3(0.0f, -0.05f, 0.0f);
                parent.GetChild(3).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 5;
            } else
            {
                // error
            }

            heroNetObj.GetComponent<Hero>().Initdata(data, this);
        }
    }

    #endregion
}
