using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;

public class ClothSimulator : MonoBehaviour
{
    public static int width = 10;
    public static int height = 10;
    public float spacing = 0.5f;
    public Transform clothRootTransform;
    public GameObject particlePrefab;

    Particle[,] particleGrid;
    private List<Spring> structuralSprings = new List<Spring>();

    
    public void BuildCloth()
    {
        particleGrid = new Particle[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector3 position = new Vector3(i * spacing, j * spacing, 0);
                GameObject particleObject = Instantiate(particlePrefab, position, Quaternion.identity, clothRootTransform);
                particleObject.name = $"Particle{i}_{j}";
                Particle particle = particleObject.GetComponent<Particle>();

                bool isPinned = (j == height - 1 && (i == 0 || i == width - 1));
                particle.Initialize(position, isPinned);

                particleGrid[i, j] = particle;
            }
        }
    }

    private void CreateSpring(Particle a, Particle b)
    {
        GameObject springObject = new GameObject("Spring_" + a.name + "_" + b.name);
        springObject.transform.parent = this.transform;

        Spring spring = springObject.AddComponent<Spring>();
        spring.Initialize(a, b);
        structuralSprings.Add(spring);
    }

    public void CreateStructuralConstraints()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Particle current = particleGrid[i, j];
                if (i <  width - 1)
                {
                    CreateSpring(current, particleGrid[i + 1, j]);
                }
                if (j < height - 1)
                {
                    CreateSpring(current, particleGrid[i, j + 1]);
                }
            }
        }
    }
}
