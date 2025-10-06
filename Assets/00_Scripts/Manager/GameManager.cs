using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public delegate void OnMoneyEventHandler();
public delegate void OnUpdateUIEventHandler();


public partial class GameManager : NetworkBehaviour
{
    public static GameManager Instance => Singleton<GameManager>.Instance;

    public event OnUpdateUIEventHandler OnUpdateUIWave;
    public event Action<bool> OnUpdateUITime;

    public int Money = 50;
    public int SummonNeedMoney = 20;
    public int HeroCount = 0;
    public const int MAX_HERO_COUNT = 25;
    public const int MAX_MONSTER_COUNT = 100;

    public List<Monster> Monsters = new();
    public int MonsterCount;

    public List<Monster> BossMonsters = new();

    public event OnMoneyEventHandler OnMoney;

    public int[] mUpgrade = new int[4];

    private void Start()
    {
        if (IsServer)
        {
            StartServer();
        }

        if (IsClient)
        {
            StartClient();
        }
    }
    private void Update()
    {
        if (IsServer)
        {
            UpdateServer();
        }

        if (IsClient)
        {
            UpdateClient();
        }
    }

    private void StartClient()
    {
        CS_UpdateNickName_ServerRpc(UtilManager.LocalID, CloudManager.Instance.m_dataPlayer.m_id);
    }

    private void UpdateClient()
    {
        
    }

    public void GetMoney(int value)
    {
        Money += value;
        OnMoney?.Invoke();
    }

    public void AddMonster(Monster m, bool isBoss)
    {
        if(isBoss) 
        {
            BossMonsters.Add(m);
        } 
        else
        {
            Monsters.Add(m);
        }
            
        MonsterCount++;
        
        BC_ClientMonsterCount_ClientRpc(MonsterCount, false); //TODO 서버에게 요청 하고 처리 하도록 변경 필요

        if (MonsterCount >= MAX_MONSTER_COUNT)
        {
            Spawner.instance.StopSpawn();

            var payload = UniversalDictPayload.From(dicAccDamage);
            BC_GameOver_ClientRpc(curWave, payload);
        }
    }

    public void RemoveMonster(Monster m, bool isBoss)
    {
        bool deadBoss = false;

        if (isBoss)
        {
            BossMonsters.Remove(m);
            if(BossMonsters.Count == 0)
            {
                inBoss = false;
                deadBoss = true;

                //
                if (coCountDown != null)
                {
                    StopCoroutine(coCountDown);
                }
                remainTime = DEFAULT_REMAIN_TIME;
                ++curWave;
                coCountDown = StartCoroutine(CoCountdown());

                StartCoroutine(Spawner.instance.CoSpawnMonster());
                //


            }
        }
        else
        {
            Monsters.Remove(m);
        }
        
        MonsterCount--;
        BC_ClientMonsterCount_ClientRpc(MonsterCount, deadBoss); //TODO 서버에게 요청 하고 처리 하도록 변경 필요
    }

    public List<Monster> FindMonsters(Vector3 worldCenterPos, float radius)
    {
        List<Monster> result = new List<Monster>();

        foreach (var monster in Monsters)
        {
            if (monster == null) continue;

            float dist = Vector3.Distance(worldCenterPos, monster.transform.position);
            if (dist <= radius)
            {
                result.Add(monster);
            }
        }

        return result;
    }

    public List<Hero> FindHeros(Vector3 worldCenterPos, float radius)
    {
        List<Hero> result = new List<Hero>();

        foreach (var (clientId, holders) in Spawner.instance.dicHolder)
        {
            foreach (var holder in holders)
            {
                foreach (var hero in holder.Heros)
                {
                    float dist = Vector3.Distance(worldCenterPos, hero.transform.position);
                    if (dist <= radius)
                    {
                        result.Add(hero);
                    }
                }
            }
        }

        return result;
    }

    #region RPC
    [ClientRpc]
    private void BC_GameOver_ClientRpc(int curWave, UniversalDictPayload payload)
    {
        var dic = payload.ToDictionary<ulong, int>();
        if (dic == null || dic.Count != 2) return;

        UIMain.Instance.OnGameOver(curWave, dic);
    }

    [ClientRpc]
    private void BC_ClientMonsterCount_ClientRpc(int count, bool isDeadBoss)
    {
        // Debug.Log($"[S->C]{nameof(BC_ClientMonsterCount_ClientRpc)}");

        MonsterCount = count;

        if(isDeadBoss)
        {
            UIMain.Instance.objBossWaveCount.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    private void BC_UpdateTime_ClientRpc(int remainTime, int curWave, bool changedWave)
    {
        // Debug.Log($"[S->C]{nameof(BC_UpdateTime_ClientRpc)}");

        this.remainTime = remainTime;
        this.curWave = curWave;

        string bossName = string.Empty;
        bool isBossWave = curWave % Spawner.BOSS_WAVE == 0;

        OnUpdateUIWave?.Invoke();
        OnUpdateUITime?.Invoke(isBossWave);
        
        if (changedWave)
        {
            if (isBossWave)
            {
                var bossIdx = curWave / Spawner.BOSS_WAVE - 1;
                bossName = Spawner.instance.dataBoss.listBossData[bossIdx].bossName;
            }

            UIMain.Instance.OnWavePopup(curWave, bossName);
        }
    }

    [ClientRpc]
    // fixme 애초에 게임 접속시 서버, 클라 모두 플레이어 정보를 알수 있도록 변경 필요.
    private void S2C_SelectNickName_ClientRpc(ulong clientId, string id, ClientRpcParams clientRpcParams = default)
    {
        // host는 이미 서버 로직에서 캐싱 되어 있으므로
        if(!IsHost)
        {
            Debug.Log($"{clientId}, {id}");
            dicPlayers.Add(clientId, id);
        }

        bool isMe = clientId == UtilManager.LocalID;
        UIMain.Instance.UpdateProfile(isMe, id);
    }
    #endregion
}
