using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class Spawner : NetworkBehaviour
{
    private const int GRID_X_COUNT = 6;
    private const int GRID_Y_COUNT = 3;

    [Header("Variables")]
    [SerializeField] private float MONSTER_SPAWN_INTERVAL = 1.0f;

    [SerializeField] private GameObject spawnHolder;
    [SerializeField] private Monster prefMonster;

    public List<Vector2> myMonsterMoveList = new List<Vector2>();
    public List<Vector2> otherMonsterMoveList = new List<Vector2>();

    private List<Vector2> mySpawnList = new List<Vector2>();
    private List<bool> mySpawnedList = new List<bool>(); // 소한된 위치 정보

    private List<Vector2> otherSpawnList = new List<Vector2>();
    private List<bool> otherSpawnedList = new List<bool>(); // 소한된 위치 정보

    Dictionary<ulong/*clientID*/, List<HeroHolder>> dicHolder = new();

    public static float xValue, yValue;

    void Start()
    {
        SetGrid();
        StartCoroutine(CSpawnMonster());
    }

    private void SetGrid()
    {
        GridStart(transform.GetChild(0), isPlayer: true);
        GridStart(transform.GetChild(1), isPlayer: false);

        for (int i = 0; i < transform.GetChild(0).childCount; i++)
        {
            myMonsterMoveList.Add(transform.GetChild(0).GetChild(i).position);
        }

        for (int i = 0; i < transform.GetChild(1).childCount; i++)
        {
            otherMonsterMoveList.Add(transform.GetChild(1).GetChild(i).position);
        }
    }

    #region Make Grid
    private void GridStart(Transform tr, bool isPlayer)
    {
        var parentSprite = tr.GetComponent<SpriteRenderer>();
        float parentSpriteWidth = parentSprite.bounds.size.x;
        float parentSpriteHeight = parentSprite.bounds.size.y;

        float gridWidth = tr.localScale.x / GRID_X_COUNT;
        float gridHeight = tr.localScale.y / GRID_Y_COUNT;

        xValue = gridWidth;
        yValue = gridHeight;

        Debug.Log($"{xValue} {yValue}");

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
                }
                else
                {
                    otherSpawnList.Add(new Vector2(posX, posY + tr.position.y));
                    otherSpawnedList.Add(false);
                }
            }
        }
    }
    #endregion

    #region SummonCharacter
    public void Summon()
    {
        //if (GameManager.instance.Money < GameManager.instance.SummonCount)
        //    return;

        //GameManager.instance.Money -= GameManager.instance.SummonCount;
        //GameManager.instance.SummonCount += 2;


        //if (IsClient)
        //{
        //    ServerHeroSpawnServerRpc(LocalID());
        //}
        //else
        //{
        //    HeroSpawn(LocalID());
        //}
        
        ServerHeroSpawnHolderServerRpc(LocalID());
    }

    private void HeroSpawn(ulong clientid)
    {
        if (!IsServer)
            return;


        HeroStat[] datas = Resources.LoadAll<HeroStat>("HeroData");
        var data = datas[UnityEngine.Random.Range(0, datas.Length)];
        

        if(!dicHolder.TryGetValue(clientid, out var heroHolders))
        {
            dicHolder.Add(clientid, new());
        }

        bool isAdd = false;
        dicHolder[clientid].ForEach((holder) =>
        {
            if(holder.HolderName == data.Name && holder.Heros.Count < 3)
            {
                holder.SpawnHeroHolder(data.GetData());
                isAdd = true;
                return;
            }
        });

        if (isAdd)
            return;

        var h = Instantiate(spawnHolder);
        dicHolder[clientid].Add(h.GetComponent<HeroHolder>());
        NetworkObject netObjHolder = h.GetComponent<NetworkObject>();
        netObjHolder.Spawn();

        ClientHeroHolderSpawnClientRpc(netObjHolder.NetworkObjectId, clientid, data.GetData());

        //foreach(var dd in dicHolder)
        //{
        //    if(dd.Value.Heros.Count < 3 && dd.Value.HolderName == data.Name)
        //    {
        //        dd.Value.SpawnHeroHolder(data.GetData());
        //        return;
        //    }
        //}

        //var h = Instantiate(spawnHolder);
        //// dicHolder.Add(dicHolder.Count.ToString(), h.GetComponent<HeroHolder>());
        //NetworkObject netObjHolder = h.GetComponent<NetworkObject>();
        //netObjHolder.Spawn();

        //ClientHeroHolderSpawnClientRpc(netObjHolder.NetworkObjectId, clientid, data.GetData());
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerHeroSpawnHolderServerRpc(ulong clientid)
    {
        HeroSpawn(clientid);
    }

    [ClientRpc]
    private void ClientHeroHolderSpawnClientRpc(ulong netObjId, ulong clientid, HeroStatData data)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjId, out NetworkObject heroNetObj))
        {
            bool isPlayer = NetworkManager.Singleton.LocalClientId == clientid;
            Debug.Log(isPlayer);
            SetPositionHero(heroNetObj, isPlayer);
            
            heroNetObj.GetComponent<HeroHolder>().SpawnHeroHolder(data);
        }
    }

    private void SetPositionHero(NetworkObject netObj, bool Player)
    {
        List<bool> spawnedList = Player ? mySpawnedList : otherSpawnedList;
        List<Vector2> spawnList = Player ? mySpawnList : otherSpawnList;

        int positionValue = -1;
        for (int i = 0; i < spawnedList.Count; i++)
        {
            if (spawnedList[i] == false)
            {
                positionValue = i;
                spawnedList[i] = true;
                break;
            }
        }

        netObj.transform.position = spawnList[positionValue];
    }

    #endregion

    #region SummonMonster
    IEnumerator CSpawnMonster()
    {
        // 서버에서 이미 몬스터 경로 정보 읽어서 스폰해서 클라한테 알리지만 클라는 몬스터 경로 정보 읽지 못해서 클라에서 에러나서 임시로 최초 스폰 딜레이 줌
        yield return new WaitForSeconds(3.0f);

        // yield return new WaitForSeconds(MONSTER_SPAWN_INTERVAL);

        //if (IsClient)
        //{
        //    ServerMonsterSpawnServerRpc(NetworkManager.Singleton.LocalClientId);
        //}
        //else
        //{
        //    MonsterSpawn(NetworkManager.Singleton.LocalClientId);
        //}

        if (!IsServer) yield break;

        while (true)
        {
            yield return new WaitForSeconds(MONSTER_SPAWN_INTERVAL);

            for (int i = 0; i < NetworkManager.ConnectedClients.Count; i++)
            {
                var monster = Instantiate(prefMonster);
                NetworkObject netObj = monster.GetComponent<NetworkObject>();
                netObj.Spawn();

                GameManager.instance.AddMonster(monster);
                BC_MonsterSpawnClientRpc(netObj.NetworkObjectId, (ulong)i);
            }
        }
        
        // StartCoroutine(CSpawnMonster());
    }

    [ClientRpc]
    private void BC_MonsterSpawnClientRpc(ulong netObjId, ulong clientid)
    {
        if(NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjId, out NetworkObject monsterNetObj))
        {
            if (clientid == NetworkManager.Singleton.LocalClientId)
            {
                monsterNetObj.transform.position = myMonsterMoveList[0];
                monsterNetObj.GetComponent<Monster>().Init(myMonsterMoveList);
            }
            else
            {
                monsterNetObj.transform.position = otherMonsterMoveList[0];
                monsterNetObj.GetComponent<Monster>().Init(otherMonsterMoveList);
            }
        }
    }

    private ulong LocalID()
    {
        return NetworkManager.Singleton.LocalClientId;
    }

    #endregion
}
