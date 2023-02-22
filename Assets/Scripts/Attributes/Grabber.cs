using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public abstract class IGrabbableRepository : MonoBehaviour
{
    public abstract Grabbable GetGrabbableAtPosition(Vector3 grabPos);
    public abstract void GrabberReleasedAtPosition(Grabbable pos, Vector3 releasePos);
    public abstract void GrabberHoverAtPosition(Grabbable grabbedObject, Vector3 hoverPosition);
    public abstract Vector3 GetNearestSnapPosition(Vector3 atPos);
}

[RequireComponent(typeof(Waypoint))]
public class Grabber : MonoBehaviour
{
    [Tooltip("[REQUIRED] An object capable of giving grabbable objects to this grabber, and handling when the grabber lets go of one.")]
    [SerializeField] private IGrabbableRepository grabbableRepository = null;
    [Tooltip("If the grabbable repository has snap point, should the grabber always snap to the nearest such point?")]
    [SerializeField] private bool snapToPoints = false;
    [Tooltip("How far should the pointer be allowed to travel from the original grab point while grabbing something")]
    [SerializeField] private float maxDragDistance = Mathf.Infinity;
    [Tooltip("If set to non-zero, the grabber freeze and let go of the object after this delay when reaching the max drag distance")]
    [SerializeField] private float releaseDelayOnMaxDistanceReached = 0.0f;
    [Tooltip("List of directions the grabber is allowed to drag in. Empty list = omnidirectional drag.")]
    [SerializeField] private List<Vector2> allowedDragDirections = new List<Vector2>();

    [Header("Debug")]
    [SerializeField] private Vector3 originalGrabPosition = new Vector3();
    [SerializeField] private Grabbable grabbedObject = null;
    [SerializeField] private Waypoint pointerWaypoint = null;
    [SerializeField] private float releaseDelayLeft = 0.0f;

    void Awake()
    {
        originalGrabPosition = new Vector3();
        grabbedObject = null;
        pointerWaypoint = GetComponent<Waypoint>();
        Debug.Assert(pointerWaypoint != null);
        releaseDelayLeft = 0.0f;

        allowedDragDirections.ForEach(v => v.Normalize());
    }

    void Update()
    {
        if (0.0f < releaseDelayLeft)
        {
            releaseDelayLeft -= Time.deltaTime;
            if (releaseDelayLeft <= 0.0f)
            {
                Release();
                releaseDelayLeft = 0.0f;
            }
        }
    }

    public void OnHover(InputAction.CallbackContext context)
    {
        if (releaseDelayLeft == 0.0f)
        {
            UpdatePointerPosition(context.ReadValue<Vector2>());
            if (IsGrabbing())
            {
                grabbableRepository.GrabberHoverAtPosition(grabbedObject, PointerPosition);
            }
        }
    }

    private void UpdatePointerPosition(Vector2 pointerPositionRaw)
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(pointerPositionRaw.x, pointerPositionRaw.y, Camera.main.nearClipPlane));
        if (snapToPoints)
        {
            worldPosition = grabbableRepository.GetNearestSnapPosition(worldPosition);
        }

        if (IsGrabbing())
        {
            Vector3 originalPosition = grabbedObject.OriginalPosition;

            Vector3 dragVector = FindClosestAllowedDragVectorTo(worldPosition - originalPosition);
            if (maxDragDistance <= dragVector.magnitude)
            {
                worldPosition = originalPosition + (dragVector.normalized * maxDragDistance);
                releaseDelayLeft = releaseDelayOnMaxDistanceReached;
            }
        }
        PointerPosition = new Vector2(worldPosition.x, worldPosition.y);
    }

    private Vector2 FindClosestAllowedDragVectorTo(Vector2 vector)
    {
        if (allowedDragDirections.Count == 0)
        {
            return vector;
        }
        else
        {
            Vector2 closestVector = allowedDragDirections.Aggregate((a, b) => Vector2.Dot(vector, b) < Vector2.Dot(vector, a) ? a : b) * vector.magnitude;
            return closestVector * Vector2.Dot(vector, closestVector);
        }
    }

    public void OnGrab(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Started:
                Grab();
                break;
            case InputActionPhase.Canceled:
                Release();
                break;
        }
    }

    private void Grab()
    {
        if (!IsGrabbing())
        {
            Grabbable objectToGrab = grabbableRepository?.GetGrabbableAtPosition(PointerPosition);
            if (objectToGrab != null)
            {
                objectToGrab.GrabWith(this);
                grabbedObject = objectToGrab;
                originalGrabPosition = objectToGrab.transform.position;
            }
        }
    }

    private void Release()
    {
        if (IsGrabbing())
        {
            grabbedObject?.ReleaseGrab();
            grabbableRepository?.GrabberReleasedAtPosition(grabbedObject, transform.position);
            grabbedObject = null;
        }
    }

    public bool IsGrabbing()
    {
        return grabbedObject != null;
    }

    private Vector2 PointerPosition { get { return pointerWaypoint.transform.position; } set { pointerWaypoint.transform.position = value; } }
}
