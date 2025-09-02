using UnityEngine;
using UnityEngine.U2D;

public class ResourceManager
{
    public static SpriteAtlas m_atlas = Resources.Load<SpriteAtlas>("atlas");
    public static Sprite GetSprite(string name) => m_atlas.GetSprite(name);
}
