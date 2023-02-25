using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class GameBoard
{
    [SerializeField]
    private float delayAfterAllBlockSpawned = 1.0f;
    private float delayAfterBlocksDestroyed = 1.0f;

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

    async UniTask FillMapWithPieces()
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
}