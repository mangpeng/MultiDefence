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
    private Coroutine mCoSlow;

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
            GameManager.Instance.RemoveMonster(this, isBoss);
            StartCoroutine(CoDespawnAfter(1.0f));
        }
        else
        {
            BC_Hit_ClientRpc(HP, dmg);
        }
    }


    private IEnumerator CoSlow(float slowAmount, float slowDuration)
    {
        sprRr.color = Color.blue;
        var newSpeed = mSpeed - (mSpeed * slowAmount);
        newSpeed = Mathf.Max(newSpeed, 0.1f);
        mSpeed = newSpeed;

        yield return new WaitForSeconds(slowDuration);
        sprRr.color = Color.white;
        mSpeed = MOVE_SPEED;
        mCoSlow = null;
    }

    #region RPC
    [ServerRpc(RequireOwnership = false)]
    private void C2S_MonsterDeadServerRpc()
    {
        NetworkManager.Destroy(this);
    }
    #endregion
}
