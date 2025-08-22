using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public partial class Monster
{
    public void GetDamage(int dmg)
    {
        if (!IsServer) return;
        if (isDead) return;

        HP -= dmg;

        isDead = HP <= 0;

        if (isDead)
        {
            BC_GetMoney_ClientRpc(1);
            BC_Dead_ClientRpc(HP, dmg);

            // 서버에서 삭제
            // 주의! 서버에서 미리 삭제하면 ClientRpc는 작동 안함
            //NetworkObject.Despawn(false);
            GameManager.Instance.RemoveMonster(this);
            StartCoroutine(CoDespawnAfter(1.0f));
        }
        else
        {
            BC_Hit_ClientRpc(HP, dmg);
        }
    }

    #region RPC
    [ServerRpc(RequireOwnership = false)]
    private void C2S_MonsterDeadServerRpc()
    {
        NetworkManager.Destroy(this);
    }
    #endregion
}
