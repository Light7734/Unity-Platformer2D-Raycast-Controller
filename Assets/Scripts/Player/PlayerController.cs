using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : ControllerBase
{
    // State
    public CollisionState collisionState = new CollisionState();
    private bool ignoreOneWayPlatforms = false;
    GameObject ignoredOneWayPlatform = null;

    // Attributes
    private float maxSlopeAscendAngle;
    private float maxSlopeDecendAngle;
    private float stepHeight;

    // Layer masks
    [SerializeField] private LayerMask platformMask;
    [SerializeField] private LayerMask oneWayPlatformMask;
    [SerializeField] private LayerMask triggerMask;

    [SerializeField] Tilemap tileMap;

    // Events TODO:
    public event Action<RaycastHit2D> onControllerCollidedEvent;
    public event Action<Collider2D> onTriggerEnterEvent;
    public event Action<Collider2D> onTriggerStayEvent;
    public event Action<Collider2D> onTriggerExitrEvent;

    protected override void Start()
    {
        base.Start();
    }

    public void SetAttributes(Attributes attributes)
    {
        maxSlopeAscendAngle = attributes.maxSlopeAscendAngle;
        maxSlopeDecendAngle = attributes.maxSlopeDecendAngle;
        stepHeight = attributes.stepHeight;
    }

    public void IgnoreOneWayPlatforms()
    {
        ignoreOneWayPlatforms = true;
    }

    public override void Move(Vector2 velocity, ForceDir forceDir)
    {
        // Prepare the collisions/raycasts
        collisionState.Reset();

        UpdateRaycastOrigins();
        collisionState.prevVelocity = velocity;

        // Calculate velocity
        if (velocity.y < 0f)
            DecendSlope(ref velocity);

        if (velocity.x != 0f)
            HorizontalCollisions(ref velocity);

        if (velocity.y != 0f)
            VerticalCollisions(ref velocity);

        // Movement was applied by another object
        if(forceDir != ForceDir.Self)
        {
            if (forceDir == ForceDir.Below)
                collisionState.below = true;

            if (forceDir == ForceDir.Above)
                collisionState.above = true;

            if (forceDir == ForceDir.Left)
                collisionState.left = true;

            if (forceDir == ForceDir.Right)
                collisionState.right = true;
        }
        
        // Apply the calculated velocity
        transform.Translate(velocity);

        // Check for the previous collisions
        UpdateRaycastOrigins();
        CheckPrevCollisions();
        
        // oneWayPlatforms
        if (velocity.y > 0.0f || collisionState.below)
            ignoredOneWayPlatform = null;

        ignoreOneWayPlatforms = false;
    }

    private void CheckPrevCollisions()
    {
        // TODO:

        // Below
        if (collisionState.below == false && collisionState.prevBelow == true)
        {
            for (int i = 0; i < VERTICAL_RAY_COUNT; i++)
            {
                Vector2 rayOrigin = raycastOrigins.bottomLeft + (Vector2.right * verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, SHELL_WIDTH * 2, platformMask);

                if ((ignoredOneWayPlatform != null) && hit.transform.gameObject == ignoredOneWayPlatform)
                    return;

                if (hit)
                    collisionState.below = true;
            }
        }

        // Above
        if (collisionState.above == false && collisionState.prevAbove == true)
        {
            for (int i = 0; i < VERTICAL_RAY_COUNT; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + (Vector2.right * verticalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, SHELL_WIDTH * 2, platformMask);

                if (hit)
                    collisionState.above = true;
            }
        }

        // Left
        if (collisionState.left == false && collisionState.prevLeft == true)
        {
            for (int i = 0; i < HORIZONTAL_RAY_COUNT; i++)
            {
                Vector2 rayOrigin = raycastOrigins.bottomLeft + (Vector2.up * horizontalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.left, SHELL_WIDTH * 2, platformMask);

                if (hit)
                    collisionState.left = true;
            }
        }

        // Right
        if (collisionState.right == false && collisionState.prevRight == true)
        {
            for (int i = 0; i < HORIZONTAL_RAY_COUNT; i++)
            {
                Vector2 rayOrigin = raycastOrigins.bottomRight + (Vector2.up * horizontalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right, SHELL_WIDTH * 2, platformMask);

                if (hit)
                    collisionState.right = true;
            }
        }
    }

    #region COLLISIONS
    private void HorizontalCollisions(ref Vector2 velocity)
    {
        float xDir = Mathf.Sign(velocity.x);
        float rayLength = Mathf.Abs(velocity.x) + SHELL_WIDTH;

        float distToStair = 0f;
        bool climbingStair = false;

        for(int i = 0; i < HORIZONTAL_RAY_COUNT; i++)
        {
            Vector2 rayOrigin = xDir == -1 ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin.y += horizontalRaySpacing * i;

            RaycastHit2D hit;

            if (i == 0 && !ignoreOneWayPlatforms)
                hit = Physics2D.Raycast(rayOrigin, Vector2.right * xDir, rayLength, platformMask | oneWayPlatformMask);
            else
                hit = Physics2D.Raycast(rayOrigin, Vector2.right * xDir, rayLength, platformMask);

            if(hit)
            {
                if (hit.transform.gameObject == ignoredOneWayPlatform)
                    continue;

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                // Step
                if (i == 0 && slopeAngle == 90f && collisionState.prevBelow)
                {
                    Sprite sprite = tileMap.GetSprite(tileMap.WorldToCell(hit.point + SHELL_WIDTH * xDir * Vector2.right));;
                    List<Vector2> physicsShape = new List<Vector2>();

                    float stairTop;
                    float stairHeight = Mathf.Infinity;

                    for (int j = 0; j < sprite.GetPhysicsShapeCount(); j++)
                    {
                        sprite.GetPhysicsShape(j, physicsShape);
                        stairTop = tileMap.WorldToCell(hit.point).y + .5f + physicsShape[2].y;

                        if (stairTop - hit.point.y <= 0)
                            continue;

                        if (stairTop - hit.point.y < stairHeight)
                            stairHeight = stairTop - hit.point.y;
                    }

                    if (stairHeight <= stepHeight)
                    {
                        velocity.y = stairHeight + SHELL_WIDTH;
                        climbingStair = true;
                        distToStair = hit.distance;
                        continue;
                    }
                }

                // Ignore rays against the stair
                if (climbingStair && distToStair == hit.distance)
                    continue;

                // We're moving into a oneWayPlatform from it's back
                if (i == 0 && !ignoreOneWayPlatforms && hit.collider as EdgeCollider2D != null && slopeAngle > maxSlopeAscendAngle)
                    continue;

                // Ascending slope
                if (i == 0 && slopeAngle <= maxSlopeAscendAngle)
                {
                    float distanceToSlopeStart = 0f;
                    if(collisionState.decendingSlope)
                    {
                        collisionState.decendingSlope = false;
                        velocity = collisionState.prevVelocity;
                    }

                    if(slopeAngle != collisionState.prevSlopeAngle)
                    {
                        distanceToSlopeStart = hit.distance - SHELL_WIDTH;
                        velocity.x -= distanceToSlopeStart * xDir;
                    }

                    AscendSlope(ref velocity, slopeAngle);
                    
                    if(distanceToSlopeStart > SHELL_WIDTH)
                        velocity.x += distanceToSlopeStart * xDir;
                }

                // Not a slope or the angle is too large
                if(!collisionState.ascendingSlope || slopeAngle > maxSlopeAscendAngle)
                {
                    velocity.x = (hit.distance - SHELL_WIDTH) * xDir;

                    if (collisionState.ascendingSlope)
                        velocity.y = Mathf.Tan(collisionState.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);

                    rayLength = hit.distance;

                    if (xDir == -1)
                        collisionState.left = true;
                    if (xDir == 1)
                        collisionState.right = true;
                }
            }
        }
    }

    private void VerticalCollisions(ref Vector2 velocity)
    {
        float yDir = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + SHELL_WIDTH;

        for(int i = 0; i < VERTICAL_RAY_COUNT; i++)
        {
            Vector2 rayOrigin = yDir == -1 ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin.x += (verticalRaySpacing * i) + velocity.x;

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * yDir, rayLength,
                                                 yDir == -1 ? (LayerMask)(platformMask | oneWayPlatformMask) : platformMask);

            if(hit)
            {
                if ( (ignoreOneWayPlatforms && Math.Pow(2, hit.transform.gameObject.layer) == oneWayPlatformMask.value) ||
                      ignoredOneWayPlatform == hit.transform.gameObject)
                {
                    ignoredOneWayPlatform = hit.transform.gameObject;

                    // re-check against platformMask only
                    hit = Physics2D.Raycast(rayOrigin, Vector2.up * yDir, rayLength, platformMask);

                    if(!hit)
                        continue;
                }

                if (hit.distance <= SHELL_WIDTH)
                    velocity.y = 0f;
                else
                    velocity.y = (hit.distance - SHELL_WIDTH) * yDir;

                rayLength = hit.distance;

                if (yDir == -1)
                    collisionState.below = true;
                if (yDir == 1)
                    collisionState.above = true;
            }
        }

        // Check if we can step down
        if (collisionState.below == false && collisionState.prevBelow == true && velocity.y < 0f && velocity.x != 0f)
        {
            Vector2 rayOrigin = Mathf.Sign(velocity.x) == -1f ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, stepHeight + SHELL_WIDTH, platformMask | oneWayPlatformMask);

            // Step down
            if(hit)
            {
                if( !( (ignoreOneWayPlatforms && Mathf.Pow(2, hit.transform.gameObject.layer) == oneWayPlatformMask.value) ||
                       ignoredOneWayPlatform == hit.transform.gameObject) )
                {
                    velocity.y = -(Mathf.Abs(hit.distance) - SHELL_WIDTH);
                    collisionState.below = true;
                    collisionState.ascendingSlope = false;
                }
            }
        }
    }

    #endregion // __COLLISIONS__ //

    #region Slopes

    private void AscendSlope(ref Vector2 velocity, float slopeAngle)
    {
        float xVel = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * velocity.x;
        float yVel = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);

        if(yVel >= velocity.y)
        {
            velocity = new Vector2(xVel, yVel);

            collisionState.below = true;
            collisionState.ascendingSlope = true;
            collisionState.slopeAngle = slopeAngle;
        }
    }

    private void DecendSlope(ref Vector2 velocity)
    {
        float xDir = Mathf.Sign(velocity.x);
        Vector2 rayOrigin = xDir == -1 ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, Mathf.Infinity, platformMask | oneWayPlatformMask);

        if(hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

            if(slopeAngle != 0f && slopeAngle <= maxSlopeDecendAngle)
            {
                if(Mathf.Sign(hit.normal.x) == xDir)
                {
                    if(hit.distance - SHELL_WIDTH <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x))
                    {
                        // oneWayPaltform
                        if( (ignoreOneWayPlatforms && Math.Pow(2, hit.transform.gameObject.layer) == oneWayPlatformMask.value) || 
                             ignoredOneWayPlatform == hit.transform.gameObject)
                        {
                            ignoredOneWayPlatform = hit.transform.gameObject;
                            return;
                        }

                        float yVel = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                        float xVel = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * velocity.x;

                        velocity = new Vector2(xVel, velocity.y - yVel);

                        collisionState.below = true;
                        collisionState.decendingSlope = true;
                        collisionState.slopeAngle = slopeAngle;
                    }
                }
            }
        }

    }

    #endregion // __SLOPES__ //

    #region InternalTypes

    public struct Attributes
    {
        public float maxSlopeDecendAngle;
        public float maxSlopeAscendAngle;
        public float stepHeight;
    }

    public class CollisionState
    {
        // COLLISIONS
        public bool below, above, left, right;
        public bool prevBelow, prevAbove, prevLeft, prevRight;


        // SLOPES
        public bool ascendingSlope;
        public bool decendingSlope;

        public float slopeAngle;
        public float prevSlopeAngle;

        public Vector2 prevVelocity;

        public CollisionState()
        {
            below = above = left = right = false;
            prevBelow = prevAbove = prevLeft = prevRight = false;

            ascendingSlope = decendingSlope = false;

            slopeAngle = prevSlopeAngle = 0f;

            prevVelocity = Vector2.zero;
        }

        public bool hasCollision() { return below || above || left || right; }
        public bool isGrounded() { return below; }

        public void Reset()
        {
            // COLLISIONS //
            // previous state
            prevAbove = above;
            prevBelow = below;
            prevLeft = left;
            prevRight = right;

            // current state
            below = false;
            above = false;
            left = false;
            right = false;

            // SLOPES //
            // previous state
            prevSlopeAngle = slopeAngle;

            // current state
            ascendingSlope = false;
            decendingSlope = false;
            slopeAngle = 0f;
        }

        public override string ToString()
        {
            // TODO: Format better
            return string.Format("[PlayerController::CollisionState]: b: {0}, a: {1}, l: {2}, r: {3}, climingSlope: {4}, decendingSlope: {5}, slopeAngel: {6}, slopeAngelOld: {7}",
                                 below, above, left, right, ascendingSlope, decendingSlope, slopeAngle, prevSlopeAngle);
        }

    }

    #endregion // __INTERNAL_TYPES__
}
