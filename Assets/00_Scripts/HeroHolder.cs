using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.ShaderData;
using static UnityEngine.GraphicsBuffer;

public partial class HeroHolder : NetworkBehaviour
{
    [SerializeField] private Hero _spawnHero;
    [SerializeField] private Transform circleRange;
    [SerializeField] private Transform square;
    [SerializeField] private Transform circle;
    [SerializeField] private GameObject objCanvas;

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
        
        btnSell.onClick.AddListener(EventSell);
        btnCompose.onClick.AddListener(EventComposition);
    }

    #region Event
    public void EventSell()
    {        
        C2S_SellHero_ServerRpc(UtilManager.LocalID);
    }

    public void EventComposition()
    {
        List<HeroHolder> holders = new List<HeroHolder>();

        foreach (var holder in Spawner.instance.dicHolder[clientId])
        {
            if (holder.HolderName == HolderName)
            {
                holders.Add(holder);
            }
        }

        const int NEED_HERO_COUNT = 2;
        List<Hero> heros = new();
        for (int i = 0; i < holders.Count; i++)
        {
            foreach (var h in holders[i].Heros)
            {
                heros.Add(h);
            }
        }

        if (heros.Count < NEED_HERO_COUNT)
        {
            Debug.LogWarning($"Not enough to sell heros. heroCount: {heros.Count}");
            return;
        }

        for(int i = 0; i < NEED_HERO_COUNT; i++)
        {
            heros[i].Sell(UtilManager.LocalID);
        }

        Spawner.instance.Summon(HolderName, "Uncommon");
    }
    #endregion
    #region UI
    public void ShowSquare(bool isShow) => square.gameObject.SetActive(isShow);
    public void ShowCircle(bool isShow) => circle.gameObject.SetActive(isShow);
    public void ShowRange()
    {
        circleRange.localScale = new Vector3(heroData.heroRange * 2, heroData.heroRange * 2, 1);
        circleRange.gameObject.SetActive(true);
        objCanvas.SetActive(true);
    }
    public void HideRange()
    {
        circleRange.gameObject.SetActive(false);
        objCanvas.SetActive(false);
    }
    #endregion
    #region RPC
    [ClientRpc]
    public void BC_SellHero_ClientRpc(ulong netObjId, ulong clientid)
    {
        Debug.Log($"[S->C]{nameof(BC_SellHero_ClientRpc)}");

        var netGo = NetworkManager.Singleton.SpawnManager.SpawnedObjects[netObjId]; // 이것도 위험해 보이는데...
        Heros.Remove(netGo.GetComponent<Hero>());
        if (Heros.Count == 0)
        {
            if (IsHost) // 같은 요청을 모든 클라가 할 필요가 없어서 임시로.
                C2S_DestroyHeroHolder_ServerRpc(clientid);
        }
        else
        {
            CheckGetPosition();
        }
    }
    
    [ClientRpc]
    public void BC_DestroyHeroHolder_ClientRpc(ulong clientId)
    {
        Debug.Log($"[S->C]{nameof(BC_DestroyHeroHolder_ClientRpc)}");

        Spawner.instance.dicHolder[clientId].Remove(this);
        bool isMe = clientId == UtilManager.LocalID;
        var spawnedList = isMe ? Spawner.mySpawnedList : Spawner.otherSpawnedList;
        spawnedList[idx] = false;
    }
    
    [ClientRpc]
    private void BC_ClientHeroSpawn_ClientRpc(ulong netObjId, ulong clientid, HeroStatData data, string rarity)
    {
        Debug.Log($"[S->C]{nameof(BC_ClientHeroSpawn_ClientRpc)}");

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjId, out NetworkObject heroNetObj))
        {
            heroData = data;

            var hero = heroNetObj.GetComponent<Hero>();
            hero.Initdata(data, this, rarity);

            // sync holders between server and client
            if(!IsHost)
            {
                Heros.Add(hero);
            }

            CheckGetPosition();
        }
    }
    #endregion
    
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

    public void SpawnHeroHolder(HeroStatData data, ulong clientid, string rarity)
    {
        if (Heros.Count == 0)
        {
            HolderName = data.heroName;
            this.clientId = clientid;
        }

        // 클라들이 중복된 요청 서버에게 보내는 것을 처리하기 위해 임시처리.
        // 서버에서 holder 생성 -> hero 생성을 한번에 처리 하도록 수정 필요.
        if (IsHost)
        {
            C2S_SpawnHero_ServerRpc(NetworkManager.Singleton.LocalClientId, data, rarity);
        }
    }

    private void SpawnHero(ulong clientId, HeroStatData data, string rarity)
    {
        if (!IsServer)
            return;

        var go = Instantiate(_spawnHero);
        Heros.Add(go);


        NetworkObject netObj = go.GetComponent<NetworkObject>();
        netObj.Spawn();
        netObj.transform.parent = this.transform;

        BC_ClientHeroSpawn_ClientRpc(netObj.NetworkObjectId, clientId, data, rarity);
    }
    private void CheckGetPosition()
    {
        int siblingCount = Heros.Count;

        if (siblingCount == 1)
        {
            transform.GetChild(4).transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            transform.GetChild(4).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 3;
        }
        else if (siblingCount == 2)
        {
            transform.GetChild(4).transform.localPosition = new Vector3(-0.1f, 0.0f, 0.0f);
            transform.GetChild(4).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 3;
            transform.GetChild(5).transform.localPosition = new Vector3(0.1f, 0.0f, 0.0f);
            transform.GetChild(5).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 4;
        }
        else if (siblingCount == 3)
        {
            transform.GetChild(4).transform.localPosition = new Vector3(-0.1f, 0.05f, 0.0f);
            transform.GetChild(4).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 3;
            transform.GetChild(5).transform.localPosition = new Vector3(0.1f, 0.05f, 0.0f);
            transform.GetChild(5).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 4;
            transform.GetChild(6).transform.localPosition = new Vector3(0.0f, -0.05f, 0.0f);
            transform.GetChild(6).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = 5;
        }
        else
        {
            // error
        }
    }
}
