using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace RpcDebug
{
    public enum RpcDirection { ServerToClient, ClientToServer }
    public enum RpcKind { ClientRpc, ServerRpc }

    [Serializable]
    public struct RpcLogEntry
    {
        public DateTime Timestamp;          // 최초 기록 시각
        public DateTime LastTimestamp;      // 마지막 합쳐진 기록 시각
        public int RepeatCount;             // 동일 로그가 연속으로 몇 번 합쳐졌는지 (최소 1)

        public RpcKind Kind;
        public RpcDirection Direction;
        public string Method;               // RPC method name
        public ulong SenderClientId;        // who sent
        public string Targets;              // "All" or "1,3,5"
        public string PayloadSummary;       // small summary text (JSON 가능)
    }

    /// <summary>
    /// Runtime-safe logger used by RPC methods. GUI (EditorWindow) pulls entries via Entries.
    /// - ConcurrentQueue(_queue): runtime push
    /// - List(_buffer): editor window shows (여기서 중복 압축 처리)
    /// </summary>
    public static class RpcLogger
    {
        private static readonly ConcurrentQueue<RpcLogEntry> _queue = new();
        private static readonly List<RpcLogEntry> _buffer = new(2048);

        // 큐 폭주 방지 하드 상한
        private static int _queueHardLimit = 20000;

        public static IReadOnlyList<RpcLogEntry> Entries => _buffer;

        public static void SetQueueHardLimit(int newLimit)
        {
            _queueHardLimit = Mathf.Clamp(newLimit, 1000, 200000);
        }

        public static void Log(
            RpcKind kind,
            RpcDirection dir,
            string method,
            ulong senderClientId,
            IEnumerable<ulong> targetIds = null,
            string payloadSummary = null,
            int historyLimit /*deprecated*/ = 2000 // 과거 호환
        )
        {
            var targets = targetIds == null ? "All" : string.Join(",", targetIds);
            var now = DateTime.Now;

            _queue.Enqueue(new RpcLogEntry
            {
                Timestamp = now,
                LastTimestamp = now,
                RepeatCount = 1,

                Kind = kind,
                Direction = dir,
                Method = method ?? "(null)",
                SenderClientId = senderClientId,
                Targets = targets,
                PayloadSummary = payloadSummary ?? string.Empty
            });

            // 큐 하드 상한 초과 시 앞에서 드롭
            while (_queue.Count > _queueHardLimit && _queue.TryDequeue(out _)) { }
        }

        public static void LogRpc(
            MethodBase method,
            string payloadSummary = "",
            ulong? senderOverride = null,
            IEnumerable<ulong> targetIds = null
        )
        {
            if (method is not MethodInfo mi) return;

            bool isServer = Attribute.IsDefined(mi, typeof(ServerRpcAttribute));
            bool isClient = Attribute.IsDefined(mi, typeof(ClientRpcAttribute));
            if (!isServer && !isClient) return;

            var kind = isServer ? RpcKind.ServerRpc : RpcKind.ClientRpc;
            var dir = isServer ? RpcDirection.ClientToServer : RpcDirection.ServerToClient;
            ulong sender = senderOverride ?? SafeLocalClientId();

            Log(kind, dir, mi.Name, sender, targetIds, payloadSummary);
        }

        /// <summary>
        /// Queue → Buffer. 이 시점에서 **중복 압축** 수행 (연속 동일 키면 합치기).
        /// </summary>
        public static void Flush(int maxBuffer = 4000, int keepAfterTrim = 3000)
        {
            // sane bounds
            if (maxBuffer < 100) maxBuffer = 100;
            if (keepAfterTrim < 50) keepAfterTrim = 50;
            if (keepAfterTrim > maxBuffer) keepAfterTrim = (int)(maxBuffer * 0.8f);

            while (_queue.TryDequeue(out var e))
            {
                if (_buffer.Count == 0)
                {
                    _buffer.Add(e);
                    continue;
                }

                // ref 대신: 복사해서 수정 후 다시 대입
                int lastIndex = _buffer.Count - 1;
                var last = _buffer[lastIndex];

                if (IsSameKey(last, e))
                {
                    last.RepeatCount += e.RepeatCount;
                    last.LastTimestamp = e.LastTimestamp;
                    _buffer[lastIndex] = last;   // 수정된 값 재대입
                }
                else
                {
                    _buffer.Add(e);
                }
            }

            // Auto-trim (앞에서 오래된 것 제거)
            if (_buffer.Count > maxBuffer)
            {
                int remove = Math.Max(0, _buffer.Count - keepAfterTrim);
                if (remove > 0) _buffer.RemoveRange(0, Math.Min(remove, _buffer.Count));
            }
        }

        private static bool IsSameKey(RpcLogEntry a, RpcLogEntry b)
        {
            return a.Kind == b.Kind
                && a.Direction == b.Direction
                && a.SenderClientId == b.SenderClientId
                && string.Equals(a.Method, b.Method, StringComparison.Ordinal)
                && string.Equals(a.Targets, b.Targets, StringComparison.Ordinal)
                && string.Equals(a.PayloadSummary, b.PayloadSummary, StringComparison.Ordinal);
        }

        /// <summary>표시 버퍼/큐 모두 비우기.</summary>
        public static void Clear()
        {
            while (_queue.TryDequeue(out _)) { }
            _buffer.Clear();
        }

        /// <summary>표시 버퍼를 최대 maxCount개로 유지(오래된 것부터 제거)</summary>
        public static void TrimTo(int maxCount)
        {
            if (maxCount <= 0) return;
            int over = _buffer.Count - maxCount;
            if (over > 0) _buffer.RemoveRange(0, over);
        }

#if UNITY_EDITOR
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void EditorDebugLogLast()
        {
            if (_buffer.Count <= 0) return;
            var e = _buffer[_buffer.Count - 1];
            Debug.Log($"[RPC x{e.RepeatCount}] {e.Timestamp:HH:mm:ss.fff}~{e.LastTimestamp:HH:mm:ss.fff} {e.Kind} {e.Direction} {e.Method} Sender={e.SenderClientId} Targets={e.Targets} {e.PayloadSummary}");
        }
#endif

        // --- helpers ---

        private static bool IsSameKey(ref RpcLogEntry a, ref RpcLogEntry b)
        {
            // "연속 동일 로그" 판단 기준: 내용 키 전부 동일
            return a.Kind == b.Kind
                && a.Direction == b.Direction
                && a.SenderClientId == b.SenderClientId
                && string.Equals(a.Method, b.Method, StringComparison.Ordinal)
                && string.Equals(a.Targets, b.Targets, StringComparison.Ordinal)
                && string.Equals(a.PayloadSummary, b.PayloadSummary, StringComparison.Ordinal);
        }

        private static ulong SafeLocalClientId()
        {
            try
            {
                var nm = NetworkManager.Singleton;
                if (nm != null) return nm.LocalClientId;
            }
            catch { }
            return 0;
        }
    }
}
