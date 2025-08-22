using UnityEngine;

/// <summary>
/// Generic Singleton base class for Unity.
/// Inherit from this class to easily make a MonoBehaviour Singleton.
/// Example: public class GameManager : Singleton<GameManager> { }
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    /// <summary>
    /// Global access point to the Singleton instance.
    /// If no instance exists in the scene, it will automatically create one.
    /// </summary>
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                // Unity 2023+: Use FindFirstObjectByType instead of FindObjectOfType
                instance = FindFirstObjectByType<T>();

                // If no instance is found in the scene, create a new one
                if (instance == null)
                {
                    GameObject obj = new GameObject(typeof(T).Name);
                    instance = obj.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    /// <summary>
    /// Ensures only one instance exists.
    /// Keeps the object alive across scenes (DontDestroyOnLoad).
    /// </summary>
    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(gameObject); // Persist between scenes
        }
        else if (instance != this)
        {
            Destroy(gameObject); // Prevent duplicate Singletons
        }
    }
}
