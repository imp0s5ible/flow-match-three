using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public partial class GameBoard : MonoBehaviour
{
    [SerializeField]
    private RuleTile floorTile = null;

    [SerializeField]
    private RuleTile wallTile = null;

    [SerializeField]
    private GameObject blockContainer = null;

    [SerializeField]
    private float totalBlockSpawnTimeInSeconds = 1.0f;
    [SerializeField]
    private List<BlockType> possibleBlockTypes = new List<BlockType>();

    private Tilemap cachedTilemap = null;



    async UniTask Start()
    {
        cachedTilemap = GetComponent<Tilemap>();
        Debug.Assert(possibleBlockTypes.Count != 0);
        Debug.Assert(floorTile != null);
        Debug.Assert(wallTile != null);
        await FillMapWithBlocks();
        RecachePossibleSwitches();
    }

    void Update()
    {
        foreach ((Vector2Int from, Vector2Int to) in possibleSwitches.Keys)
        {
            Debug.DrawLine(GetCellCenterWorld(from), GetCellCenterWorld(to), Color.green);
        }
    }

    private Block GetBlockAt(Vector2Int gridPosition)
    {
        return blocksOnBoard.GetValueOrDefault(gridPosition, null);
    }

    private BlockType GetBlockTypeAt(Vector2Int gridPosition)
    {
        return GetBlockAt(gridPosition)?.BlockType;
    }

    private Vector3 GetCellCenterWorld(Vector2Int gridPosition)
    {
        return blockContainer.transform.position + cachedTilemap.GetCellCenterWorld((Vector3Int)gridPosition);
    }

    private Vector3 GetCellCenterLocal(Vector2Int gridPosition)
    {
        return cachedTilemap.GetCellCenterLocal((Vector3Int)gridPosition);
    }

    private Vector2Int WorldToCell(Vector3 worldPos)
    {
        return (Vector2Int)cachedTilemap.WorldToCell(worldPos);
    }

    private Vector2Int LocalToCell(Vector3 localPos)
    {
        return (Vector2Int)cachedTilemap.LocalToCell(localPos);
    }

    private Vector2Int? GetLowestSpawnPositionForColumn(int x)
    {
        for (Vector2Int currentPosition = new Vector2Int(x, cachedTilemap.cellBounds.yMax);
             cachedTilemap.cellBounds.yMin <= currentPosition.y;
             currentPosition += Vector2Int.down)
        {
            if (IsPositionWall(currentPosition) && IsPositionFloor(currentPosition + Vector2Int.down))
            {
                return currentPosition;
            }
        }
        return null;
    }

    private int CountEmptyCellsInColumn(int x)
    {
        int sum = 0;
        for (Vector2Int currentPosition = new Vector2Int(x, cachedTilemap.cellBounds.yMin);
             currentPosition.y <= cachedTilemap.cellBounds.yMax;
             currentPosition += Vector2Int.up)
        {
            if (IsPositionFloor(currentPosition) && GetBlockAt(currentPosition) == null)
            {
                ++sum;
            }
        }
        return sum;
    }

    private bool IsPositionFloor(Vector2Int gridPosition)
    {
        return cachedTilemap.GetTile((Vector3Int)gridPosition) == floorTile;
    }

    private bool IsPositionWall(Vector2Int gridPosition)
    {
        return cachedTilemap.GetTile((Vector3Int)gridPosition) == wallTile;
    }


}
