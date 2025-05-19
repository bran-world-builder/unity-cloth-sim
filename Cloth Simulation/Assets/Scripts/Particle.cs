using Unity.PlasticSCM.Editor.WebApi;
using UnityEngine;

public class Particle : MonoBehaviour
{
    [SerializeField] Vector3 acceleration;
    
    public Vector3 currentPosition;
    public Vector3 previousPosition;
    public bool isPinned;

    private Vector3 velocity;
    private Vector3 newPosition;


    public void Initialize(Vector3 startPosition)
    {
        currentPosition = startPosition;
        previousPosition = startPosition;
    }

    public void UpdatePosition()
    {
        velocity = currentPosition - previousPosition;
        newPosition = currentPosition + velocity + acceleration * Time.deltaTime * Time.deltaTime;
        previousPosition = currentPosition;
        currentPosition = newPosition;
    }
}
