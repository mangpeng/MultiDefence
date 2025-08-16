using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

public class Spawner : NetworkBehaviour
{
    private const int GRID_X_COUNT = 6;
    private const int GRID_Y_COUNT = 3;

    [Header("Variables")]
    [SerializeField] private float MONSTER_SPAWN_INTERVAL = 1.0f;

    [SerializeField] private GameObject prefPlayer;
    [SerializeField] private Monster prefMonster;

    public List<Vector2> myMonsterMoveList = new List<Vector2>();
    public List<Vector2> otherMonsterMoveList = new List<Vector2>();

    private List<Vector2> mySpawnList = new List<Vector2>();
    private List<bool> mySpawnedList = new List<bool>(); // 소한된 위치 정보

    private List<Vector2> otherSpawnList = new List<Vector2>();
    private List<bool> otherSpawnedList = new List<bool>(); // 소한된 위치 정보

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

        if (IsClient)
        {
            ServerHeroSpawnServerRpc(LocalID());
        }
        else
        {
            HeroSpawn(LocalID());
        }
    }

    private void HeroSpawn(ulong clientid)
    {
        var hero = Instantiate(prefPlayer);
        NetworkObject netObj = hero.GetComponent<NetworkObject>();
        netObj.Spawn();

        ClientHeroSpawnClientRpc(netObj.NetworkObjectId, clientid);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerHeroSpawnServerRpc(ulong clientid)
    {
        HeroSpawn(clientid);
    }

    [ClientRpc]
    private void ClientHeroSpawnClientRpc(ulong netObjId, ulong clientid)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjId, out NetworkObject heroNetObj))
        {
            bool isPlayer = NetworkManager.Singleton.LocalClientId == clientid;
            SetPositionHero(heroNetObj, isPlayer);
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
