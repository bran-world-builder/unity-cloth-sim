using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.ParticleSystem;

public class ClothSimulator : MonoBehaviour
{
    [Header("Global Sim Settings")]
    public int solverIterations = 15;
    public float groundHeight = -3f;
    public GameObject groundPlane;
    public float gravityStrength = 9.81f;
    public float yOffset = 1.0f;
    public float clickForceY = 100000f;
    public float clickForceZ = 150000f;
    public enum ForceMode { Force, Hit } // Force is Newtonian, Hit is PBD
    public ForceMode forceMode = ForceMode.Force;

    [Header("Spring Toggles")]
    public bool structuralToggle = true;
    public bool shearToggle = true;
    public bool bendToggle = true;

    [Header("Cloth Settings")]
    public int width = 10;
    public int height = 10;
    public float spacing = 0.5f;
    [Range(0.001f, 0.1f)]
    public float structuralStiffness = 0.05f;
    [Range(0f, 0.003f)]
    public float shearStiffness = 0.00005f;
    [Range(0f, 0.01f)]
    public float bendStiffness = 0.005f;
    public Transform clothRootTransform;
    public GameObject particlePrefab;

    [Header("Wind Settings")]
    public Vector3 windDirection = new Vector3(1f, 0f, 0f);
    public float windStrength = 0.5f;
    public bool oscillateWind = false;
    public float windOscillationSpeed = 1f;

    [Header("Sliders")]
    public Slider windStrengthSlider;
    public Slider oscillationSpeedSlider;
    public Slider forceYSlider;
    public Slider forceZSlider;

    [Header("Mesh Generation")]
    public GameObject clothMeshObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh clothMesh;

    private Vector3[] meshVertices;
    private int[] meshTriangles;


    Particle[,] particleGrid;
    private List<Spring> structuralSprings = new List<Spring>();
    private List<Spring> shearSprings = new List<Spring>();
    private List<Spring> bendSprings = new List<Spring>();

    private RaycastHit hit;

    public void Start()
    {
        // Set listeners for updates in slider values
        windStrengthSlider.onValueChanged.AddListener((value) =>
        {
            windStrength = value;
        });

        oscillationSpeedSlider.onValueChanged.AddListener((value) =>
        {
            windOscillationSpeed = value;
        });
        
        forceYSlider.onValueChanged.AddListener((value) =>
        {
            clickForceY = value;
        });
        
        forceZSlider.onValueChanged.AddListener((value) =>
        {
            clickForceZ = value;
        });
        
        BuildCloth();
        GenerateClothMesh();
    }

    public void FixedUpdate()
    {
        SetGroundPos();
        
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

        // Toggle wind
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (!oscillateWind)
            {
                oscillateWind = true;
            }
            else
            {
                oscillateWind = false;
            }
        }

        // Change force mode
        if (Input.GetKeyDown(KeyCode.M))
        {
            forceMode = forceMode == ForceMode.Force ? ForceMode.Hit : ForceMode.Force;
            Debug.Log($"Force mode: " + forceMode);
        }

        // Call reset
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCloth();
        }

        // Toggle structural constraints with "1"
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (!structuralToggle)
            {
                structuralToggle = true;
            }
            else
            {
                structuralToggle = false;
            }
        }

        // Toggle shear constraints with "2"
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (!shearToggle)
            {
                shearToggle = true;
            }
            else
            {
                shearToggle = false;
            }
        }

        // Toggle bend constraints with "3"
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (!bendToggle)
            {
                bendToggle = true;
            }
            else
            {
                bendToggle = false;
            }
        }

        // Apply force to single particle
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int interactionMask = LayerMask.GetMask("ClothInteraction");

            if (Physics.Raycast(ray,out hit))
            {
                Vector3 clickPoint = hit.point;
                Particle closest = FindClosestParticle(clickPoint);
                Debug.Log($"Ray hit at: {hit.point}");
                Debug.Log($"Closest Particle: {closest.name}");
                if (closest != null)
                {
                    Vector3 clickForce = new Vector3(0f, clickForceY, clickForceZ) * Time.fixedDeltaTime;
                    
                    if (forceMode == ForceMode.Force)
                    {
                        // Apply force to particle
                        closest.ApplyForce(clickForce);
                    }
                    if (forceMode == ForceMode.Hit)
                    {
                        // Directly affect position, position based dynamics
                        closest.currentPosition += new Vector3(0, 0.2f, 0.2f);
                    }
                }
            }
        }

        // Apply force to particles in an area
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int interactionMask = LayerMask.GetMask("ClothInteraction");

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 clickPoint = hit.point;
                Debug.Log($"Ray hit at: {hit.point}");
                ApplyForceInRadius(clickPoint, 1.5f, 5f);
            }
        }    
    }

    // Final call of updates before frame refresh
    private void LateUpdate()
    {
        UpdateClothMesh();
    }


    #region Builders

    // Programatically build cloth
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
        CreateShearConstraints();
        CreateBendConstraints();
    }

    // create standard spring
    private void CreateSpring(Particle a, Particle b)
    {
        GameObject springObject = new GameObject("Spring_" + a.name + "_" + b.name);
        springObject.transform.parent = this.transform;

        Spring spring = springObject.AddComponent<Spring>();
        spring.Initialize(a, b);
        spring.stiffness = structuralStiffness;
        structuralSprings.Add(spring);
    }

    // check spring validity, then create spring if valid
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

    // Create structural, particle and up, down, left, right neighbors
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

    // Create shear constraints, particle and diagonal all neighbors
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

    // Create bend constraints, particle and particle + 2 neighbor in all directions
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

    public void GenerateClothMesh()
    {
        meshFilter = clothMeshObject.GetComponent<MeshFilter>();
        meshRenderer = clothMeshObject.GetComponent<MeshRenderer>();

        clothMesh = new Mesh();
        clothMesh.name = "Cloth Mesh";

        meshFilter.mesh = clothMesh;

        int vertsPerRow = width;
        int vertsPerColumn = height;
        int vertexCount = vertsPerRow * vertsPerColumn;
        int quadCount = (vertsPerRow - 1) * (vertsPerColumn - 1);
        int triangleCount = quadCount * 2;

        meshVertices = new Vector3[vertexCount];
        meshTriangles = new int[triangleCount * 3];

        // initial vertex positions
        for (int j  = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int index = j * width + i;
                meshVertices[index] = particleGrid[i, j].currentPosition;
            }
        }

        // create triangles
        int t = 0;
        for (int j = 0; j < height - 1; j++)
        {
            for (int i = 0; i < width - 1; i++)
            {
                int topLeft = j * width + i;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + width;
                int bottomRight = bottomLeft + 1;

                // first triangle
                meshTriangles[t++] = topLeft;
                meshTriangles[t++] = bottomLeft;
                meshTriangles[t++] = topRight;

                // second triangle
                meshTriangles[t++] = topRight;
                meshTriangles[t++] = bottomLeft;
                meshTriangles[t++] = bottomRight;
            }
        }

        clothMesh.vertices = meshVertices;
        clothMesh.triangles = meshTriangles;
        clothMesh.RecalculateNormals();
    }

    #endregion
    #region Gizmos

    // draw Gizmos for testing
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

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(hit.point, 0.1f);
        }
    }

    #endregion
    #region Tools

    // apply forces to each particle 
    void ApplyForces()
    {
        Vector3 gravity = new Vector3(0, -gravityStrength, 0);
        Vector3 wind = windDirection.normalized * windStrength;

        if (oscillateWind)
        {
            float t = Mathf.Sin(Time.time * windOscillationSpeed);
            wind *= t;
        }

        foreach (var particle in particleGrid)
        {
            if(!particle.isPinned)
            {
                particle.ApplyForce(gravity);
                particle.ApplyForce(wind);
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
            if (structuralToggle)
            {
                foreach (var spring in structuralSprings)
                {
                    spring.ApplyConstraint();
                }
            }
            
            if (shearToggle)
            {
                foreach (var spring in shearSprings)
                {
                    spring.ApplyConstraint();
                }
            }
            
            if (bendToggle)
            { 
                foreach (var spring in bendSprings)
                {
                    spring.ApplyConstraint();
                }
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

    // Drop the cloth
    void UnpinEverything()
    {
        foreach (var particle in particleGrid)
        {
            particle.isPinned = false;
        }
    }

    // Update ground position based on sim parameters
    void SetGroundPos()
    {
        Vector3 currentGroundPos = groundPlane.transform.position;
        currentGroundPos.y = groundHeight;
        groundPlane.transform.position = currentGroundPos;
    }

    // Find closest particle to apply force to
    private Particle FindClosestParticle(Vector3 point)
    {
        Particle closest = null;
        float closestDistance = float.MaxValue;

        foreach (var particle in particleGrid)
        {
            float dist = Vector3.Distance(particle.currentPosition, point);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = particle;
            }
        }

        return closest;
    }

    // Apply force over a group of particles
    private void ApplyForceInRadius(Vector3 center, float radius, float maxForce)
    {
        foreach (var particle in particleGrid)
        {
            float dist = Vector3.Distance(particle.currentPosition, center);
            if (dist < radius)
            {
                Vector3 forceDirection = new Vector3(0f, clickForceY, clickForceZ) * Time.fixedDeltaTime;
                float strength = Mathf.Lerp(maxForce, 0, dist/ radius);
                particle.ApplyForce(forceDirection * strength);
            }
        }
    }

    // Reset the simulation
    public void ResetCloth()
    {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Particle particle = particleGrid[i, j];
                Vector3 startPos = new Vector3(i * spacing, j * spacing + yOffset, 0);
                particle.currentPosition = startPos;
                particle.previousPosition = startPos;
                particle.ResetAcceleration();
                particle.isPinned = (j == height - 1 && (i == 0 || i == width -1));
            }
        }
    }

    // Update cloth mesh to match particle movements
    private void UpdateClothMesh()
    {
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int index = j * width + i;
                meshVertices[index] = particleGrid[i, j].currentPosition;
            }
        }

        clothMesh.vertices = meshVertices;
        clothMesh.RecalculateNormals();
        clothMesh.RecalculateBounds();
    }

    #endregion

    #region UI Functionality

    public void DropClothUI()
    {
        UnpinEverything();
    }

    public void ResetClothUI()
    {
        ResetCloth();
    }

    public void InteractionModeUI()
    {
        forceMode = forceMode == ForceMode.Force ? ForceMode.Hit : ForceMode.Force;
    }

    public void WindOscillationUI()
    {
        if (!oscillateWind)
        {
            oscillateWind = true;
        }
        else
        {
            oscillateWind = false;
        }
    }

    #endregion
}
