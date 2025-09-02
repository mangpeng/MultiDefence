using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

[System.Serializable]
public class DataPlayer
{
    public string m_id;
    public int m_level;


    public DataPlayer(string id, int level)
    {
        m_id = id;
        m_level = level;
    }
} 

public class CloudManager : Singleton<CloudManager>
{
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

        DataPlayer defaultData = new DataPlayer(null, 1);
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
}

