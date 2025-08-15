using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    private const int GRID_X_COUNT = 6;
    private const int GRID_Y_COUNT = 3;

    [Header("Variables")]
    [SerializeField] private float MONSTER_SPAWN_INTERVAL = 1.0f;

    [SerializeField] private GameObject prefPlayer;
    [SerializeField] private Monster prefMonster;

    public static List<Vector2> monsterMoveList = new List<Vector2>();

    private List<Vector2> spawnList = new List<Vector2>();
    private List<bool> spawnedList = new List<bool>(); // 소한된 위치 정보

    void Start()
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            monsterMoveList.Add(transform.GetChild(i).position);
        }

        GridStart();
        StartCoroutine(CSpawnMonster());
    }

    #region Make Grid
    private void GridStart()
    {
        var parentSprite = GetComponent<SpriteRenderer>();
        float parentSpriteWidth = parentSprite.bounds.size.x;
        float parentSpriteHeight = parentSprite.bounds.size.y;

        float gridWidth = transform.localScale.x / GRID_X_COUNT;
        float gridHeight = transform.localScale.y / GRID_Y_COUNT;

        for (int row = 0; row < GRID_Y_COUNT; row++)
        {
            for (int col = 0; col < GRID_X_COUNT; col++)
            {
                float posX = (-parentSpriteWidth / 2) + (col * gridWidth) + (gridWidth / 2);
                float posY = (parentSpriteHeight / 2) - (row * gridHeight) + (gridHeight / 2);

                spawnList.Add(new Vector2(posX, posY + transform.position.y - gridHeight));
                spawnedList.Add(false);
            }
        }
    }
    #endregion

    #region SummonCharacter
    public void Summon()
    {
        if (GameManager.instance.Money < GameManager.instance.SummonCount)
            return;

        GameManager.instance.Money -= GameManager.instance.SummonCount;
        GameManager.instance.SummonCount += 2;

        int positionValue = -1;

        for (int i =0 ; i < spawnedList.Count; i++)
        {
            if (spawnedList[i] == false)
            {
                positionValue = i;
                spawnedList[i] = true;
                break;
            }
        }

        if(positionValue != -1)
        {
            var player = Instantiate(prefPlayer);
            
            var newPos = spawnList[positionValue];
            player.transform.position = newPos;
        }
    }
    #endregion

    #region SummonMonster
    IEnumerator CSpawnMonster()
    {
        var monster = Instantiate(prefMonster, monsterMoveList[0], Quaternion.identity);
        GameManager.instance.AddMonster(monster);
        if(monster == null)
            yield break;

        yield return new WaitForSeconds(MONSTER_SPAWN_INTERVAL);
        StartCoroutine(CSpawnMonster());
    }
    #endregion
}
