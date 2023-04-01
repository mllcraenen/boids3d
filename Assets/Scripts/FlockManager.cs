using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    public GameObject boidPrefab;
    [Range(0, 500)]
    public int boidCount = 20;
    public GameObject[] boids;
    //TODO: This creates a temp movement sphere. Use the walls in the future!
    public Vector3 roomLimit = new Vector3(5, 5, 5);
    public Vector3 goalPos = Vector3.zero;

    [Header("Boid config")]
    [Range(0.0f, 5.0f)]
    public float minSpeed;
    [Range(0.0f, 5.0f)]
    public float maxSpeed;
    [Range(1.0f, 50.0f)]
    public float rotationSpeed;
    [Range(0.0f, 10.0f)]
    public float visionRange;
    [Range(0.0f, 180.0f)]
    public float visionAngle;

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

    [Header("Collision avoidance config")]
    [Range(0, 1000)]
    public int collisionPointCount;
	[Range(0, 2)]
    private float turnFraction = 1.61803399f;

    public Vector3[] collisionPoints;
    [InspectorButton("generateCollisionPoints")]
    public bool clickToGenerateCollisionPoints;

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

    void Start()
    {
        boids = new GameObject[boidCount];
        for(int i = 0; i < boidCount; i++) {
            Vector3 pos = this.transform.position + new Vector3(Random.Range(-roomLimit.x, roomLimit.x),
                                                                Random.Range(-roomLimit.y, roomLimit.y),
                                                                Random.Range(-roomLimit.z, roomLimit.z));
            Quaternion rot = Quaternion.Euler(new Vector3(Random.Range(-180, 180),
                                                                Random.Range(-180, 180),
                                                                Random.Range(-180, 180)));
            boids[i] = Instantiate(boidPrefab, pos, rot);
		}
        boids[0].GetComponent<BoidBehaviour>().isDebugBoid = true;
        goalPos = this.transform.position;
    }
}
