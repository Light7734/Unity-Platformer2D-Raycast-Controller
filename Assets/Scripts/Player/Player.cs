using UnityEngine;
using UnityEngine.InputSystem;

// TODO: Tidy up / rename stuff
[RequireComponent(typeof(PlayerController))]
public class Player : MonoBehaviour
{
    [SerializeField] private PlayerController controller;

    // General Movement
    [SerializeField] [Range(1f, 10f)] private float moveSpeed = 6f;
    [SerializeField] [Range(0f, .5f)] private float stepHeight = 0f;

    float velXSmoothing;

    // Slope movement
    [SerializeField] [Range(0f, 90f)] private float maxClimbAngle = 45f;
    [SerializeField] [Range(0f, 90f)] private float maxDecendAngle = 45f;

    // Air movement
    [SerializeField] [Range(0f, 1f)] private float accelerationAirborne = .3f;
    [SerializeField] [Range(0f, 1f)] private float accelerationGrounded = .1f;

    // Jumping
    [SerializeField] [Range(1f, 10f)] private float jumpHeight = 5f;
    [SerializeField] [Range(0f, 1f)] private float timeToJumpApex = .1f;

    private float gravity;
    private float jumpVelocity;

    // Current input
    private Vector2 desiredVelocity = Vector2.zero;

    private float moveDir = 0f;
    private bool isJumping = false;

    void Start()
    {
        // Controller attributes
        PlayerController.Attributes controllerAttributes;

        controllerAttributes.maxSlopeAscendAngle = maxClimbAngle;
        controllerAttributes.maxSlopeDecendAngle = maxDecendAngle;
        controllerAttributes.stepHeight = stepHeight;

        controller.SetAttributes(controllerAttributes);

        // Jump / Gravity
        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity * timeToJumpApex);

    }

    void Update()
    {
    }

    private void FixedUpdate()
    {
        // COLLISIONS //
        if (controller.collisionState.below && desiredVelocity.y < 0f)
            desiredVelocity.y = 0f;
        if (controller.collisionState.above && desiredVelocity.y > 0f)
            desiredVelocity.y = 0f;

        // MOVEMENT //
        if (controller.collisionState.below)
            desiredVelocity.x = Mathf.SmoothDamp(desiredVelocity.x, moveDir * moveSpeed, ref velXSmoothing, accelerationGrounded);
        else
            desiredVelocity.x = Mathf.SmoothDamp(desiredVelocity.x, moveDir * moveSpeed, ref velXSmoothing, accelerationAirborne);

        // GRAVITY //
        desiredVelocity.y += gravity * Time.deltaTime;

        // JUMP //
        if (isJumping)
        {
            desiredVelocity.y = jumpVelocity;
            isJumping = false;
        }

        controller.Move(desiredVelocity * Time.fixedDeltaTime, ForceDir.Self);
    }


    public void OnMovement(InputValue value)
    {
        moveDir = value.Get<float>();
    }

    public void OnJump(InputValue value)
    {
        if (controller.collisionState.below)
            isJumping = true;
    }

}
