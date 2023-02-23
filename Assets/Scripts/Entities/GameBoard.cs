using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class GameBoard : MonoBehaviour
{
    [SerializeField]
    private RuleTile floorTile = null;

    [SerializeField]
    private RuleTile wallTile = null;

    [SerializeField]
    private GameObject pieceContainer = null;

    [SerializeField]
    private float totalBlockSpawnTimeInSeconds = 1.0f;

    [SerializeField]
    private float delayAfterAllBlockSpawned = 1.0f;

    [SerializeField]
    private List<BlockType> possibleBlockTypes = new List<BlockType>();

    private Tilemap cachedTilemap = null;

    private Dictionary<Vector2Int, Grabbable> piecesOnBoard;

    private bool interactionLocked = false;

    private readonly object interactionLock = new object();

    public delegate void GameBoardOperation();

    // Start is called before the first frame update
    async UniTask Start()
    {
        cachedTilemap = GetComponent<Tilemap>();
        Debug.Assert(possibleBlockTypes.Count != 0);
        Debug.Assert(floorTile != null);
        Debug.Assert(wallTile != null);
        await FillMapWithPieces();
    }

    // Update is called once per frame
    void Update()
    {
    }

    private bool IsPositionFloor(Vector2Int gridPosition)
    {
        return cachedTilemap.GetTile((Vector3Int)gridPosition) == floorTile;
    }

    private bool IsPositionWall(Vector2Int gridPosition)
    {
        return cachedTilemap.GetTile((Vector3Int)gridPosition) == wallTile;
    }

    async UniTask FillMapWithPieces()
    {
        await DoWithInteractionLock(async () =>
         {
             IEnumerable<Vector2Int> floorPositions = IterateCellPositionsRegular().Where(v => IsPositionFloor(v));

             int floorCount = floorPositions.Count();
             if (floorCount <= 0)
             {
                 return;
             }

             System.TimeSpan singleDelay =
                 System
                     .TimeSpan
                     .FromSeconds(totalBlockSpawnTimeInSeconds /
                     (float)floorCount);

             foreach (Vector2Int gridPosition in floorPositions)
             {
                 SpawnBlockAt(gridPosition,
                 possibleBlockTypes[Random
                     .Range(0, possibleBlockTypes.Count - 1)]);
                 await UniTask.Delay(singleDelay);
             }
         });
    }

    async UniTask DoWithInteractionLock(System.Func<UniTask> task)
    {
        if (!AcquireInteractionLockImmediate())
        {
            Debug.LogError("Failed to acquire interaction lock!");
            return;
        }

        try
        {
            await task();
        }
        finally
        {
            ReleaseInteractionLockImmediate();
        }
    }

    private bool AcquireInteractionLockImmediate()
    {
        lock (interactionLock)
        {
            if (!interactionLocked)
            {
                interactionLocked = true;
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private void ReleaseInteractionLockImmediate()
    {
        lock (interactionLock)
        {
            Debug
                .Assert(interactionLocked,
                "Tried to release interaction lock but it was already released!");
            interactionLocked = false;
        }
    }

    private IEnumerable<Vector2Int> IterateCellPositionsRegular()
    {
        for (
            int y = cachedTilemap.cellBounds.yMax - 1;
            cachedTilemap.cellBounds.yMin <= y;
            --y
        )
        {
            for (
                int x = cachedTilemap.cellBounds.xMin;
                x < cachedTilemap.cellBounds.xMax;
                ++x
            )
            {
                yield return new Vector2Int(x, y);
            }
        }
    }

    Block SpawnBlockAt(Vector2Int gridPosition, BlockType blockType)
    {
        Vector3 worldPosition =
            cachedTilemap.GetCellCenterWorld((Vector3Int)gridPosition);
        Block newBlock =
            Block
                .InstantiateWithBlockType(worldPosition,
                blockType,
                pieceContainer ?? gameObject);
        return newBlock;
    }
}
