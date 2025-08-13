using System.Collections.Generic;
using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    const int GRID_X_COUNT = 6;
    const int GRID_Y_COUNT = 3;

    [SerializeField] private GameObject prefPlayer;

    private List<Vector2> spawnList = new List<Vector2>();
    private List<bool> spawnedList = new List<bool>(); // 소한된 위치 정보

    void Start()
    {
        GridStart();
    }

    // Update is called once per frame
    void Update()
    {
        
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

    public void Summon()
    {
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
}
