using Unity.Netcode;

/// <summary>
/// Example of using Singleton by inheritance.
/// Useful when the class needs both MonoBehaviour functionality and Singleton behavior.
/// </summary>
public class AAA : Singleton<AAA>
{
    /// <summary>
    /// Calls the base Awake() to initialize the Singleton instance.
    /// Add AAA-specific initialization logic here if needed.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        // TODO: Add custom initialization for AAA
    }
}

/// <summary>
/// Example of using Singleton with composition.
/// Use this when the class already inherits from another base class (e.g., NetworkBehaviour).
/// </summary>
public class BBB : NetworkBehaviour
{
    /// <summary>
    /// Provides global access to the Singleton instance.
    /// </summary>
    public static BBB Instance => Singleton<BBB>.Instance;

    // TODO: Add BBB-specific logic here
}
