using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GameBoard
{
    private Dictionary<Vector2Int, Block> blocksOnBoard = new Dictionary<Vector2Int, Block>();
    private Dictionary<(Vector2Int, Vector2Int), IEnumerable<Vector2Int>> possibleSwitches = new Dictionary<(Vector2Int, Vector2Int), IEnumerable<Vector2Int>>();

    [SerializeField]
    int matchingBlocksNeeded = 3;

    void RecachePossibleSwitches()
    {
        possibleSwitches.Clear();
        void TestSwitch(Vector2Int position, Vector2Int direction)
        {
            Vector2Int otherPosition = position + direction;
            IEnumerable<Vector2Int> destroyedPositions = GetBlockPositionsDestroyedBySwitch(position, otherPosition);
            if (destroyedPositions.Any())
            {
                Debug.Assert(!possibleSwitches.ContainsKey((position, otherPosition)), "Valid switch already contained in cache!");
                Debug.Assert(!possibleSwitches.ContainsKey((otherPosition, position)), "Valid switch already contained in cache!");
                possibleSwitches[(position, otherPosition)] = destroyedPositions;
            }
        }
        foreach (Vector2Int position in IterateCellPositionsRegular())
        {
            TestSwitch(position, Vector2Int.right);
            TestSwitch(position, Vector2Int.down);
        }
    }

    Block SpawnRandomBlockAt(Vector2Int gridPosition, bool canCauseDestruction = false)
    {
        int randomIndex = Random.Range(0, possibleBlockTypes.Count - 1);

        BlockType blockType = possibleBlockTypes[randomIndex];
        if (!canCauseDestruction && SpawnAtPositionCanCauseDestruction(blockType, gridPosition))
        {
            bool foundAlternativeBlockType = false;
            for (int offsetIndex = randomIndex + 1 % possibleBlockTypes.Count; offsetIndex != randomIndex; offsetIndex = (offsetIndex + 1) % possibleBlockTypes.Count)
            {
                BlockType blockTypeToTry = possibleBlockTypes[offsetIndex];
                if (!SpawnAtPositionCanCauseDestruction(blockTypeToTry, gridPosition))
                {
                    blockType = blockTypeToTry;
                    foundAlternativeBlockType = true;
                    break;
                }
            }
            if (!foundAlternativeBlockType)
            {
                Debug.LogError("Failed to spawn block that wouldn't cause destruction!");
            }
        }

        return SpawnBlockAt(gridPosition, blockType);
    }

    Block SpawnBlockAt(Vector2Int gridPosition, BlockType blockType)
    {
        if (blocksOnBoard.ContainsKey(gridPosition))
        {
            Debug.LogError("Tried to spawn a block at a grid point that already has a block!");
            return null;
        }

        Vector3 worldPosition =
            GetCellCenterWorld(gridPosition);
        worldPosition.z = blockContainer.transform.position.z;
        Block newBlock =
            Block
                .InstantiateWithBlockType(worldPosition,
                blockType,
                blockContainer ?? gameObject);

        blocksOnBoard[gridPosition] = newBlock;
        return newBlock;
    }

    bool SwitchCanCauseDestruction(Vector2Int from, Vector2Int to)
    {
        return GetBlockPositionsDestroyedBySwitch(from, to).Any();
    }

    bool SpawnAtPositionCanCauseDestruction(BlockType spawnedBlockType, Vector2Int gridPosition)
    {
        return GetBlockPositionsDestroyedBySpawn(spawnedBlockType, gridPosition).Any();
    }

    IEnumerable<Vector2Int> GetBlockPositionsDestroyedBySpawn(BlockType spawnedBlockType, Vector2Int gridPosition)
    {
        Dictionary<Vector2Int, BlockType> blockTypeOverrides = new Dictionary<Vector2Int, BlockType> { { gridPosition, spawnedBlockType } };
        return GetBlockPositionsDestroyedByBlockTypes(blockTypeOverrides, gridPosition);
    }

    IEnumerable<Vector2Int> GetBlockPositionsDestroyedBySwitch(Vector2Int from, Vector2Int to)
    {
        if (!(IsPositionFloor(from) && IsPositionFloor(to)))
        {
            return Enumerable.Empty<Vector2Int>();
        }

        Dictionary<Vector2Int, BlockType> blockTypeOverrides = new Dictionary<Vector2Int, BlockType> {
            { from, GetBlockTypeAt(to) },
            { to, GetBlockTypeAt(from)}
        };
        return GetBlockPositionsDestroyedByBlockTypes(blockTypeOverrides, to).Union(GetBlockPositionsDestroyedByBlockTypes(blockTypeOverrides, from));
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
}
