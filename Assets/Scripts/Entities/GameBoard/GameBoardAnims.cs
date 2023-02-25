using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class GameBoard
{
    [SerializeField]
    private float delayAfterAllBlockSpawned = 1.0f;
    [SerializeField]
    private float delayAfterBlocksDestroyed = 1.0f;
    [SerializeField]
    private AnimationCurve blockFallCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
    [SerializeField]
    private float blockFallTime = 1.0f;
    [SerializeField]
    private int fallThroughWalls = 1;

    private bool interactionLocked = false;

    private readonly object interactionLock = new object();

    private bool InteractionLocked
    {
        get
        {
            lock (interactionLock)
            {
                return interactionLocked;
            }
        }
        set
        {
            lock (interactionLock)
            {
                interactionLocked = value;
            }
        }
    }

    private class InteractionLock : System.IDisposable
    {
        private GameBoard boardToLock;
        public InteractionLock(GameBoard boardToLock)
        {
            this.boardToLock = boardToLock;
            lock (boardToLock.interactionLock)
            {
                Debug.Assert(!boardToLock.interactionLocked);
                boardToLock.interactionLocked = true;
            }
        }

        void System.IDisposable.Dispose()
        {
            lock (boardToLock.interactionLock)
            {
                Debug.Assert(boardToLock.interactionLocked);
                boardToLock.interactionLocked = false;
            }
        }
    }

    async UniTask FillMapWithBlocks()
    {
        using (new InteractionLock(this))
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
                SpawnRandomBlockAt(gridPosition);
                await UniTask.Delay(singleDelay);
            }

            await UniTask.Delay(System.TimeSpan.FromSeconds(delayAfterAllBlockSpawned));
        }
    }

    private IEnumerable<UniTask> FallBlocksInColumn(int x)
    {
        List<(Block block, Vector2Int to)> fallInstructions = new List<(Block, Vector2Int)>();
        for (int y = cachedTilemap.cellBounds.yMin; y <= cachedTilemap.cellBounds.yMax; ++y)
        {
            Vector2Int currentPosition = new Vector2Int(x, y);
            Block fallingBlock = GetBlockAt(currentPosition);
            if (fallingBlock != null)
            {
                Vector2Int fallPosition = GetFallPositionForBlockAt(currentPosition);

                blocksOnBoard.Remove(currentPosition);
                blocksOnBoard.Add(fallPosition, fallingBlock);

                fallInstructions.Add((fallingBlock, fallPosition));
            }
        }

        foreach (var fallInstruction in fallInstructions)
        {
            yield return MoveObject.WithCurve(fallInstruction.block.gameObject, GetCellCenterWorld(fallInstruction.to), blockFallCurve, blockFallTime);
        }
    }
}