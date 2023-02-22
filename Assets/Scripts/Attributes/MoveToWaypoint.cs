using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MoveToWaypoint : MonoBehaviour
{
    public enum MovementUpdateSyncMode
    {
        Graphics,
        Physics
    }

    [SerializeField] private MovementUpdateSyncMode syncMovementTo = MovementUpdateSyncMode.Graphics;
    [SerializeField] private Waypoint nextWaypoint;
    private Vector3 previousPosition;
    private float timeSinceCurrentWaypoint = 0.0f;

    void Awake()
    {
        previousPosition = transform.position;
        timeSinceCurrentWaypoint = 0.0f;
        nextWaypoint?.IncrementRefCounter();
    }

    void Update()
    {
        if (syncMovementTo == MovementUpdateSyncMode.Graphics)
        {
            StepMovement();
        }
    }

    void FixedUpdate()
    {
        if (syncMovementTo == MovementUpdateSyncMode.Physics)
        {
            StepMovement();
        }
    }

    private void StepMovement()
    {
        if (nextWaypoint != null)
        {
            UpdatePosition();
            timeSinceCurrentWaypoint += Time.deltaTime;
        }
    }

    private void UpdatePosition()
    {
        NextWaypoint.MoveObjectStep(this);
    }


    public Waypoint NextWaypoint
    {
        get { return nextWaypoint; }
        set
        {
            if (nextWaypoint != value)
            {
                nextWaypoint?.DecrementRefCounter();
                nextWaypoint = value;
                nextWaypoint?.IncrementRefCounter();
                previousPosition = transform.position;
                timeSinceCurrentWaypoint = 0.0f;
            }
        }
    }

    public float TimeSinceCurrentWaypoint
    {
        get { return timeSinceCurrentWaypoint; }
    }

    public Vector3 PreviousPosition
    {
        get { return previousPosition; }
    }
}
