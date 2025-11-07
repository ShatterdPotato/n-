using System;
using System.IO;
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
    [SerializeField] public float backwardsHorizontalForce;                     //this is the horizontal velocity for a wall jump if the ninja wall jumps yet is pressing the keys that go towards the wall.
    [SerializeField] public float verticalAppliedForce;                         //makes the ninja jump up to max height quicker.
    [SerializeField] public Vector2 wallJumpForce;                              //sets the velocity of the ninja for a wall-jump.
    [SerializeField] public Vector2 initialWallJumpForce;                       //initial velocity of the ninja for a wall-jump.
    [SerializeField] public float terminalSlideVelocity;                        //speed in which ninja slides down wall while wall-sliding.
    [SerializeField] public Transform groundCheck;                              //stores empty GameObject pos for calculating if ninja is touching ground.
    [SerializeField] public Transform leftWallCheck;                            //stores empty GameObject pos for calculating if ninja is in contact with a wall from the left.
    [SerializeField] public Transform rightWallCheck;                           //stores empty GameObject pos for calculating if ninja is in contact with a wall from the right.
    [SerializeField] public float checkRadius;                                  //radius for circle used for any "checks" to see if ninja is near a ground or wall.
    [SerializeField] public LayerMask groundLayer;                              //Layer of all blocks that the ninja can jump on.
    [SerializeField] public LayerMask wallLayer;                                //Layer of all blocks that the ninja can cling onto.
    [SerializeField] public LayerMask slopeLayer;                                //Layer of all slopes.
    [SerializeField] public Collider2D ninjaHitbox;                              //associated Collider2D of ninja.
    [SerializeField] private float deltaJumpPos;
    [SerializeField] private float horizontalAcc;
    [SerializeField] private float timeHeld;
    [SerializeField] private bool grounded;
    [SerializeField] private bool jumping;
    [SerializeField] private bool leftSliding;
    [SerializeField] private bool rightSliding;
    [SerializeField] private bool leftJumping;
    [SerializeField] private bool rightJumping;


    private NinjaControls ninjaInputs;
    private Rigidbody2D ninjaPhysics;
    private float jumpYPivot;
    private Collider2D leftWallTouching;
    private Collider2D rightWallTouching;
    private bool sloped;


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
        leftWallTouching = null;
        rightWallTouching = null;
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

        rightSliding = !grounded && rightWallTouching;
        leftSliding = !grounded && leftWallTouching;
        if (horizontalAcc == -1)
            rightSliding = false;
        else if (horizontalAcc == 1)
            leftSliding = false;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        Action initializeJumpForce = () =>
        {
            ninjaPhysics.linearVelocityY = initialWallJumpForce.y;
            if (horizontalAcc == -1)
                ninjaPhysics.linearVelocityX = backwardsHorizontalForce;
            else if (horizontalAcc == 1)
                ninjaPhysics.linearVelocityX = -1 * backwardsHorizontalForce;
            else if (leftWallTouching)
                ninjaPhysics.linearVelocityX = initialWallJumpForce.x;
            else if (rightWallTouching)
                ninjaPhysics.linearVelocityX = -1 * initialWallJumpForce.x;
            timeHeld = 0f;
        };

        if (grounded)
        {
            jumping = true;
            jumpYPivot = transform.position.y;
        }
        else if (!grounded && leftWallTouching)
        {
            leftSliding = false;
            leftJumping = true;
            initializeJumpForce();
        }
        else if (!grounded && rightWallTouching)
        {
            rightSliding = false;
            rightJumping = true;
            initializeJumpForce();
        }

    }

    private void FixedUpdate()
    {
        grounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        leftWallTouching = Physics2D.OverlapCircle(leftWallCheck.position, checkRadius, wallLayer);
        rightWallTouching = Physics2D.OverlapCircle(rightWallCheck.position, checkRadius, wallLayer);
        if (ninjaInputs == null) return;
        bool jumpPressed = ninjaInputs.Ninja.Jump.IsPressed();

        if (sloped)
            handleSlopedMovement();
        else
            handleMovement();
        handleJumpMovement(jumpPressed);
        handleWallMovement();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        Gizmos.DrawWireSphere(rightWallCheck.position, checkRadius);
        Gizmos.DrawWireSphere(leftWallCheck.position, checkRadius);
    }

    private void handleMovement()
    {
        if (ninjaInputs.Ninja.Movement.IsPressed())
        {
            ninjaPhysics.linearVelocityX += horizontalAcc * accelerationConstant;
            if (Math.Abs(ninjaPhysics.linearVelocityX) > terminalVelocityX)
                ninjaPhysics.linearVelocityX = horizontalAcc * terminalVelocityX;
        }
        else if (ninjaPhysics.linearVelocityX != 0)
        {
            ninjaPhysics.linearVelocityX -= Math.Sign(ninjaPhysics.linearVelocityX) * terminalVelocityX * Time.deltaTime * frictionModifier;
            if (Math.Abs(ninjaPhysics.linearVelocityX) < 0.1)
                ninjaPhysics.linearVelocityX = 0;
        }
    }

    private void handleSlopedMovement()
    {
        if (ninjaInputs.Ninja.Movement.IsPressed())
        {
            ninjaPhysics.linearVelocityX += (float)Math.Cos(Math.PI / 4) * (horizontalAcc * accelerationConstant);
            ninjaPhysics.linearVelocityY += (float)Math.Sin(Math.PI / 4) * (horizontalAcc * accelerationConstant);
            if (Math.Abs(Math.Sqrt(Math.Pow(ninjaPhysics.linearVelocityX, 2) + Math.Pow(ninjaPhysics.linearVelocityY, 2))) > terminalVelocityX)
                ninjaPhysics.linearVelocityX = horizontalAcc * terminalVelocityX;
        }
        else if (ninjaPhysics.linearVelocityX != 0)
        {
            ninjaPhysics.linearVelocityX -= Math.Sign(ninjaPhysics.linearVelocityX) * terminalVelocityX * Time.deltaTime * frictionModifier;
            if (Math.Abs(ninjaPhysics.linearVelocityX) < 0.1)
                ninjaPhysics.linearVelocityX = 0;
        }
    }


    private void handleJumpMovement(bool jumpPressed)
    {
        Action<int> jumpTimeCalc = (direction) => //dont ask why I did an int here just accept it ok? (-1 = left, 1 = right, 0 = ground) see it works im a genius
        {
            if (direction != 0)
                timeHeld += Time.deltaTime;
            else
                deltaJumpPos = transform.position.y - jumpYPivot;
            if (timeHeld > maxJumpTime || (direction == 0 && deltaJumpPos > maxJumpHeight))
            {
                if (!grounded && rightWallTouching)
                    rightSliding = true;
                else if (!grounded && leftWallTouching)
                    leftSliding = true;
                if (direction == -1)
                    leftJumping = false;
                else if (direction == 0)
                    jumping = false;
                else
                    rightJumping = false;
                timeHeld = 0f;
                deltaJumpPos = 0f;
            }
        };

        if (jumpPressed && jumping)
        {
            ninjaPhysics.linearVelocityY += verticalAppliedForce;
            jumpTimeCalc(0);
        }
        else if (jumpPressed && leftJumping)
        {
            ninjaPhysics.linearVelocity += wallJumpForce * Time.deltaTime;
            jumpTimeCalc(-1);
        }
        else if (jumpPressed && rightJumping)
        {
            ninjaPhysics.linearVelocityY += wallJumpForce.y * Time.deltaTime;
            ninjaPhysics.linearVelocityX += -1 * wallJumpForce.x * Time.deltaTime;
            jumpTimeCalc(1);

        }
        else
        {
            if (horizontalAcc == 1 && !grounded && rightWallTouching)
                rightSliding = true;
            if (horizontalAcc == -1 && !grounded && leftWallTouching)
                leftSliding = true;
            jumping = false;
            leftJumping = false;
            rightJumping = false;
        }
    }

    private void handleWallMovement()
    {
        if (rightSliding && !rightWallTouching)
            rightSliding = false;
        if (leftSliding && !leftWallTouching)
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

    public void makeSloped()
    {
        sloped = true;
    }

    public bool slopeCheck()
    {
        sloped = Physics2D.OverlapCircle(groundCheck.position, checkRadius, slopeLayer);
        return sloped;
    }
    
}


