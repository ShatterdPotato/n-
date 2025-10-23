using System;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

public class NinjaMovement : MonoBehaviour
{
    [SerializeField] public float terminalVelocityX;                            //maximum velocity for the ninja horizontally.
    [SerializeField] public float accelerationConstant;                         //how quickly the ninja accelerates to its terminal velocity.
    [SerializeField] public float minJumpHeight;                                //the abolsute minimum distance the ninja can jump, as if they only held the jump input for a single frame.
    [SerializeField] public float maxJumpHeight;                                //maximum height ninja can jump, regardles of how long jump input is held.
    [SerializeField] public float maxJumpTime;                                  //max time jump gives vertical force while held
    [SerializeField] public float frictionModifier;                             //fricitonal force that decelerates ninja to rest. 1 = 1 second, < 1 increases time, > 1 decreases time.
    [SerializeField] public float verticalAppliedForce;                         //makes the ninja jump up to max height quicker.
    [SerializeField] public Vector2 wallJumpForce;                              //sets the velocity of the ninja for a wall-jump.
    [SerializeField] public float terminalSlideVelocity;                        //speed in which ninja slides down wall while wall-sliding.
    [SerializeField] public Transform groundCheck;                              //stores empty GameObject pos for calculating if ninja is touching ground.
    [SerializeField] public Transform leftWallCheck;                            //stores empty GameObject pos for calculating if ninja is in contact with a wall from the left.
    [SerializeField] public Transform rightWallCheck;                           //stores empty GameObject pos for calculating if ninja is in contact with a wall from the right.
    [SerializeField] public float checkRadius;                                  //radius for circle used for any "checks" to see if ninja is near a ground or wall.
    [SerializeField] public LayerMask groundLayer;                              //Layer of all blocks that the ninja can jump on.
    [SerializeField] public LayerMask wallLayer;                                //Layer of all blocks that the ninja can cling onto.
    [SerializeField] private float deltaJumpPos;
    [SerializeField] private float horizontalAcc;
    [SerializeField] private float timeHeld;

    private NinjaControls ninjaInputs;
    private Rigidbody2D ninjaPhysics;
    private bool grounded;
    private bool jumping;
    private bool leftSliding;
    private bool rightSliding;
    private bool leftJumping;
    private bool rightJumping;
    private float jumpYPivot;
   
   
    private void Awake()
    {
        ninjaInputs = new NinjaControls();
        horizontalAcc = 0.0f;
        deltaJumpPos = 0.0f;
        ninjaPhysics = GetComponent<Rigidbody2D>();
        leftJumping = false;
        rightJumping = false;
        leftSliding = false;
        rightSliding = false;
        grounded = true;
        jumpYPivot = 0.0f;
    }

    private void OnEnable()
    {
        if (ninjaInputs == null) return;
        ninjaInputs.Ninja.Enable();
        ninjaInputs.Ninja.Movement.started += OnMove;
        ninjaInputs.Ninja.Jump.performed += OnJump;
        ninjaInputs.Ninja.Movement.canceled += OnMove;
    }
        
    private void OnDisable()
    {
        if (ninjaInputs == null) return;
        ninjaInputs.Ninja.Disable();
        ninjaInputs.Ninja.Movement.performed -= OnMove;
        ninjaInputs.Ninja.Jump.performed -= OnJump;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        horizontalAcc = context.ReadValue<float>();
        
        if (!grounded && Physics2D.OverlapCircle(rightWallCheck.position, checkRadius, wallLayer))
            rightSliding = true;
        else if (!grounded && Physics2D.OverlapCircle(leftWallCheck.position, checkRadius, wallLayer))
            leftSliding = true;
        if (horizontalAcc == -1)
        {
            rightSliding = false;

        }
        else if (horizontalAcc == 1)
        {
            leftSliding = false;
           
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        
        if (!grounded && Physics2D.OverlapCircle(leftWallCheck.position, checkRadius, wallLayer))
        {
            leftSliding = false;
            leftJumping = true;
            ninjaPhysics.linearVelocity = wallJumpForce;
            jumpYPivot = transform.position.y;
        }
        if (!grounded && Physics2D.OverlapCircle(rightWallCheck.position, checkRadius, wallLayer))
        {
            rightSliding = false;
            rightJumping = true;
            ninjaPhysics.linearVelocity = new Vector2(-1 * wallJumpForce.x, wallJumpForce.y);
            jumpYPivot = transform.position.y;
        }
        if (grounded)
        {
            jumping = true;
            jumpYPivot = transform.position.y;
        }
    }

    private void FixedUpdate()
    {
        print(rightJumping);
        grounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        if (ninjaInputs == null) return;
        if (ninjaInputs.Ninja.Movement.IsPressed())
        {
            ninjaPhysics.linearVelocityX += horizontalAcc * accelerationConstant;
            if (Math.Abs(ninjaPhysics.linearVelocityX) > terminalVelocityX)
                ninjaPhysics.linearVelocityX = horizontalAcc * terminalVelocityX;
        }
        else if (!ninjaInputs.Ninja.Movement.IsPressed() && ninjaPhysics.linearVelocityX != 0)
        {
            ninjaPhysics.linearVelocityX -= Math.Sign(ninjaPhysics.linearVelocityX) * terminalVelocityX * Time.deltaTime * frictionModifier;
            if (Math.Abs(ninjaPhysics.linearVelocityX) <= 0)
                ninjaPhysics.linearVelocityX = 0;
        }

        if (ninjaInputs.Ninja.Jump.IsPressed() && jumping)
        {
            ninjaPhysics.linearVelocityY += verticalAppliedForce;
            deltaJumpPos = transform.position.y - jumpYPivot;
            if (deltaJumpPos > maxJumpHeight)
            {
                if (!grounded && Physics2D.OverlapCircle(rightWallCheck.position, checkRadius, wallLayer))
                    rightSliding = true;
                else if (!grounded && Physics2D.OverlapCircle(leftWallCheck.position, checkRadius, wallLayer))
                    leftSliding = true;
                jumping = false;
                deltaJumpPos = 0f;
            }
        }
        else if (ninjaInputs.Ninja.Jump.IsPressed() && leftJumping)
        {
            ninjaPhysics.linearVelocityY += wallJumpForce.y * Time.deltaTime;
            ninjaPhysics.linearVelocityX = wallJumpForce.x;
            timeHeld += Time.deltaTime;
            if (timeHeld > maxJumpTime)
            {
                if (!grounded && Physics2D.OverlapCircle(rightWallCheck.position, checkRadius, wallLayer))
                    rightSliding = true;
                if (!grounded && Physics2D.OverlapCircle(leftWallCheck.position, checkRadius, wallLayer))
                    leftSliding = true;
                leftJumping = false;
                deltaJumpPos = 0f;
            }

        }
        else if (ninjaInputs.Ninja.Jump.IsPressed() && rightJumping)
        {
            ninjaPhysics.linearVelocityY += wallJumpForce.y * Time.deltaTime;
            ninjaPhysics.linearVelocityX = -1 * wallJumpForce.x;
            timeHeld += Time.deltaTime;
            if (timeHeld > maxJumpTime)
            {
                if (!grounded && Physics2D.OverlapCircle(rightWallCheck.position, checkRadius, wallLayer))
                    rightSliding = true;
                if (!grounded && Physics2D.OverlapCircle(leftWallCheck.position, checkRadius, wallLayer))
                    leftSliding = true;
                rightJumping = false;
                deltaJumpPos = 0f;
            }

        }
        else
        {
            if (horizontalAcc == 1 && !grounded && Physics2D.OverlapCircle(rightWallCheck.position, checkRadius, wallLayer))
                rightSliding = true;
            if (horizontalAcc == -1 && !grounded && Physics2D.OverlapCircle(leftWallCheck.position, checkRadius, wallLayer))
                leftSliding = true;
            jumping = false;
        }

        if (rightSliding && !Physics2D.OverlapCircle(rightWallCheck.position, checkRadius, wallLayer))
            rightSliding = false;
        if (leftSliding && !Physics2D.OverlapCircle(leftWallCheck.position, checkRadius, wallLayer))
            leftSliding = false;

        if (leftSliding || rightSliding)
        {
            if (ninjaPhysics.linearVelocityY > terminalSlideVelocity)
            {
                ninjaPhysics.linearVelocityY -= terminalSlideVelocity * Time.deltaTime;
            }
            else
                ninjaPhysics.linearVelocityY = Mathf.Clamp(ninjaPhysics.linearVelocityY, terminalSlideVelocity, float.MaxValue);
        }

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        Gizmos.DrawWireSphere(rightWallCheck.position, checkRadius);
        Gizmos.DrawWireSphere(leftWallCheck.position, checkRadius);
    }
}


