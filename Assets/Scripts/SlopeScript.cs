using UnityEngine;

public class SlopeScript : MonoBehaviour
{
    public Collider2D ninjaBottom;
    public Collider2D ninja;
    public NinjaMovement ninjaMovement;
    public int orientation; //1 = left, -1 = right
    public int angle;
    public Rigidbody2D ninjaPhysics;

    private void Awake()
    {
        if (transform.localScale.x < 1)
            orientation = -1;
        else
            orientation = 1;

    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        print(collision.collider);
        if (collision.collider == ninjaBottom && ninjaMovement.slopeCheck())
        {
            print("sup");
            ninjaMovement.makeSloped();
            ninjaMovement.setAngle(angle);
            ninjaPhysics.gravityScale = 0.5f;
            ninja.transform.localEulerAngles = new Vector3(0f, 0f, orientation * angle);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider == ninja && !ninjaMovement.slopeCheck())
        {
            ninjaMovement.setAngle(0);
            ninja.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
            ninjaPhysics.gravityScale = 1f;
        }
    }
}
