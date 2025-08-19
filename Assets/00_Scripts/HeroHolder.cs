using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.ShaderData;
using static UnityEngine.GraphicsBuffer;

public class HeroHolder : NetworkBehaviour
{
    [SerializeField] private Hero _spawnHero;
    [SerializeField] private Transform circleRange;
    [SerializeField] private Transform square;
    [SerializeField] private Transform circle;

    public string HolderName;
    public ulong clientId;
    public List<Hero> Heros = new();
    public HeroStatData heroData;
    public Vector2 pos;
    public int idx;

    public UnityEngine.UI.Button btnSell;
    public UnityEngine.UI.Button btnCompose;

    void Start()
    {
        var collider = gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(Spawner.xValue, Spawner.yValue);

        square.transform.localScale = collider.size;

        btnSell.onClick.AddListener(() => CS_Sell_ServerRpc());
        // btnCompose.onClick.AddListener(() => CS_Sell_ServerRpc());

    }

    [ServerRpc(RequireOwnership = false)]
    public void CS_Sell_ServerRpc()
    {
        var target = Heros.Last();
        var netGo = NetworkManager.Singleton.SpawnManager.SpawnedObjects[target.NetworkObjectId];
        BC_Sell_ClientRpc(netGo.NetworkObjectId);
        netGo.Despawn(); // 이것도 위험해 보이는데...
    }

    [ClientRpc]
    public void BC_Sell_ClientRpc(ulong netObjId)
    {
        var netGo = NetworkManager.Singleton.SpawnManager.SpawnedObjects[netObjId]; // 이것도 위험해 보이는데...
        Heros.Remove(netGo.GetComponent<Hero>());
    }

    public void Composition()
    {

    }

    public void HeroChange(HeroHolder holder)
    {
        List<Vector2> poss = new();

        switch (Heros.Count)
        {
            case 1:
                {
                    poss = new List<Vector2>();
                    poss.Add(new Vector2(0.0f, 0.0f));
                    break;
                }
            case 2:
                {
                    poss = new List<Vector2>();
                    poss.Add(new Vector2(-0.1f, 0.0f));
                    poss.Add(new Vector2(0.1f, 0.0f));
                    break;
                }
            case 3:
                {
                    poss = new List<Vector2>();
                    poss.Add(new Vector2(-0.1f, 0.05f));
                    poss.Add(new Vector2(0.1f, 0.05f));
                    poss.Add(new Vector2(0.0f, -0.05f));
                    break;
                }
        }

        for (int i = 0; i < poss.Count; i++)
        {
            var worldPos = holder.transform.TransformPoint(poss[i]);
            poss[i] = worldPos;
        }

        for (int i = 0; i < Heros.Count; i++)
        {
            Heros[i].ChangePosition(holder, poss, i);
        }
    }

    public void ShowSquare(bool isShow) => square.gameObject.SetActive(isShow);
    public void ShowCircle(bool isShow) => circle.gameObject.SetActive(isShow);
    

    public void ShowRange()
    {
        circleRange.localScale = new Vector3(heroData.heroRange * 2, heroData.heroRange * 2, 1);
        circleRange.gameObject.SetActive(true);
    }

    public void HideRange()
    {
        // circleRange.localScale = Vector3.zero;
        circleRange.gameObject.SetActive(false);
    }

    public void SpawnHeroHolder(HeroStatData data, ulong clientid)
    {
        if(Heros.Count == 0)
        {
            HolderName = data.heroName;
            this.clientId = clientid;
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
            heroData = data;

            

            var parent = heroNetObj.transform.parent;
            int siblingCount = parent.childCount;

            // 기본으로 자식 개수는 3
            if (siblingCount == 4)
            {
                parent.GetChild(3).transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
                parent.GetChild(3).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 3;
            } else if(siblingCount == 5)
            {
                parent.GetChild(3).transform.localPosition = new Vector3(-0.1f, 0.0f, 0.0f);
                parent.GetChild(3).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 3;
                parent.GetChild(4).transform.localPosition = new Vector3(0.1f, 0.0f, 0.0f);
                parent.GetChild(4).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 4;
            } else if(siblingCount == 6)
            {
                parent.GetChild(3).transform.localPosition = new Vector3(-0.1f, 0.05f, 0.0f);
                parent.GetChild(3).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 3;
                parent.GetChild(4).transform.localPosition = new Vector3(0.1f, 0.05f, 0.0f);
                parent.GetChild(4).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 4;
                parent.GetChild(5).transform.localPosition = new Vector3(0.0f, -0.05f, 0.0f);
                parent.GetChild(5).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 5;
            } else
            {
                // error
            }

            var hero = heroNetObj.GetComponent<Hero>();
            hero.Initdata(data, this);

            // sync holders between server and client
            if(!IsHost)
            {
                Heros.Add(hero);
            }
        }
    }

    #endregion
}
