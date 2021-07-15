using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneWayPlatformController : ControllerBase
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
        foreach (PassengerMovement passenger in passengers)
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