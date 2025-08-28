using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using WebSocketSharp;

public partial class Spawner : NetworkBehaviour
{
    public static Spawner instance = null;
    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    private const int GRID_X_COUNT = 6;
    private const int GRID_Y_COUNT = 3;

    public const int BOSS_WAVE = 5;

    [Header("Variables")]
    [SerializeField] private float MONSTER_SPAWN_INTERVAL = 1.0f;

    [SerializeField] private GameObject spawnHolder;
    [SerializeField] private Monster prefMonster;

    public List<Vector2> myMonsterMoveList = new List<Vector2>();
    public List<Vector2> otherMonsterMoveList = new List<Vector2>();

    public static List<Vector2> mySpawnList = new List<Vector2>();
    public static List<bool> mySpawnedList = new List<bool>(); // 소한된 위치 정보

    private List<Vector2> otherSpawnList = new List<Vector2>();
    public static List<bool> otherSpawnedList = new List<bool>(); // 소한된 위치 정보

    public Dictionary<ulong/*clientID*/, List<HeroHolder>> dicHolder = new();

    public static float xValue, yValue;

    public int cnt = 0;
    
    void Start()
    {
        StartServer();
    }

    #region Grid
    IEnumerator CDealy(Action action, float sec)
    {
        yield return new WaitForSeconds(sec);
        action?.Invoke();
    }
    private void SetGrid()
    {
        Debug.Log($"SetGrid {UtilManager.LocalID}");
        GridStart(transform.GetChild(0), isPlayer: true);
        GridStart(transform.GetChild(1), isPlayer: false);

        for (int i = 0; i < transform.GetChild(0).childCount - 1; i++)
        {
            myMonsterMoveList.Add(transform.GetChild(0).GetChild(i).position);
        }

        for (int i = 0; i < transform.GetChild(1).childCount - 1; i++)
        {
            otherMonsterMoveList.Add(transform.GetChild(1).GetChild(i).position);
        }
    }

    private void GridStart(Transform tr, bool isPlayer)
    {
        Debug.Log($"GridStart {UtilManager.LocalID}");
        var parentSprite = tr.GetComponent<SpriteRenderer>();
        float parentSpriteWidth = parentSprite.bounds.size.x;
        float parentSpriteHeight = parentSprite.bounds.size.y;

        float gridWidth = tr.localScale.x / GRID_X_COUNT;
        float gridHeight = tr.localScale.y / GRID_Y_COUNT;

        xValue = gridWidth;
        yValue = gridHeight;

        for (int row = 0; row < GRID_Y_COUNT; row++)
        {
            for (int col = 0; col < GRID_X_COUNT; col++)
            {
                float posX = (-parentSpriteWidth / 2) + (col * gridWidth) + (gridWidth / 2);
                float posY = ((isPlayer ? parentSpriteHeight : -parentSpriteHeight) / 2) + ((isPlayer ? -1 : 1 ) * (row * gridHeight)) + (gridHeight / 2);

                if (isPlayer)
                {
                    mySpawnList.Add(new Vector2(posX, posY + tr.position.y - gridHeight));
                    mySpawnedList.Add(false);
                    Debug.Log($"mylistSize: {mySpawnList.Count} {mySpawnedList.Count}");
                    // C2S_SpawnHeroHolder_ServerRpc(UtilManager.LocalID);

                }
                else
                {
                    otherSpawnList.Add(new Vector2(posX, posY + tr.position.y));
                    otherSpawnedList.Add(false);
                    Debug.Log($"mylistSize: {otherSpawnList.Count} {otherSpawnedList.Count}");
                }

                
            }
        }
    }

    private void GenerateSpawnHolder()
    {
        for (int row = 0; row < GRID_Y_COUNT; row++)
        {
            for (int col = 0; col < GRID_X_COUNT; col++)
            {
                C2S_SpawnHeroHolder_ServerRpc(UtilManager.LocalID);
            }
        }
    }
    #endregion

    #region Summon
    public void Summon(string rarity, HeroStat data)
    {
        Summon("", rarity, data);
    }

    public void Summon(string holderName, string rarity, HeroStat data = null)
    {                
        C2S_SpawnHero_ServerRpc(UtilManager.LocalID, holderName, rarity, data ? data.GetData() : new HeroStatData());

    }

    private void SetPositionHeroHolder(HeroHolder holder, List<Vector2> spawnList, List<bool> spawnedList)
    {
        spawnedList[holder.idx] = true;
        holder.transform.position = spawnList[holder.idx];
    }

    public void SwapHoldersChanges(ulong clientId, int h1, int h2)
    {
        C2S_GetPosition_ServerRpc(clientId, h1, h2);
    }

    private void GetPositionSet(ulong clientId, int h1, int h2)
    {
        
        var holder1 = dicHolder[clientId].Find((h) => h.idx == h1);
        var holder2 = dicHolder[clientId].Find((h) => h.idx == h2);

        holder1.HeroChange(holder2);
        holder2.HeroChange(holder1);

        (holder1.Heros, holder2.Heros) = (holder2.Heros, holder1.Heros);
        (holder1.HolderName, holder2.HolderName) = (holder2.HolderName, holder1.HolderName);
        (holder1.heroData, holder2.heroData) = (holder2.heroData, holder1.heroData);
    }

    #endregion

    #region RPC
    [ClientRpc]
    private void BC_GetPosition_ClientRpc(ulong clientId, int h1, int h2)
    {
        Debug.Log($"[S->C]{nameof(BC_GetPosition_ClientRpc)}");

        GetPositionSet(clientId, h1, h2);
    }

    [ClientRpc]
    private void BC_MonsterSpawnClientRpc(ulong netObjId, ulong clientid)
    {
        // Debug.Log($"[S->C]{nameof(BC_MonsterSpawnClientRpc)}");

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjId, out NetworkObject monsterNetObj)) 
        {
            var moveList = clientid == NetworkManager.Singleton.LocalClientId ? myMonsterMoveList : otherMonsterMoveList;
            monsterNetObj.transform.position = moveList[0];
            monsterNetObj.GetComponent<Monster>().Init(moveList);
        }
    }



    [ClientRpc]
    private void BC_SpawnHeroHolder_ClientRpc(ulong netObjId, ulong clientId)
    {
        Debug.Log($"[S->C]{nameof(BC_SpawnHeroHolder_ClientRpc)}");

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjId, out NetworkObject netObjHeroHolder))
        {
            bool isPlayer = NetworkManager.Singleton.LocalClientId == clientId;

            if (!dicHolder.TryGetValue(clientId, out var heroHolders))
            {
                dicHolder.Add(clientId, new());
            }

            var holder = netObjHeroHolder.GetComponent<HeroHolder>();
            
            var list = dicHolder[clientId];
            int idx = FindFirstMissingIndex(list, x => x.idx);
            holder.idx = idx;
            holder.clientId = clientId;
            dicHolder[clientId].Add(holder);

            Debug.LogWarning($"idx {idx}");
            var list1 = isPlayer ? mySpawnList : otherSpawnList;
            var list2 = isPlayer ? mySpawnedList : otherSpawnedList;
            Debug.LogWarning($"idx {idx} {list1.Count} {list1.Count}");
            SetPositionHeroHolder(holder,
                list1,
                list2);


            Debug.LogWarning($"IsPlayer{isPlayer} clientId: {clientId}, dic 사이즈: {dicHolder[clientId].Count}");
        }
    }

    #endregion

    #region Utils

    // 중간에 비어 있는 가장 작은 idx 를 찾습니다.
    int FindFirstMissingIndex<T>(IEnumerable<T> items, Func<T, int> selector)
    {
        var set = new HashSet<int>(items.Select(selector)); // idx만 추출
        int i = 0;
        while (set.Contains(i)) i++;
        return i;
    }

    public HeroHolder FindEmptyHereHolderOrNull(ulong clientid, string hereName)
    {
        return dicHolder[clientid].FindAll((holder) => (holder.HolderName == hereName && holder.Heros.Count < 3) || (holder.HolderName.IsNullOrEmpty())).FirstOrDefault();
    }

    public HeroStat GetRandomHeroCommonData()
    {
        HeroStat[] datas = Resources.LoadAll<HeroStat>("HeroData/Common");
        return datas[UnityEngine.Random.Range(0, datas.Length)];
    }

    #endregion
}
