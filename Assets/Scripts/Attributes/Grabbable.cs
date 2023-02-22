using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using static WaypointDisplacementHelpers;

[RequireComponent(typeof(MoveToWaypoint))]
public class Grabbable : MonoBehaviour
{
    [Tooltip("Waypoint this grabbable will return to once released\nA new copy of this will be created if Clone Return Waypoint is checked.\nThe Waypoint object will snap to the spawn position of this object if Snap Return Waypoint To Object On Spawn is checked.")]
    [SerializeField] Waypoint returnWaypoint = null;
    [Tooltip("Checking this will create a copy of the return waypoint for exclusive use by this object, leaving the original alone.")]
    [SerializeField] bool cloneReturnWaypoint = false;
    [Tooltip("Checking this will move the return waypoint to the position of the object upon spawn.")]
    [SerializeField] bool snapReturnWaypointToObjectOnSpawn = false;
    [Tooltip("This event is invoked when the object gets grabbed")]
    [SerializeField] public UnityEvent<Vector3> onGrabbed = new UnityEvent<Vector3>();
    [Tooltip("This event is invoked when the object gets released")]
    [SerializeField] public UnityEvent<Vector3> onReleased = new UnityEvent<Vector3>();
    [Tooltip("If the object has a return waypoint, this event is called if and when the object reaches the return waypoint")]
    [SerializeField] public UnityEvent<Vector3> onReturn = new UnityEvent<Vector3>();

    [Header("Diagnostic")]
    [SerializeField] public WaypointDisplacement displacement = null;

    private MoveToWaypoint cachedMoveToWaypoint = null;

    private Animator cachedAnimator = null;

    public Vector3 OriginalPosition => IsGrabbed ? displacement.OriginalPosition : transform.position;

    public void GrabWith(Grabber grabber)
    {
        if (!IsGrabbed)
        {
            displacement = cachedMoveToWaypoint.DisplaceToWaypoint(grabber.GetComponent<Waypoint>(), returnWaypoint, gameObject.transform.parent.gameObject);
            onGrabbed.Invoke(grabber.transform.position);
            cachedAnimator?.SetBool("grabbed", true);
        }
    }

    public void ReleaseGrab()
    {
        if (IsGrabbed)
        {
            displacement.Release();
            onReleased.Invoke(cachedMoveToWaypoint.NextWaypoint.transform.position);
            cachedAnimator?.SetBool("grabbed", false);
        }
    }

    public bool IsGrabbed => displacement != null;

    void Awake()
    {
        cachedMoveToWaypoint = GetComponent<MoveToWaypoint>();
        cachedAnimator = GetComponentInChildren<Animator>();
        displacement = null;
        Debug.Assert(cachedMoveToWaypoint != null);
        InitReturnWaypoint();
    }

    private void InitReturnWaypoint()
    {
        if (returnWaypoint != null)
        {
            if (cloneReturnWaypoint)
            {
                returnWaypoint = GameObject.Instantiate<Waypoint>(returnWaypoint,
                                                                  snapReturnWaypointToObjectOnSpawn ? transform.position : returnWaypoint.transform.position,
                                                                  Quaternion.identity);
                returnWaypoint.transform.parent = transform.parent;
            }
            else if (snapReturnWaypointToObjectOnSpawn)
            {
                returnWaypoint.transform.position = transform.position;
            }

            returnWaypoint.onObjectPass.AddListener(delegate (GameObject passedObj)
            {
                Grabbable grabbable = passedObj.GetComponent<Grabbable>();
                if (grabbable != null && grabbable == this)
                {
                    grabbable.onReturn.Invoke(grabbable.transform.position);
                }
            });
            cachedMoveToWaypoint.NextWaypoint = returnWaypoint;
        }
    }
}
