using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    public GameObject boidPrefab;
    public ComputeShader boidCompute;
	const int threadGroupSize = 1024; // <-- check this when you are more awake, also the dispatch method. What is the optimal thread group size?
	private BoidData[] boidData;

    [Range(0, 500)]
    public int boidCount = 20;
    public GameObject[] boidObjects;
    public BoidBehaviour[] boids;
    //TODO: This creates a temp movement sphere. Use the walls in the future!
    public Vector3 spawnSpace = new Vector3(5, 5, 5);

    [Header("Boid config")]
    [Range(1.0f, 15.0f)]
    public float minSpeed;
    [Range(1.0f, 15.0f)]
    public float maxSpeed;
    [Range(1.0f, 50.0f)]
    public float maxSteerForce;
    [Range(0.0f, 10.0f)]
    public float visionRange;
    // Used for checking whether neighbours are in the vision cone. The dot product of the boid's forward vector and the vector to the neighbour must be greater than this value.
    // 1.0f means the neighbour is directly in front of the boid, -1.0f means directly behind. 0.0f means the neighbour is to the side.
    [Range(-1.0f, 1.0f)]
    public float visionAngleDotProduct;

	[Header("Flocking config")]
    [Range(0.0f, 15.0f)]
    public float separation;
    [Range(0.0f, 15.0f)]
    public float alignment;
    [Range(0.0f, 15.0f)]
    public float cohesion;
    [Range(0.0f, 15.0f)]
    public float targeting;
	[Range(0.0f, 15.0f)]
	public float collisionAvoidance;
	[Range(0.0f, 15.0f)]
	public float collisionAvoidDst;
    //The radius of the sphere in spherecast used to avoid collisions
	[Range(0.0f, 5.0f)]
	public float collisionAvoidBounds;


	[Header("Collision avoidance config")]
    [Range(0, 1000)]
    public readonly int collisionPointCount = 250;
	[Range(0, 2)]
    private float turnFraction = 1.61803399f;
    [HideInInspector]
    public Vector3[] collisionPoints;
    //[InspectorButton("generateCollisionPoints")]
    //public bool clickToGenerateCollisionPoints;

    private void generateCollisionPoints() {
        collisionPoints = new Vector3[collisionPointCount];
        for (int i = 0; i < collisionPointCount; i++) {
            float t = i / (collisionPointCount - 1f);
            float inclination = Mathf.Acos(1 - 2 * t);
            float azimuth = 2 * Mathf.PI * turnFraction * i;

            float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
            float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
            float z = Mathf.Cos(inclination);
            
            collisionPoints[i] = new Vector3(x, y, z);
        }
    }

    FlockManager() {
        generateCollisionPoints();
    }

	private void Awake() {
		// Init the boid data array for the compute shader
		boidData = new BoidData[boidCount];
	}

	void Start()
    {
		boidObjects = new GameObject[boidCount];
        boids = new BoidBehaviour[boidCount];
        for(int i = 0; i < boidCount; i++) {
            Vector3 pos = this.transform.position + new Vector3(Random.Range(-spawnSpace.x, spawnSpace.x),
                                                                Random.Range(-spawnSpace.y, spawnSpace.y),
                                                                Random.Range(-spawnSpace.z, spawnSpace.z));
            Quaternion rot = Quaternion.Euler(new Vector3(Random.Range(-180, 180),
                                                                Random.Range(-180, 180),
                                                                Random.Range(-180, 180)));
			boidObjects[i] = Instantiate(boidPrefab, pos, rot);
			boidObjects[i].name = "Boid " + i;
            boids[i] = boidObjects[i].GetComponent<BoidBehaviour>();
		}
		boids[0].isDebugBoid = true;
    }

	//private void Update() {
	//	if(boids != null) {
 //           for(int i = 0; i < boidCount; i++) {
 //               boidData[i].pos = boids[i].transform.position;
 //               boidData[i].dir = boids[i].transform.forward;
	//		}
 //           var boidBuffer = new ComputeBuffer(boidCount, BoidData.size);
 //           boidBuffer.SetData(boidData);
 //           boidCompute.SetBuffer(0, "boids", boidBuffer);
 //           boidCompute.SetInt("numBoids", boidCount);
 //           boidCompute.SetFloat("viewRadius", visionRange);
 //           //TODO:: Separation does not yet use steerTowards, nor does it have a set evadeRadius.
 //           //// This will cause problems, fix that first and probs make the below parameter evadeRadius instead of separation
 //           boidCompute.SetFloat("evadeRadius", separation);

	//		int threadGroups = Mathf.CeilToInt(boidCount / (float) threadGroupSize);
	//		boidCompute.Dispatch(0, threadGroups, 1, 1);
	//		boidBuffer.GetData(boidData);

 //           for(int i = 0; i < boidCount; i++) {
 //               boids[i].transform.position = boidData[i].pos;
 //               boids[i].transform.forward = boidData[i].dir;
 //               boids[i].flockHeading = boidData[i].flockHeading;
 //               boids[i].flockCenter = boidData[i].flockCenter;
 //               boids[i].separationHeading = boidData[i].flockEvasion;
 //               boids[i].flockmates = boidData[i].flockmates;

 //               boids[i].updateBoid();
	//		}

	//		boidBuffer.Release();
	//	}
	//}

}

public struct BoidData {
    public Vector3 pos;
    public Vector3 dir;
    public Vector3 flockHeading;
    public Vector3 flockCenter;
    public Vector3 flockEvasion;
    public int flockmates;
    public static int size = sizeof(float) * 3 * 5 + sizeof(int);
}
