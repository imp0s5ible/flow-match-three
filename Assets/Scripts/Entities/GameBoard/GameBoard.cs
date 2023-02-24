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
    private GameObject pieceContainer = null;

    [SerializeField]
    private float totalBlockSpawnTimeInSeconds = 1.0f;
    [SerializeField]
    private List<BlockType> possibleBlockTypes = new List<BlockType>();

    private Tilemap cachedTilemap = null;


    private bool interactionLocked = false;

    private readonly object interactionLock = new object();

    public delegate void GameBoardOperation();

    async UniTask Start()
    {
        cachedTilemap = GetComponent<Tilemap>();
        Debug.Assert(possibleBlockTypes.Count != 0);
        Debug.Assert(floorTile != null);
        Debug.Assert(wallTile != null);
        await FillMapWithPieces();
        RecachePossibleSwitches();
    }

    void Update()
    {
        foreach ((Vector2Int from, Vector2Int to) in possibleSwitches.Keys)
        {
            Debug.DrawLine(cachedTilemap.GetCellCenterWorld((Vector3Int)from), cachedTilemap.GetCellCenterWorld((Vector3Int)to), Color.green);
        }
    }

    private Block GetBlockAt(Vector2Int gridPosition)
    {
        return piecesOnBoard.GetValueOrDefault(gridPosition, null);
    }

    private BlockType GetBlockTypeAt(Vector2Int gridPosition)
    {
        return GetBlockAt(gridPosition)?.BlockType;
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
