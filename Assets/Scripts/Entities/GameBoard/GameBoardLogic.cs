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

    IEnumerable<Vector2Int> GetBlockPositionsDestroyedByBlockTypes(Dictionary<Vector2Int, BlockType> blockTypeOverrides, Vector2Int gridPosition)
    {

        IEnumerable<Vector2Int> horizontalDestructionRange = DirectionalDestructionRange(Vector2Int.right);
        horizontalDestructionRange = matchingBlocksNeeded <= horizontalDestructionRange.Count() ? horizontalDestructionRange : Enumerable.Empty<Vector2Int>();

        IEnumerable<Vector2Int> verticalDestructionRange = DirectionalDestructionRange(Vector2Int.up);
        verticalDestructionRange = matchingBlocksNeeded <= verticalDestructionRange.Count() ? verticalDestructionRange : Enumerable.Empty<Vector2Int>();

        return horizontalDestructionRange.Concat(verticalDestructionRange).Distinct();

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

        IEnumerable<Vector2Int> DirectionalDestructionRange(Vector2Int direction)
        {
            yield return gridPosition;
            for (Vector2Int t = gridPosition + direction; IsPositionFloor(t) && GetBlockTypeWithOverrides(t) == GetBlockTypeWithOverrides(gridPosition); t += direction)
            {
                yield return t;
            }
            for (Vector2Int t = gridPosition - direction; IsPositionFloor(t) && GetBlockTypeWithOverrides(t) == GetBlockTypeWithOverrides(gridPosition); t -= direction)
            {
                yield return t;
            }
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
