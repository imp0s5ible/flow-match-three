using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class Waypoint : MonoBehaviour
{
    [Tooltip("(Optional) The next waypoint in this chain. None means the object will stop moving here.\nTip #1: You can set this to the first waypoint in the chain to create a loop.\nTip #2: You can set this to itself to make the object constantly move towards this waypoint.")]
    [SerializeField] public Waypoint nextWaypoint = null;
    [Tooltip("Event fired when an object visits this waypoint")]
    [SerializeField] public UnityEvent<GameObject> onObjectPass = new UnityEvent<GameObject>();
    [Tooltip("If checked, this waypoint will delete itself when the last object stops following it.")]
    [SerializeField] public bool garbageCollected = false;
    [Tooltip("If garbage collected, this is how many frames this waypoint is allowed to exist without a parent before getting garbage collected")]
    [SerializeField] private int framesWithoutParent = 3;

    [SerializeField] private int refCount = 0;
    private int framesWithoutParentLeft = 3;

    public void FixedUpdate()
    {
        if (garbageCollected && refCount <= 0)
        {
            if (framesWithoutParentLeft <= 0)
            {
                GameObject.Destroy(gameObject);
            }
            else
            {
                --framesWithoutParentLeft;
            }
        }
        else
        {
            framesWithoutParentLeft = framesWithoutParent;
        }
    }
    public void MoveObjectStep(MoveToWaypoint target)
    {
        MoveObjectInternal(target);
        if (TargetArrived(target))
        {
            onObjectPass.Invoke(target.gameObject);
            target.NextWaypoint = nextWaypoint;
        }
    }

    public void IncrementRefCounter()
    {
        ++refCount;
    }

    public void DecrementRefCounter()
    {
        --refCount;
        if (garbageCollected && refCount <= 0)
        {
            GameObject.Destroy(gameObject);
        }
    }

    protected abstract bool TargetArrived(MoveToWaypoint target);

    protected abstract void MoveObjectInternal(MoveToWaypoint target);
}
