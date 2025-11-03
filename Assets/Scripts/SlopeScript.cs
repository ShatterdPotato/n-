using UnityEngine;

public class SlopeScript : MonoBehaviour
{
    public Collider2D ninja;
    public NinjaMovement ninjaMovement;
    public int orientation; //1 = left, -1 = right

    private void Awake()
    {
        if (transform.localScale.x < 1)
            orientation = -1;
        else
            orientation = 1;

    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider == ninja)
        {
            ninjaMovement.makeSloped();
            ninja.transform.localEulerAngles = new Vector3(0f, 0f, orientation * 45f);
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
