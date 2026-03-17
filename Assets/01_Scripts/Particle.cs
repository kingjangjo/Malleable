using UnityEngine;

public class Particle : MonoBehaviour
{
    public Vector3 position;
    public Vector3 prevPosition;
    public Vector3 velocity;

    public float density;
    public float nearDensity;

    public float pressure;
    public float nearPressure;

    public bool isPlayerOwned;

    public Particle(Vector3 pos)
    {
        position = pos;
        velocity = Vector3.zero;
    }
}
