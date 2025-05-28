using UnityEngine;
using System.Collections.Generic;

public class ClothSimulator : MonoBehaviour
{
    public static int width = 10;
    public static int height = 10;
    public float spacing = 0.5f;
    public int solverIterations = 15;
    public float groundHeight = -0.5f;
    public float gravityStrength = 9.81f;
    public float yOffset = 1.0f;
    [Range(0f, 1f)]
    public float structuralStiffness = 0.001f;
    [Range(0f, 1f)]
    public float shearStiffness = 0.0005f;
    [Range(0f, 1f)]
    public float bendStiffness = 0.0001f;
    public Transform clothRootTransform;
    public GameObject particlePrefab;

    Particle[,] particleGrid;
    private List<Spring> structuralSprings = new List<Spring>();
    private List<Spring> shearSprings = new List<Spring>();
    private List<Spring> bendSprings = new List<Spring>();

    public void Start()
    {
        BuildCloth();
    }

    public void FixedUpdate()
    {
        // apply force
        ApplyForces();

        // simulate verlet, get new position
        SimulateVerlet();

        // enforce constraints using new position
        EnforceConstraints();

        // check for the ground
        EnforceGroundPosition();

        // update particle transforms with constrained new position
        UpdateParticleTransforms();

        if (Input.GetKeyDown(KeyCode.D))
        {
            UnpinEverything();
        }
    }


    #region Builders

    public void BuildCloth()
    {
        particleGrid = new Particle[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector3 position = new Vector3(i * spacing, j * spacing + yOffset, 0);
                GameObject particleObject = Instantiate(particlePrefab, position, Quaternion.identity, clothRootTransform);
                particleObject.name = $"Particle{i}_{j}";
                Particle particle = particleObject.GetComponent<Particle>();

                // pin the top corners
                bool isPinned = (j == height - 1 && (i == 0 || i == width - 1));
                particle.Initialize(position, isPinned);

                particleGrid[i, j] = particle;
            }
        }
        CreateStructuralConstraints();
        //CreateShearConstraints();
        CreateBendConstraints();
    }

    private void CreateSpring(Particle a, Particle b)
    {
        GameObject springObject = new GameObject("Spring_" + a.name + "_" + b.name);
        springObject.transform.parent = this.transform;

        Spring spring = springObject.AddComponent<Spring>();
        spring.Initialize(a, b);
        spring.stiffness = structuralStiffness;
        structuralSprings.Add(spring);
    }

    private void CreateSpringIfValid(int i1, int j1, int i2, int j2, List<Spring> springList, string type)
    {
        if (i2 >= 0 && i2 < width && j2 >= 0 && j2 < height)
        {
            Particle a = particleGrid[i1, j1];
            Particle b = particleGrid[i2, j2];

            GameObject springObject = new GameObject($"{type}_Spring_{i1}_{j1}_{i2}_{j2}");
            springObject.transform.parent = this.transform;

            Spring spring = springObject.AddComponent<Spring>();
            spring.Initialize(a, b);
            if (type == "Shear")
            {
                spring.stiffness = shearStiffness;
            }
            else
            {
                spring.stiffness = bendStiffness;
            }
            springList.Add(spring);
        }
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

    public void CreateShearConstraints()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                CreateSpringIfValid(i, j, i + 1, j + 1, shearSprings, "Shear");
                CreateSpringIfValid(i, j, i - 1, j + 1, shearSprings, "Shear");
                CreateSpringIfValid(i, j, i + 1, j - 1, shearSprings, "Shear");
                CreateSpringIfValid(i, j, i - 1, j - 1, shearSprings, "Shear");
            }
        }
    }

    public void CreateBendConstraints()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                CreateSpringIfValid(i, j, i + 2, j, bendSprings, "Bend");
                CreateSpringIfValid(i, j, i - 2, j, bendSprings, "Bend");
                CreateSpringIfValid(i, j, i, j + 2, bendSprings, "Bend");
                CreateSpringIfValid(i, j, i, j - 2, bendSprings, "Bend");
            }
        }
    }

    #endregion
    #region Gizmos
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            foreach (var spring in structuralSprings)
            {
                Gizmos.DrawLine(spring.particle1.currentPosition, spring.particle2.currentPosition);
            }

            Gizmos.color = Color.purple;
            foreach (var spring in shearSprings)
            {
                Gizmos.DrawLine(spring.particle1.currentPosition, spring.particle2.currentPosition);
            }

            Gizmos.color = Color.green;
            foreach (var spring in bendSprings)
            {
                Gizmos.DrawLine(spring.particle1.currentPosition, spring.particle2.currentPosition);
            }
        }
    }

    #endregion
    #region Tools

    // apply forces to each particle 
    void ApplyForces()
    {
        Vector3 gravity = new Vector3(0, -gravityStrength, 0);
        foreach (var particle in particleGrid)
        {
            if(!particle.isPinned)
            {
                particle.ApplyForce(gravity);
            }
        }
    }

    // update particle position if its not pinned
    void SimulateVerlet()
    {
        foreach (var particle in particleGrid)
        {
            if (!particle.isPinned)
            {
                particle.UpdatePosition();
            }
        }
    }

    // Check and enforce constraints for each spring in each spring list,
    // iterate multiple times for stability
    void EnforceConstraints()
    {
        for (int i = 0; i < solverIterations; i++)
        {
            foreach (var spring in structuralSprings)
            {
                spring.ApplyConstraint();
            }

            foreach (var spring in shearSprings)
            {
                spring.ApplyConstraint();
            }

            foreach (var spring in bendSprings)
            {
                spring.ApplyConstraint();
            }
        }
    }

    // if a particle touches the ground, clamp its y position
    void EnforceGroundPosition()
    {
        foreach (var particle in particleGrid)
        {
            particle.EnforceGroundHeight(groundHeight);
        }
    }

    // update world position for each particle
    void UpdateParticleTransforms()
    {
        foreach (var particle in particleGrid)
        {
            particle.transform.position = particle.currentPosition;
        }
    }

    void UnpinEverything()
    {
        foreach (var particle in particleGrid)
        {
            particle.isPinned = false;
        }
    }

    #endregion
}
