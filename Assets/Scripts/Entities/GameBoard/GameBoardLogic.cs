using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GameBoard
{
    private Dictionary<Vector2Int, Block> piecesOnBoard = new Dictionary<Vector2Int, Block>();

    [SerializeField]
    int matchingBlocksNeeded = 3;

    bool CanCauseDestructionAtPosition(Dictionary<Vector2Int, BlockType> blockTypeOverrides, Vector2Int gridPosition)
    {
        return GetBlockPositionsDestroyedByBlockTypes(blockTypeOverrides, gridPosition).Any();
    }

    IEnumerable<Vector2Int> GetUniqueBlockPositionsDestroyedByBlockTypes(Dictionary<Vector2Int, BlockType> blockTypeOverrides, Vector2Int gridPosition)
    {
        return GetBlockPositionsDestroyedByBlockTypes(blockTypeOverrides, gridPosition).Distinct();
    }

    IEnumerable<Vector2Int> GetBlockPositionsDestroyedByBlockTypes(Dictionary<Vector2Int, BlockType> blockTypeOverrides, Vector2Int gridPosition)
    {
        BlockType GetBlockTypeWithOverrides(Vector2Int t)
        {
            if (blockTypeOverrides.ContainsKey(t))
            {
                return blockTypeOverrides[t];
            }
            else
            {
                return GetBlockTypeAt(t);
            }
        }

        foreach (IEnumerable<Vector2Int> range in DestructibleRanges(gridPosition))
        {

            if (range.All(t => GetBlockTypeWithOverrides(t) == GetBlockTypeWithOverrides(gridPosition)))
            {
                foreach (Vector2Int pos in range)
                {
                    yield return pos;
                }
            }
        }
    }


    IEnumerable<IEnumerable<Vector2Int>> DestructibleRanges(Vector2Int origin)
    {
        for (int beginX = origin.x - matchingBlocksNeeded + 1; beginX <= origin.x; ++beginX)
        {
            IEnumerable<Vector2Int> candidateRange = HorizontalRange(new Vector2Int(beginX, origin.y));
            if (candidateRange.All(v => IsPositionFloor(v)))
            {
                yield return candidateRange;
            }
        }

        for (int beginY = origin.y - matchingBlocksNeeded + 1; beginY <= origin.y; ++beginY)
        {
            IEnumerable<Vector2Int> candidateRange = VerticalRange(new Vector2Int(origin.x, beginY));
            if (candidateRange.All(v => IsPositionFloor(v)))
            {
                yield return candidateRange;
            }
        }
    }

    IEnumerable<Vector2Int> HorizontalRange(Vector2Int leftBegin)
    {
        for (int x = leftBegin.x; x < leftBegin.x + matchingBlocksNeeded; ++x)
        {
            yield return new Vector2Int(x, leftBegin.y);
        }
    }

    IEnumerable<Vector2Int> VerticalRange(Vector2Int bottomBegin)
    {
        for (int y = bottomBegin.y; y < bottomBegin.y + matchingBlocksNeeded; ++y)
        {
            yield return new Vector2Int(bottomBegin.x, y);
        }
    }

    Block SpawnRandomBlockAt(Vector2Int gridPosition, bool canCauseDestruction = false)
    {
        int randomIndex = Random.Range(0, possibleBlockTypes.Count - 1);
        BlockType blockType = possibleBlockTypes[randomIndex];
        Block spawnedBlock = SpawnBlockAt(gridPosition, blockType);
        Dictionary<Vector2Int, BlockType> blockTypeOverrides = new Dictionary<Vector2Int, BlockType> { { gridPosition, spawnedBlock.BlockType } };
        if (!canCauseDestruction && CanCauseDestructionAtPosition(blockTypeOverrides, gridPosition))
        {
            bool foundAlternativeBlockType = false;
            for (int offsetIndex = randomIndex + 1 % possibleBlockTypes.Count; offsetIndex != randomIndex; offsetIndex = (offsetIndex + 1) % possibleBlockTypes.Count)
            {
                BlockType blockTypeToTry = possibleBlockTypes[offsetIndex];
                Dictionary<Vector2Int, BlockType> blockTypeOverridesToTry = new Dictionary<Vector2Int, BlockType> { { gridPosition, blockTypeToTry } };
                if (!CanCauseDestructionAtPosition(blockTypeOverridesToTry, gridPosition))
                {
                    spawnedBlock.BlockType = blockTypeToTry;
                    foundAlternativeBlockType = true;
                    break;
                }
            }
            if (!foundAlternativeBlockType)
            {
                Debug.LogError("Failed to spawn block that wouldn't cause destruction!");
            }
        }
        return spawnedBlock;
    }

    Block SpawnBlockAt(Vector2Int gridPosition, BlockType blockType)
    {
        if (piecesOnBoard.ContainsKey(gridPosition))
        {
            Debug.LogError("Tried to spawn a block at a grid point that already has a block!");
            return null;
        }

        Vector3 worldPosition =
            cachedTilemap.GetCellCenterWorld((Vector3Int)gridPosition);
        Block newBlock =
            Block
                .InstantiateWithBlockType(worldPosition,
                blockType,
                pieceContainer ?? gameObject);

        piecesOnBoard[gridPosition] = newBlock;
        return newBlock;
    }
}
