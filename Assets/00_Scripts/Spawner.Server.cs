using System.Collections;
using Unity.Netcode;
using UnityEngine;

public partial class Spawner
{
    public BossStat dataBoss;

    private void StartServer()
    {
        SetGrid();
        StartCoroutine(CDealy(() =>
        {
            GenerateSpawnHolder();
            StartCoroutine(CSpawnMonster());
        }, 5f));
    } 

    public IEnumerator CSpawnMonster()
    {
        if (!IsServer) yield break;
        
        // �������� �̹� ���� ��� ���� �о �����ؼ� Ŭ������ �˸����� Ŭ��� ���� ��� ���� ���� ���ؼ� Ŭ�󿡼� �������� �ӽ÷� ���� ���� ������ ��
        yield return new WaitForSeconds(3.0f);

        var beforeWave = GameManager.Instance.curWave;
        while (!GameManager.Instance.inBoss)
        {
            var curWave = GameManager.Instance.curWave;
            bool isChangedWave = beforeWave != curWave;
            bool isBossWave = curWave % BOSS_WAVE == 0;
            bool isBossSpawn = isChangedWave && isBossWave;
            
            if (!isBossSpawn)
            {
                yield return new WaitForSeconds(MONSTER_SPAWN_INTERVAL);
            }
            
            for (int i = 0; i < NetworkManager.ConnectedClients.Count; i++)
            {
                Monster monster = null;

                if (isBossSpawn)
                {
                    var bossIdx = curWave / BOSS_WAVE - 1;
                    monster = Instantiate(dataBoss.listBossData[bossIdx].prfMonster);
                    GameManager.Instance.inBoss = true;
                    // GameManager.Instance.remainTime = 60;
                } else
                {
                    monster = Instantiate(prefMonster);
                }

                NetworkObject netObj = monster.GetComponent<NetworkObject>();
                netObj.Spawn();

                GameManager.Instance.AddMonster(monster, isBossSpawn);
                BC_MonsterSpawnClientRpc(netObj.NetworkObjectId, (ulong)i);
            }

            if(isChangedWave)
            {
                beforeWave = curWave;
            }
        }

    }
    private void HeroSpawn(ulong clientid, string holderName, string rarity, HeroStatData data)
    {
        if (!IsServer)
            return;

        var newPath = "";
        if (rarity == "Uncommon")
        {
            newPath = $"HeroData/{rarity}/{holderName}(Uncommon)";
            data = Resources.Load<HeroStat>(newPath).GetData();
        }

        var emptyHolder = FindEmptyHereHolderOrNull(clientid, data.heroName);
        if(emptyHolder == null)
        {
            Debug.LogWarning("There are not enought heroholder");
        }

        emptyHolder.SpawnHero(clientid, data, rarity);
    }

    [ServerRpc(RequireOwnership = false)]
    private void C2S_SpawnHeroHolder_ServerRpc(ulong clientId)
    {
        var h = Instantiate(spawnHolder);
        NetworkObject netObjHolder = h.GetComponent<NetworkObject>();
        netObjHolder.Spawn();
        
        BC_SpawnHeroHolder_ClientRpc(netObjHolder.NetworkObjectId, clientId);
    }

    #region RPC
    [ServerRpc(RequireOwnership = false)]
    private void C2S_SpawnHero_ServerRpc(ulong clientid, string holderName, string rarity, HeroStatData data)
    {
        Debug.Log($"[C->S]{nameof(C2S_SpawnHeroHolder_ServerRpc)}");
        HeroSpawn(clientid, holderName, rarity, data);
    }

    [ServerRpc(RequireOwnership = false)]
    private void C2S_GetPosition_ServerRpc(ulong clientId, int h1, int h2)
    {
        Debug.Log($"[C->S]{nameof(C2S_GetPosition_ServerRpc)}");

        BC_GetPosition_ClientRpc(clientId, h1, h2);
    }

    #endregion


}
