using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Grabbable))]
public class Block : MonoBehaviour
{
    public BlockType BlockType
    {
        get
        {
            return blockType;
        }
        set
        {
            blockType = value;
            SetupPropertiesFromBlockType();
        }
    }

    [SerializeField]
    private BlockType blockType;

    private SpriteRenderer cachedSpriteRenderer = null;

    void Start()
    {
        Debug.Assert(blockType != null);
        SetupPropertiesFromBlockType();
    }

    private void SetupPropertiesFromBlockType()
    {
        cachedSpriteRenderer.sprite = blockType.displaySprite;
    }

    public static Block InstantiateWithBlockType(Vector3 worldPosition, BlockType blockType, GameObject parent = null)
    {
        GameObject result = new GameObject(blockType.blockName + " Block", typeof(MoveToWaypoint), typeof(Grabbable));
        result.transform.position = worldPosition;
        if (parent != null)
        {
            result.transform.parent = parent.transform;
        }

        SpriteRenderer resultSpriteRenderer = result.AddComponent<SpriteRenderer>();
        resultSpriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        Block resultBlock = result.AddComponent<Block>();
        resultBlock.cachedSpriteRenderer = resultSpriteRenderer;
        resultBlock.BlockType = blockType;

        result.SetActive(true);

        return resultBlock;
    }
}
