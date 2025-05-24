using System.Runtime.CompilerServices;
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


    public void Initialize(Vector3 startPosition, bool pinned)
    {
        currentPosition = startPosition;
        previousPosition = startPosition;
        transform.position = currentPosition;
        isPinned = pinned;
    }

    public void LateUpdate()
    {
        transform.position = currentPosition;
    }

    public void UpdatePosition()
    {
        if (isPinned) return;

        velocity = currentPosition - previousPosition;
        newPosition = currentPosition + velocity + acceleration * Time.deltaTime * Time.deltaTime;
        previousPosition = currentPosition;
        currentPosition = newPosition;
    }

    public void ApplyForce(Vector3 force)
    {
        acceleration += force;
    }

    public void ResetAcceleration()
    {
        acceleration = Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isPinned ? Color.red : Color.white;
    }
}
