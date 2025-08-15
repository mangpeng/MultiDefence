using Unity.Netcode;
using UnityEngine;

public partial class NetManager: MonoBehaviour
{
   private void OnPlayerJoined()
    {
        if(NetworkManager.Singleton.ConnectedClients.Count >= maxPlayers)
        {
            ChangeSceneForAllPlayers();
        }
    }

    private void ChangeSceneForAllPlayers()
    {
        if(NetworkManager.Singleton.IsHost)
        {
            Debug.Log("I am " + NetworkManager.Singleton.LocalClientId);
            NetworkManager.Singleton.SceneManager.LoadScene(gamePlaySceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}
