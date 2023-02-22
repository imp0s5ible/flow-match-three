using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeWaypoint : Waypoint
{
    [Tooltip("Animation curve describing the progress from the target's last position towards this waypoint.")]
    [SerializeField] private AnimationCurve distanceToWaypointCurve = new AnimationCurve();
    [Tooltip("The amount of time it takes for an object to reach this waypoint starting from its original position.")]
    [SerializeField] private float timeToReach = 1.0f;

    protected override void MoveObjectInternal(MoveToWaypoint target)
    {
        float targetProgress = distanceToWaypointCurve.Evaluate(target.TimeSinceCurrentWaypoint / timeToReach);
        target.transform.position = Vector3.Lerp(target.PreviousPosition, transform.position, targetProgress);
    }

    protected override bool TargetArrived(MoveToWaypoint target)
    {
        return target.NextWaypoint == this && timeToReach <= target.TimeSinceCurrentWaypoint;
    }
}
