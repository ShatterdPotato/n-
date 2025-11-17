using UnityEngine;

public class SlopeScript : MonoBehaviour
{
    public Collider2D ninjaBottom;
    public Collider2D ninja;
    public NinjaMovement ninjaMovement;
    public int orientation; //1 = left, -1 = right
    public int angle;
    private RigidBody2D ninjaPhysics;

    private void Awake()
    {
        if (transform.localScale.x < 1)
            orientation = -1;
        else
            orientation = 1;

    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider == ninjaBottom)
        {
            ninjaMovement.makeSloped();
            ninjaMovement.setAngle(angle);
            ninja.transform.localEulerAngles = new Vector3(0f, 0f, orientation * angle);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider == ninja && !ninjaMovement.slopeCheck())
        {
            ninja.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
        }
    }
}
