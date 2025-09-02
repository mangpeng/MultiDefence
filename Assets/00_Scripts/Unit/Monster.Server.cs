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
    private Coroutine[] mCoDebuff = new Coroutine[Enum.GetValues(typeof(Debuff)).Length];

    public void GetDamage(ulong attacker, int dmg)
    {
        if (!IsServer) return;
        if (isDead) return;

        var preHp = HP;

        HP -= dmg;
        HP = (int)MathF.Max(HP, 0);

        var finalDamage = preHp - HP;
        if(GameManager.Instance.dicAccDamage.ContainsKey(attacker))
        {
            GameManager.Instance.dicAccDamage[attacker] += finalDamage;
        } else
        {
            GameManager.Instance.dicAccDamage.Add(attacker, finalDamage);
        }

        isDead = HP <= 0;

        if (isDead)
        {
            BC_GetMoney_ClientRpc(1);
            BC_Dead_ClientRpc(HP, dmg);

            // 서버에서 삭제
            // 주의! 서버에서 미리 삭제하면 ClientRpc는 작동 안함
            //NetworkObject.Despawn(false);
            GameManager.Instance.RemoveMonster(this, isBoss);
            StartCoroutine(CoDespawnAfter(1.0f));
        }
        else
        {
            BC_Hit_ClientRpc(HP, dmg);
        }
    }

    #region RPC
    [ServerRpc(RequireOwnership = false)]
    public void C2S_Debuff_ServerRpc(Debuff type, float[] values)
    {
        BC_Debuff_ClientRpc(type, values);
    }
    #endregion
}
