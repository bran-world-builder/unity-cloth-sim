using UnityEngine;

public class Spring : MonoBehaviour
{
    public Particle particle1;
    public Particle particle2;
    public float stiffness = 0.2f;
    public float restLength;
    public float maxStretchRatio = 1.2f;

    public void Initialize(Particle a, Particle b)
    {
        particle1 = a;
        particle2 = b;
        restLength = Vector3.Distance(a.currentPosition, b.currentPosition);
    }

    public void ApplyConstraint()
    {
        Vector3 delta = particle2.currentPosition - particle1.currentPosition;
        float currentLength = delta.magnitude;
        float stretchRatio = currentLength / restLength;

        if (stretchRatio > maxStretchRatio)
        {
            currentLength = restLength * maxStretchRatio;
            delta = delta.normalized * currentLength;
        }

        float diff = (currentLength - restLength) / currentLength;

        if (particle1.isPinned && particle2.isPinned) return;

        if (!particle1.isPinned)
            particle1.currentPosition += delta * 0.5f * diff * stiffness;
        if (!particle2.isPinned)
            particle2.currentPosition -= delta * 0.5f * diff * stiffness;
    }
}
