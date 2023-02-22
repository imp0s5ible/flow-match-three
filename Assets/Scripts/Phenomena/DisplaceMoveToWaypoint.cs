using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class WaypointDisplacementHelpers
{
    public static WaypointDisplacement DisplaceToPositionPermanently(this MoveToWaypoint moveToWaypoint, Vector3 displaceToPosition, Waypoint prefab, GameObject parent = null)
    {
        return WaypointDisplacement.DisplaceObjectToPositionPermanently(moveToWaypoint, displaceToPosition, prefab, parent);
    }

    public static WaypointDisplacement DisplaceToPosition(this MoveToWaypoint moveToWaypoint, Vector3 displaceToPosition, Waypoint prefab, GameObject parent = null)
    {
        return WaypointDisplacement.DisplaceObjectToStaticPosition(moveToWaypoint, displaceToPosition, prefab, parent);
    }

    public static WaypointDisplacement DisplaceToWaypoint(this MoveToWaypoint moveToWaypoint, Waypoint displaceToWaypoint, Waypoint returnWaypoint = null, GameObject parent = null)
    {
        return WaypointDisplacement.DisplaceObjectToWaypoint(moveToWaypoint, displaceToWaypoint, returnWaypoint, parent);
    }
}

public class WaypointDisplacement : MonoBehaviour
{
    [System.Serializable]
    private enum Type
    {
        DisplaceToExistingWaypoint,
        DisplaceToPosition,
        PermanentDisplaceToPosition
    }

    [SerializeField] private MoveToWaypoint displacedFollower = null;
    [SerializeField] private Vector3 returnToPosition = new Vector3();
    [SerializeField] private Waypoint displacementWaypoint = null;
    [SerializeField] private Waypoint originalWaypoint = null;
    [SerializeField] private UnityEvent<Vector3> onDisplacementReleased = new UnityEvent<Vector3>();
    [SerializeField] private UnityEvent<Vector3> onDisplacementReturned = new UnityEvent<Vector3>();
    [SerializeField] private Type displacementType = Type.DisplaceToExistingWaypoint;
    [SerializeField] private bool active = true;

    public UnityEvent<Vector3> OnDisplacementReleased { get => onDisplacementReleased; }
    public Waypoint DisplacementWaypoint { get => DisplacementWaypoint; }
    public Vector3 OriginalPosition { get => originalWaypoint ? originalWaypoint.transform.position : returnToPosition; }

    public static WaypointDisplacement DisplaceObjectToPositionPermanently(MoveToWaypoint moveToWaypoint, Vector3 displaceToPosition, Waypoint prefab, GameObject parent = null)
    {
        Waypoint displacementWaypoint = CreateOwnWaypoint(prefab, displaceToPosition, parent);
        WaypointDisplacement displaceMoveToWaypoint = DisplaceObjectToWaypointInternal(moveToWaypoint, displacementWaypoint, parent, null, Type.PermanentDisplaceToPosition);
        displacementWaypoint.onObjectPass.AddListener(delegate (GameObject gameObject)
        {
            if (gameObject == moveToWaypoint)
            {
                displaceMoveToWaypoint.Release();
            }
        });
        return displaceMoveToWaypoint;
    }

    public static WaypointDisplacement DisplaceObjectToWaypoint(MoveToWaypoint moveToWaypoint, Waypoint displaceToWaypoint, Waypoint returnWaypoint, GameObject parent = null)
    {
        return DisplaceObjectToWaypointInternal(moveToWaypoint, displaceToWaypoint, parent, returnWaypoint, Type.DisplaceToExistingWaypoint);
    }

    public static WaypointDisplacement DisplaceObjectToStaticPosition(MoveToWaypoint moveToWaypoint, Vector3 displaceToPosition, Waypoint prefab, GameObject parent = null)
    {
        return DisplaceObjectToWaypointInternal(moveToWaypoint, CreateOwnWaypoint(prefab, displaceToPosition, parent), parent, null, Type.DisplaceToPosition);
    }

    private static Waypoint CreateOwnWaypoint(Waypoint prefab, Vector3 position, GameObject parent = null)
    {
        Waypoint displacementWaypoint = GameObject.Instantiate<Waypoint>(prefab, position, Quaternion.identity);
        displacementWaypoint.nextWaypoint = displacementWaypoint;
        if (parent != null)
        {
            displacementWaypoint.transform.parent = parent.transform;
        }
        return displacementWaypoint;
    }

    private static WaypointDisplacement DisplaceObjectToWaypointInternal(MoveToWaypoint moveToWaypoint, Waypoint displaceToWaypoint, GameObject parent, Waypoint returnWaypoint, Type displacementType)
    {
        WaypointDisplacement blockDisplacement = new GameObject().AddComponent<WaypointDisplacement>();
        string displacementName = "Waypoint Displacement";
        switch (displacementType)
        {
            case Type.DisplaceToExistingWaypoint:
                displacementName = "Displace " + moveToWaypoint.gameObject + " to " + displaceToWaypoint.gameObject;
                break;
            case Type.DisplaceToPosition:
                displacementName = "Displace " + moveToWaypoint.gameObject + " to " + displaceToWaypoint.transform.position;
                break;
            case Type.PermanentDisplaceToPosition:
                displacementName = "Displace " + moveToWaypoint.gameObject + " to " + displaceToWaypoint.transform.position + " permanently";
                break;
        }
        blockDisplacement.gameObject.name = displacementName;

        blockDisplacement.returnToPosition = moveToWaypoint.transform.position;
        if (returnWaypoint != null)
        {
            blockDisplacement.originalWaypoint = returnWaypoint;
        }
        else if (moveToWaypoint != null)
        {
            blockDisplacement.originalWaypoint = moveToWaypoint.NextWaypoint;
        }
        else
        {
            Debug.LogWarning("Object " + moveToWaypoint + " has no return waypoint!");
        }
        blockDisplacement.displacedFollower = moveToWaypoint;
        blockDisplacement.displacementWaypoint = displaceToWaypoint;
        blockDisplacement.displacementType = displacementType;

        if (parent != null)
        {
            blockDisplacement.transform.parent = parent.transform;
        }
        return blockDisplacement;
    }

    private void Start()
    {
        displacedFollower.NextWaypoint = displacementWaypoint;
    }

    public void Release()
    {
        if (!active) {
            return;
        }

        if (originalWaypoint != null)
        {
            displacedFollower.NextWaypoint = originalWaypoint;
            if (displacementType == Type.DisplaceToPosition || displacementType == Type.PermanentDisplaceToPosition)
            {
                GameObject.Destroy(displacementWaypoint);
            }
            onDisplacementReleased.Invoke(displacedFollower.transform.position);
        }
        else if (displacementWaypoint != null && (displacementType == Type.DisplaceToPosition || displacementType == Type.PermanentDisplaceToPosition))
        {
            SetupAsReturnWaypoint(displacementWaypoint);
        }
        else if (displacementType != Type.PermanentDisplaceToPosition)
        {
            Waypoint newReturnWaypoint = GameObject.Instantiate<Waypoint>(displacementWaypoint, returnToPosition, Quaternion.identity);
            SetupAsReturnWaypoint(newReturnWaypoint);
        }

        GameObject.Destroy(gameObject);
        active = false;
    }

    private void SetupAsReturnWaypoint(Waypoint waypoint)
    {
        waypoint.transform.position = returnToPosition;
        waypoint.onObjectPass.AddListener(delegate (GameObject gameObject)
        {
            if (gameObject == displacedFollower.gameObject)
            {
                onDisplacementReleased.Invoke(displacedFollower.transform.position);
                GameObject.Destroy(waypoint.gameObject);
            }
        });
    }
}
