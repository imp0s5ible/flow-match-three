using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;

public partial class GameBoard
{
    [SerializeField]
    public float blockSwitchTime = 0.5f;

    public bool Grabbing
    {
        get
        {
            return grabPosition.HasValue;
        }
        private set
        {
            if (value && !InteractionLocked)
            {
                grabPosition = hoverPosition;
            }
            else
            {
                grabPosition = null;
            }
        }
    }

    public Vector2Int HoverPosition
    {
        get
        {
            return HoverPosition;
        }
        private set
        {
            hoverPosition = value;
            if (grabPosition.HasValue && hoverPosition != grabPosition && !InteractionLocked)
            {
                Vector2Int releasePosition = grabPosition.Value;
                grabPosition = null;
                Vector2Int switchDirectionVec = hoverPosition - releasePosition;
                if (1 <= Mathf.Abs(switchDirectionVec.x))
                {
                    switchDirectionVec.x = (int)Mathf.Sign(switchDirectionVec.x);
                    switchDirectionVec.y = 0;
                }
                else
                {
                    switchDirectionVec.y = (int)Mathf.Sign(switchDirectionVec.y);
                }
                SwitchBlocks(releasePosition, releasePosition + switchDirectionVec).Forget();
            }
        }
    }

    private Vector2Int? grabPosition = null;
    private Vector2Int hoverPosition = new Vector2Int();

    private GameControls gameControls;

    private void OnEnable()
    {
        gameControls = new GameControls();
        gameControls.MatchGame.Enable();

        gameControls.MatchGame.Select.performed += ControlsSetSelect;
        gameControls.MatchGame.Select.canceled += ControlsSetSelect;

        gameControls.MatchGame.Hover.performed += ControlsSetHover;
        gameControls.MatchGame.Hover.performed += ControlsSetHover;
    }

    private void OnDisable()
    {
        gameControls.MatchGame.Disable();
    }

    private void ControlsSetSelect(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            Grabbing = true;
        }
        else if (ctx.canceled)
        {
            Grabbing = false;
        }
    }

    private void ControlsSetHover(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            Vector2 rawHoverPosition = Camera.main.ScreenToWorldPoint(ctx.ReadValue<Vector2>());
            HoverPosition = WorldToCell(rawHoverPosition);
        }
    }

    private async UniTaskVoid SwitchBlocks(Vector2Int from, Vector2Int to)
    {
        Debug.Assert(from != to);

        if (!IsPositionFloor(from) || !IsPositionFloor(to))
        {
            return;
        }

        Block blockFrom = GetBlockAt(from);
        Block blockTo = GetBlockAt(to);
        if (blockFrom == null || blockTo == null)
        {
            Debug.LogWarning("Attempted to drag from " + from.ToString() + " to " + to.ToString() + " but not both of these have a block!");
            return;
        }
        using (new InteractionLock(this))
        {
            await UniTask.WhenAll(MoveObject.Linear(blockFrom.gameObject, GetCellCenterWorld(to), blockSwitchTime), MoveObject.Linear(blockTo.gameObject, GetCellCenterWorld(from), blockSwitchTime));

            List<Vector2Int> destroyPositions = GetBlockPositionsDestroyedBySwitch(from, to).ToList();
            if (destroyPositions.Any())
            {
                blocksOnBoard[from] = blockTo;
                blocksOnBoard[to] = blockFrom;
                await UniTask.WhenAll(destroyPositions.Select(p => DestroyBlockAt(p)));
                await UniTask.Delay(System.TimeSpan.FromSeconds(delayAfterBlocksDestroyed));

                await UniTask.WhenAll(destroyPositions.Select(p => p.x).Distinct().Select(x =>
                {
                    SpawnReplacementBlocksForColumn(x);
                    return FallBlocksInColumn(x);
                }).Aggregate((i, j) => i.Concat(j)));
            }
            else
            {
                await UniTask.WhenAll(MoveObject.Linear(blockFrom.gameObject, GetCellCenterWorld(from), blockSwitchTime), MoveObject.Linear(blockTo.gameObject, GetCellCenterWorld(to), blockSwitchTime));
            }
        };
    }

    private async UniTask DestroyBlockAt(Vector2Int at)
    {
        GameObject.Destroy(GetBlockAt(at).gameObject);
        blocksOnBoard.Remove(at);
        await UniTask.Yield();
    }
}
