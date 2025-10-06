using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

[System.Serializable]
public class DataHero
{
    public string m_name;
    public Rarity m_rarity;
    public int m_count;
    public int m_level;

    public DataHero(string name, Rarity rarity, int count, int level)
    {
        m_name = name;
        m_rarity = rarity;
        m_count = count;
        m_level = level;
    }
}

[System.Serializable]
public class DataPlayer
{
    public string m_id;
    public int m_level;
    public int m_wave;
    public int m_dia;
    public Dictionary<string, DataHero> m_dicHero;

    public DataPlayer(string id, int level, int wave, int dia)
    {
        m_id = id;
        m_level = level;
        m_wave = wave;
        m_dia = dia;

        m_dicHero = new();

        for(int i = 0; i < 2; i++)
        {
            var rarity = (Rarity)i;
            var datas = ResourceManager.GetHeroDataByRarityOrNull(rarity);
            if(datas != null && datas.Count != 0)
            {
                foreach (var data in datas)
                {
                    m_dicHero.Add(data.Name, new DataHero(data.Name, data.rarity, 0, 0));
                }
            }
        }
    }
}

public delegate void OnDiaEvent();

public class CloudManager : Singleton<CloudManager>
{
    public event OnDiaEvent onAddDiaEvent;
    public event OnDiaEvent onRemoveDiaEvent;

    public const string KEY_PLAYER_DATA = "PlayerData";

    public DataPlayer m_dataPlayer;

    private float m_elapsedTime = 0.0f;
    private bool _saving;
    
    //fixme 싱글톤 한번도 호출 안되면 애초에 update문도 안 도는거 아닌가?
    private void Update()
    {
        m_elapsedTime += Time.unscaledDeltaTime;

        if (!_saving && m_elapsedTime >= 10f)
        {
            _ = SaveAsync(); // fire-and-forget (호출부는 기다리지 않음)
        }
    }

    public async Task SaveAsync()
    {
        _saving = true;
        try
        {
            await SavePlayerData(m_dataPlayer); // 여기서 프레임은 계속 진행됨(블로킹 X)
            m_elapsedTime = 0f;                 // 완료 후에만 리셋
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);              // 예외 안전하게 처리
        }
        finally
        {
            _saving = false;
        }
    }

    public async Task SavePlayerData(DataPlayer data)
    {
        try
        {
            var jsonObj = JsonConvert.SerializeObject(data);
            var jsonDic = new Dictionary<string, object> { { KEY_PLAYER_DATA, jsonObj } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(jsonDic);
            Debug.Log($"Saved player data: {jsonObj}");
        }
        catch (System.Exception e)
        {

            Debug.LogError(e.Message);
        }
    }

    public async Task<DataPlayer> LoadPlayerData()
    {
        try
        {
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { KEY_PLAYER_DATA });
            if (data.TryGetValue(KEY_PLAYER_DATA, out var item))
            {
                string jsonStr = item.Value.GetAsString();
                DataPlayer dataPlayer = JsonConvert.DeserializeObject<DataPlayer>(jsonStr);
                m_dataPlayer = dataPlayer;
                UISceneMain.Instance.SetLevel(m_dataPlayer.m_level);
                return dataPlayer;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }

        DataPlayer defaultData = new DataPlayer(id: null, level: 1, wave: 0, dia: 0);
        m_dataPlayer = defaultData;
        return defaultData;
    }

    public async Task DeletePlayerData()
    {
        try
        {
            await CloudSaveService.Instance.Data.Player.DeleteAsync(KEY_PLAYER_DATA);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    //
    public bool AddDia(int addValue)
    {
        long tmp = m_dataPlayer.m_dia + addValue;
        if (tmp > Int32.MaxValue)
        {
            Debug.LogWarning("Exceed int32 max value");
            return false;
        }

        if(tmp < 0)
        {
            Debug.LogWarning("Can't be below zero");
            return false;
        }

        m_dataPlayer.m_dia += addValue;

        if (addValue > 0)
        {
            onAddDiaEvent?.Invoke();
        }

        if (addValue < 0)
        {
            onRemoveDiaEvent?.Invoke();
        }

        return true;
    }
}

