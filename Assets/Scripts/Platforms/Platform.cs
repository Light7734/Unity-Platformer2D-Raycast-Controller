using System.Collections.Generic;
using UnityEngine;

public class Platform : MonoBehaviour
/* 
 * TODO:
 *  Go through
 *  Cyclic
 *  Smoothing
 *  Hold/Release
 *  Timed way points
 */
{
    [SerializeField] private PlatformController controller;

    [SerializeField] private List<Vector2> localWayPoints = new List<Vector2>();
    [SerializeField] private float speed;

    private List<Vector2> globalWayPoints = new List<Vector2>();
    private int fromWayPoint = 0, toWayPoint = 1;

    #region UnityEvents

    private void Awake()
    {
        globalWayPoints.Add((Vector2)transform.position);

        foreach(Vector2 wayPoint in localWayPoints)
            globalWayPoints.Add((Vector2)transform.position + wayPoint);
    }

    private void FixedUpdate()
    {
        Vector2 velocity =  Vector3.MoveTowards(transform.position, globalWayPoints[toWayPoint], speed * Time.fixedDeltaTime) - transform.position;
        controller.Move(velocity, ForceDir.Self);

        if (transform.position == (Vector3)globalWayPoints[toWayPoint])
            toWayPoint = (toWayPoint + 1) % globalWayPoints.Count;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        if (Application.isPlaying)
            for (int i = 0; i < globalWayPoints.Count; i++)
            {

                {
                    Gizmos.DrawLine(globalWayPoints[i] + (Vector2.up * .5f), globalWayPoints[i] - (Vector2.up * .5f));
                    Gizmos.DrawLine(globalWayPoints[i] + (Vector2.right * .5f), globalWayPoints[i] - (Vector2.right * .5f));
                }
            }
        else
            for (int i = 0; i < localWayPoints.Count; i++)
            {
                {
                    Gizmos.DrawLine(((Vector2)transform.position + localWayPoints[i]) + Vector2.up * (.5f), ((Vector2)transform.position + localWayPoints[i]) - Vector2.up * (.5f));
                    Gizmos.DrawLine(((Vector2)transform.position + localWayPoints[i]) + Vector2.right * (.5f), ((Vector2)transform.position + localWayPoints[i]) - Vector2.right * (.5f));
                }
            }
    }

    #endregion // __UNITY_EVENTS__

}