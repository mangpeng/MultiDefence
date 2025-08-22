using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace IGN.Common.Actions
{
    [System.Flags]
    public enum ActionFlag
    {
        None = 0,
        ByPlayer = 1 << 0,
        BySystem = 1 << 1,
        ByTimeout = 1 << 2,
        ByQuest = 1 << 3,
        ByShop = 1 << 4,
    }

    [System.Flags]
    public enum ContentFlag
    {
        None = 0,
        BySell = 1 << 0,
        ByComposition = 1 << 1,
    }

    public struct ActionContext : INetworkSerializable
    {
        public ActionFlag ActionFlags;
        public ContentFlag ContentFlags;
        public FixedString64Bytes Caller;   // string 대체 (null 불가, 기본값은 빈 문자열)
        public FixedString128Bytes Reason;  // 필요에 따라 크기 조절
        public float TimeStamp;

        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref ActionFlags);
            s.SerializeValue(ref ContentFlags);
            s.SerializeValue(ref Caller);
            s.SerializeValue(ref Reason);
            s.SerializeValue(ref TimeStamp);
        }

        // 편의 생성자/팩토리 (원하면)
        public static ActionContext Player(string caller, string reason) => new()
        {
            ActionFlags = ActionFlag.ByPlayer,
            ContentFlags = ContentFlag.None,
            Caller = caller,
            Reason = reason,
            TimeStamp = UnityEngine.Time.time
        };
    }
}
//    /// <summary>
//    /// Who/why/when context for hero-related actions.
//    /// Prefer using static factories for consistency.
//    /// </summary>
//    public struct ActionContext
//    {
//        public ActionFlag ActionFlags;
//        public ContentFlag ContentFlags;
        
//        public string Caller;
//        public string Reason;
//        public float TimeStamp;

//        public ActionContext(ActionFlag actionFlags, ContentFlag contentFlags, string caller, string reason, float timeStamp)
//        {
//            ActionFlags = actionFlags;
//            ContentFlags = contentFlags;
//            Caller = caller;
//            Reason = reason;
//            TimeStamp = timeStamp;
//        }

//        // ✅ 편의 생성기 (일관된 사용)
//        public static ActionContext Player(string caller, string reason)
//            => new(ActionFlag.ByPlayer, ContentFlag.None, caller, reason, Time.time);

//        public static ActionContext System(string caller, string reason, ActionFlag extra = ActionFlag.None)
//            => new(ActionFlag.BySystem | extra, ContentFlag.None, caller, reason, Time.time);
//    }

//    /// <summary>Flags 헬퍼</summary>
//    public static class HeroActionFlagExtensions
//    {
//        public static bool IsPlayer(this ActionFlag f) => f.HasFlag(ActionFlag.ByPlayer);
//        public static bool IsSystem(this ActionFlag f) => f.HasFlag(ActionFlag.BySystem);
//        public static ActionFlag With(this ActionFlag f, ActionFlag add) => f | add;
//        public static ActionFlag Without(this ActionFlag f, ActionFlag remove) => f & ~remove;
//    }
//}

/*
 * Example
 */
//using IGN.Common.Actions;

//var ctx1 = ActionContext.Player("SellButton", "User clicked sell");
//var ctx2 = ActionContext.System("ServerLogic", "Auto-cleanup", ActionFlags.ByTimeout | ActionFlags.ByShop);

//if (ctx1.Flags.IsPlayer()) { /* 플레이어 분기 */ }
