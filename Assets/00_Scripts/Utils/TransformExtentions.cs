using System;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
    /// <summary>
    /// parent의 모든 자식 GameObject를 파괴합니다.
    /// shouldDelete가 주어지면 true인 자식만 삭제합니다.
    /// </summary>
    public static void DestroyAllChildren(
        this Transform parent,
        bool immediate = false,
        Func<Transform, bool> shouldDelete = null)
    {
        if (parent == null) return;

        // 컬렉션 복사 후 순회(반복 중 childCount 변화 안전)
        var children = new List<Transform>(parent.childCount);
        for (int i = 0; i < parent.childCount; i++)
            children.Add(parent.GetChild(i));

        foreach (var t in children)
        {
            if (shouldDelete != null && !shouldDelete(t))
                continue;

            // Netcode 사용 시: 스폰된 네트워크 오브젝트는 먼저 Despawn
#if UNITY_NETCODE_GAMEOBJECTS || UNITY_NETCODE
            var netObj = t.GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                // true: 네트워크에서 제거하면서 GameObject도 파괴
                netObj.Despawn(true);
                continue;
            }
#endif

#if UNITY_EDITOR
            if (!Application.isPlaying && immediate)
            {
                // 에디터에서 즉시 삭제 + Undo 지원
                UnityEditor.Undo.DestroyObjectImmediate(t.gameObject);
                continue;
            }
#endif
            if (!Application.isPlaying && immediate)
                UnityEngine.Object.DestroyImmediate(t.gameObject);
            else
                UnityEngine.Object.Destroy(t.gameObject); // 플레이모드 권장
        }
    }
}

/// <summary>
/// 인스펙터/컨텍스트 메뉴로 쉽게 실행하고 싶을 때 붙여 쓰는 컴포넌트
/// </summary>
public class ChildrenCleaner : MonoBehaviour
{
    [Tooltip("비우고 싶은 부모 Transform (비워두면 자기 자신)")]
    public Transform target;

    [ContextMenu("Clear Children (Play Mode-safe)")]
    public void ClearChildren_PlaySafe()
        => (target ? target : transform).DestroyAllChildren(immediate: false);

#if UNITY_EDITOR
    [ContextMenu("Clear Children (Editor Immediate)")]
    public void ClearChildren_EditorImmediate()
        => (target ? target : transform).DestroyAllChildren(immediate: true);
#endif
}
