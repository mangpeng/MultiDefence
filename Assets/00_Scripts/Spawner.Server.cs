using System.Collections;
using Unity.Netcode;
using UnityEngine;

public partial class Spawner
{
    IEnumerator CSpawnMonster()
    {
        if (!IsServer) yield break;
        
        // �������� �̹� ���� ��� ���� �о �����ؼ� Ŭ������ �˸����� Ŭ��� ���� ��� ���� ���� ���ؼ� Ŭ�󿡼� �������� �ӽ÷� ���� ���� ������ ��
        yield return new WaitForSeconds(3.0f);

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

    }
    private void HeroSpawn(ulong clientid, string holderName, string rarity)
    {
        if (!IsServer)
            return;

        HeroStat[] datas = Resources.LoadAll<HeroStat>("HeroData/Common");
        var data = datas[UnityEngine.Random.Range(0, datas.Length)];

        var newPath = "";
        if (rarity == "Uncommon")
        {
            newPath = $"HeroData/{rarity}/{holderName}(Uncommon)";
            data = Resources.Load<HeroStat>(newPath);
        }

        if (!dicHolder.TryGetValue(clientid, out var heroHolders))
        {
            dicHolder.Add(clientid, new());
        }

        var emptyHolder = FindEmptyHereHolderOrNull(clientid, data.Name);
        if (emptyHolder != null)
        {
            emptyHolder.SpawnHeroHolder(data.GetData(), clientid, rarity);
            return;
        }

        var h = Instantiate(spawnHolder);
        NetworkObject netObjHolder = h.GetComponent<NetworkObject>();
        netObjHolder.Spawn();

        var holder = h.GetComponent<HeroHolder>();

        var list = dicHolder[clientid];
        int idx = FindFirstMissingIndex(list, x => x.idx);

        holder.GetComponent<HeroHolder>().idx = idx;
        dicHolder[clientid].Add(holder.GetComponent<HeroHolder>());

        BC_SpawnHeroHolder_ClientRpc(netObjHolder.NetworkObjectId, clientid, data.GetData(), rarity);
    }

    #region RPC
    [ServerRpc(RequireOwnership = false)]
    private void C2S_SpawnHeroHolder_ServerRpc(ulong clientid, string holderName, string rarity)
    {
        Debug.Log($"[C->S]{nameof(C2S_SpawnHeroHolder_ServerRpc)}");
        HeroSpawn(clientid, holderName, rarity);
    }

    [ServerRpc(RequireOwnership = false)]
    private void C2S_GetPosition_ServerRpc(ulong clientId, int h1, int h2)
    {
        Debug.Log($"[C->S]{nameof(C2S_GetPosition_ServerRpc)}");

        BC_GetPosition_ClientRpc(clientId, h1, h2);
    }

    #endregion


}
