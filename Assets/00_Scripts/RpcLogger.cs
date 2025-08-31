using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

namespace RpcDebug
{
    public enum RpcDirection { ServerToClient, ClientToServer }
    public enum RpcKind { ClientRpc, ServerRpc }

    [Serializable]
    public struct RpcLogEntry
    {
        public DateTime Timestamp;
        public RpcKind Kind;
        public RpcDirection Direction;
        public string Method;          // RPC method name
        public ulong SenderClientId;   // who sent
        public string Targets;         // "All" or "1,3,5"
        public string PayloadSummary;  // small summary text (JSON 가능)
    }

    /// <summary>
    /// Runtime-safe logger used by RPC methods. GUI (EditorWindow) pulls entries via Entries.
    /// </summary>
    public static class RpcLogger
    {
        private static readonly ConcurrentQueue<RpcLogEntry> _queue = new();
        private static readonly List<RpcLogEntry> _buffer = new(2048);

        public static IReadOnlyList<RpcLogEntry> Entries => _buffer;

        public static void Log(
            RpcKind kind,
            RpcDirection dir,
            string method,
            ulong senderClientId,
            IEnumerable<ulong> targetIds = null,
            string payloadSummary = null,
            int historyLimit = 2000)
        {
            var targets = targetIds == null ? "All" : string.Join(",", targetIds);
            _queue.Enqueue(new RpcLogEntry
            {
                Timestamp = DateTime.Now,
                Kind = kind,
                Direction = dir,
                Method = method ?? "(null)",
                SenderClientId = senderClientId,
                Targets = targets,
                PayloadSummary = payloadSummary ?? string.Empty
            });

            while (_queue.Count > historyLimit && _queue.TryDequeue(out _)) { }
        }

        public static void LogRpc(MethodBase method, string payloadSummary = "")
        {
            RpcKind kind = RpcKind.ServerRpc;
            RpcDirection dir= RpcDirection.ClientToServer;
            ulong senderId = 0;

            var methodInfo = method as MethodInfo;
            if (methodInfo == null) return;

            var hasServerRpc = Attribute.IsDefined(methodInfo, typeof(ServerRpcAttribute));
            var hasClientRpc = Attribute.IsDefined(methodInfo, typeof(ClientRpcAttribute));

            if (hasServerRpc)
            {
                kind = RpcKind.ServerRpc;
                dir = RpcDirection.ClientToServer;
                senderId = NetworkManager.Singleton.LocalClientId; // or ServerRpcParams
            }
            else if (hasClientRpc)
            {
                kind = RpcKind.ClientRpc;
                dir = RpcDirection.ServerToClient;
                senderId = NetworkManager.Singleton.LocalClientId;
            }

            RpcLogger.Log(kind, dir, method.Name, senderId, payloadSummary: payloadSummary);
        }

        /// <summary>Move queue → buffer. Call from editor update/OnGUI.</summary>
        public static void Flush(int maxBuffer = 4000, int keepAfterTrim = 3000)
        {
            while (_queue.TryDequeue(out var e))
                _buffer.Add(e);

            if (_buffer.Count > maxBuffer)
                _buffer.RemoveRange(0, Math.Max(0, _buffer.Count - keepAfterTrim));
        }

#if UNITY_EDITOR
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void EditorDebugLogLast()
        {
            if (_buffer.Count > 0)
            {
                var e = _buffer[_buffer.Count - 1];
                Debug.Log($"[RPC] {e.Timestamp:HH:mm:ss.fff} {e.Kind} {e.Direction} {e.Method} Sender={e.SenderClientId} Targets={e.Targets} {e.PayloadSummary}");
            }
        }
#endif
    }


}
