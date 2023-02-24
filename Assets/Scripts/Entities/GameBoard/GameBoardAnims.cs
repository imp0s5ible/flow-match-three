using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class GameBoard
{
    [SerializeField]
    private float delayAfterAllBlockSpawned = 1.0f;

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
                 SpawnRandomBlockAt(gridPosition);
                 await UniTask.Delay(singleDelay);
             }

             await UniTask.Delay(System.TimeSpan.FromSeconds(delayAfterAllBlockSpawned));
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
}