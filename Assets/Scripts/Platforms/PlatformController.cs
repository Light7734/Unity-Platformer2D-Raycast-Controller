using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : ControllerBase
{
    [SerializeField] private LayerMask mask;
    private List<PassengerMovement> passengers = new List<PassengerMovement>();

    #region UnityEvents

    protected override void Start()
    {
        base.Start();
    }

    #endregion // __UNITY_EVENTS__


    #region MOVEMENT

    public override void Move(Vector2 velocity, ForceDir forceDir)
    {
        UpdateRaycastOrigins();

        CalculatePassengerMovement(velocity);

        MovePassengers(true);
        transform.Translate(velocity);
        MovePassengers(false);
    }

    private void MovePassengers(bool beforeMovement)
    {
        foreach(PassengerMovement passenger in passengers)
        {
            if (passenger.moveBefore == beforeMovement)
                passenger.transform.GetComponent<ControllerBase>().Move(passenger.velocity, passenger.dir);
        }
    }

    private void CalculatePassengerMovement(Vector2 velocity)
    {
        passengers.Clear();
        HashSet<Transform> movedPassengers = new HashSet<Transform>();

        float xDir = Mathf.Sign(velocity.x);
        float yDir = Mathf.Sign(velocity.y);

        // Vertical movement
        if (velocity.y != 0)
        {
            float rayLength = Mathf.Abs(velocity.y) + SHELL_WIDTH;
       
            for(int i = 0; i < VERTICAL_RAY_COUNT; i++)
            {
                Vector2 rayOrigin = yDir == -1 ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                rayOrigin += (Vector2.right * verticalRaySpacing) * i;

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * yDir, rayLength, mask);

                if (hit.distance == 0f)
                    continue;

                if(hit && !movedPassengers.Contains(hit.transform))
                {
                    movedPassengers.Add(hit.transform);

                    float pushX = yDir == 1f ? velocity.x : 0f;
                    float pushY = velocity.y - (hit.distance - SHELL_WIDTH) * yDir;

                    passengers.Add(new PassengerMovement(hit.transform, new Vector2(pushX, pushY), true,
                                                         yDir == -1 ? ForceDir.Above : ForceDir.Below));
                }
            }

        }

        // Horizontal movement
        if (velocity.x != 0)
        {
            float rayLength = Mathf.Abs(velocity.x) + SHELL_WIDTH;

            for (int i = 0; i < HORIZONTAL_RAY_COUNT; i++)
            {
                Vector2 rayOrigin = xDir == -1 ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                rayOrigin += (Vector2.up * horizontalRaySpacing) * i;

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * xDir, rayLength, mask);

                if (hit.distance == 0f)
                    continue;

                if (hit && !movedPassengers.Contains(hit.transform))
                {
                    movedPassengers.Add(hit.transform);

                    float pushX = velocity.x - (hit.distance - SHELL_WIDTH) * xDir;

                    passengers.Add(new PassengerMovement(hit.transform, new Vector2(pushX, 0f), true,
                                                         xDir == -1 ? ForceDir.Right : ForceDir.Left));
                }
            }
        }

        // Passenger standing horizontally or downward moving platform
        if (yDir == -1 || (velocity.y == 0f && velocity.x != 0f))
        {
            for (int i = 0; i < HORIZONTAL_RAY_COUNT; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + (Vector2.right * verticalRaySpacing) * i;
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, SHELL_WIDTH * 2, mask);

                if (hit && !movedPassengers.Contains(hit.transform))
                {
                    movedPassengers.Add(hit.transform);
                    passengers.Add(new PassengerMovement(hit.transform, velocity, false, ForceDir.Above));
                }
            }
        }

    }

    #endregion // __MOVEMENT__

    #region InternalTypes

    private struct PassengerMovement
    {
        public Transform transform;
        public Vector2 velocity;
        public ForceDir dir;
        public bool moveBefore;

        public PassengerMovement(Transform _transform, Vector2 _velocity, bool _moveBefore, ForceDir _dir)
        {
            transform = _transform;
            velocity = _velocity;
            moveBefore = _moveBefore;
            dir = _dir;
        }
    }

    #endregion // __INTERNAL_TYPES__

}
