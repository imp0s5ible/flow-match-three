using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MatchBlockConfiguration", menuName = "Match Game/MatchBlockConfiguration", order = 1)]
public class BlockType : ScriptableObject
{
    public string blockName;
    public Sprite displaySprite;
}
