using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class ClothSimulator : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public float spacing = 0.5f;
    public GameObject particlePrefab;

    private List<Particle> particles = new List<Particle>();
    private List<Spring> structuralSprings = new List<Spring>();

    
    public void BuildCloth()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector3 position = new Vector3(i * spacing, j * spacing, 0);
                GameObject particleObject = Instantiate(particlePrefab);
                particleObject.transform.position = position;
                Particle particle = particleObject.GetComponent<Particle>();
                particle.Initialize(position);
                particles.Add(particle);
            }
        }
    }

    public void CreateStructuralConstraints()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (i <  width - 1)
                {
                    structuralSprings.Add(new Spring(GetParticle(i, j), GetParticle(i + 1, j)));
                }
                if (j < height - 1)
                {
                    structuralSprings.Add(new Spring(GetParticle(i, j), GetParticle(i, j + 1)));
                }
            }
        }
    }

    Particle GetParticle(int xPosition, int yPosition)
    {
        return particles[xPosition + yPosition * height];
    }
}
