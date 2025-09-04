using UnityEngine;

[CreateAssetMenu(fileName = "Setting", menuName = "Scriptable Objects/Setting")]
public class Setting : ScriptableObject
{
    [Header("»Ì±â È®·ü")]
    [Range(0.0f, 100.0f)]
    public float[] m_rarity_percent = new float[(int)Rarity.Lengendary + 1];
}
