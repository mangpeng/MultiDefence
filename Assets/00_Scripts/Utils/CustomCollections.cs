// Assets/00_Scripts/Utils/UniversalDictPayload.cs
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;   // ★ Newtonsoft.Json
using Unity.Netcode;

[Serializable]
public struct UniversalDictPayload : INetworkSerializable
{
    // (선택) 타입 힌트 – 디버깅/역직렬화 참고용
    public string KeyType;
    public string ValueType;

    // 실제 데이터는 JSON 문자열로 보관
    public List<string> KeysJson;
    public List<string> ValuesJson;

    // 생성자: 어떤 Dictionary든 가능
    public UniversalDictPayload(object dictionary, Type keyType, Type valueType)
    {
        KeyType = keyType?.AssemblyQualifiedName;
        ValueType = valueType?.AssemblyQualifiedName;
        KeysJson = new List<string>();
        ValuesJson = new List<string>();

        if (dictionary is IDictionary dict)
        {
            foreach (var k in dict.Keys)
            {
                KeysJson.Add(JsonConvert.SerializeObject(k, keyType, null));
                ValuesJson.Add(JsonConvert.SerializeObject(dict[k], valueType, null));
            }
        }
    }

    // 제네릭 헬퍼
    public static UniversalDictPayload From<TKey, TValue>(Dictionary<TKey, TValue> dict)
        => new UniversalDictPayload(dict, typeof(TKey), typeof(TValue));

    // 원래 타입으로 복원
    public Dictionary<TKey, TValue> ToDictionary<TKey, TValue>()
    {
        var result = new Dictionary<TKey, TValue>(KeysJson?.Count ?? 0);
        for (int i = 0; i < (KeysJson?.Count ?? 0); i++)
        {
            var k = JsonConvert.DeserializeObject<TKey>(KeysJson[i]);
            var v = JsonConvert.DeserializeObject<TValue>(ValuesJson[i]);
            result[k] = v;
        }
        return result;
    }

    public bool TryToDictionary<TKey, TValue>(out Dictionary<TKey, TValue> dict)
    {
        try { dict = ToDictionary<TKey, TValue>(); return true; }
        catch { dict = null; return false; }
    }

    // NGO 직렬화: string만 쓰므로 제약 없음
    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
    {
        s.SerializeValue(ref KeyType);
        s.SerializeValue(ref ValueType);

        int count = s.IsReader ? 0 : (KeysJson?.Count ?? 0);
        s.SerializeValue(ref count);

        if (s.IsReader)
        {
            KeysJson = new List<string>(count);
            ValuesJson = new List<string>(count);
        }

        for (int i = 0; i < count; i++)
        {
            string k = s.IsReader ? null : KeysJson[i];
            string v = s.IsReader ? null : ValuesJson[i];

            s.SerializeValue(ref k);
            s.SerializeValue(ref v);

            if (s.IsReader)
            {
                KeysJson.Add(k);
                ValuesJson.Add(v);
            }
        }
    }
}
