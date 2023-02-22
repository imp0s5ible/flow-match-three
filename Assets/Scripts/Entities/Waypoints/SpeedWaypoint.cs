using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedWaypoint : Waypoint
{
    [Tooltip("How the speed of the approaching object changes as it gets farther from this waypoint.\nVertical axis is units/second.\nHorizontal axis is distance in units.")]
    [SerializeField] public AnimationCurve distanceVsSpeed = AnimationCurve.EaseInOut(0.0f, 1.0f, 10.0f, 100.0f);
    [Tooltip("Radius within which an object is considered to have arrived at this waypoint.")]
    [SerializeField] public float visitDistance = 0.01f;

    protected override void MoveObjectInternal(MoveToWaypoint target)
    {
        Vector3 towardsTarget = transform.position - target.transform.position;
        Vector3 desiredMovement = towardsTarget.normalized * distanceVsSpeed.Evaluate(towardsTarget.magnitude) * Time.deltaTime;
        if (towardsTarget.magnitude < desiredMovement.magnitude)
        {
            desiredMovement = towardsTarget;
        }
        Debug.DrawLine(transform.position, transform.position + desiredMovement, Color.green);
        target.transform.Translate(desiredMovement);
    }

    protected override bool TargetArrived(MoveToWaypoint target)
    {
        Vector3 towardsTarget = transform.position - target.transform.position;
        return towardsTarget.magnitude <= visitDistance;
    }
}
