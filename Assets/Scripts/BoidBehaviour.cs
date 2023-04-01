using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidBehaviour : MonoBehaviour
{
    /**
     *  Boid flocking requires 3 input forces:
     *  - Separation:   Avoid collision
     *  - Alignment:    Align with average flock heading
     *  - Cohesion:     Move toward average position of flock
     */

    public bool isDebugBoid = false;
    float speed = 1;
    Vector3 velocity = new Vector3();
    static FlockManager config;
    Food target;
    float twoRSquared;
    float visionConeWidth;

    public int fatigue;
    public int hunger;

    //TODO: Factor this out to not be recalculated every frame at some point

    //factored out for gizmo drawing purposes
    RaycastHit hit;

    private void Awake() {
        config = FindObjectOfType<FlockManager>();
        if (config == null) Debug.LogError("FlockManager script not found in the scene.");
        target = FindObjectOfType<Food>(); 
        if (target == null) Debug.LogError("Food not found in the scene.");


        twoRSquared = (2 * Mathf.Pow(config.visionRange, 2));
        visionConeWidth = Mathf.Sqrt(twoRSquared - twoRSquared * Mathf.Cos(config.visionAngle));
    }
	void Start() {
        float startSpeed = (config.minSpeed + config.maxSpeed) / 2;
        velocity = transform.forward * startSpeed;

        if (isDebugBoid) {
            GetComponent<Renderer>().material.SetColor("_Color", Color.red);
        }
    }

	void Update()
    {
        if(Random.Range(0f, 1f) < .01f)
            fatigue++;
        if (Random.Range(0f, 1f) < .01f)
            hunger++;

        calculateNeighbors();
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void calculateNeighbors() {
        GameObject[] neighbors = config.boids;
        Vector3 vCohesion = Vector3.zero;
        Vector3 vSeparation = Vector3.zero;
        Vector3 vAlignment = Vector3.zero;
        Vector3 vTarget = Vector3.zero;
        Vector3 vCollisionAvoidance = steerTowards(getCollisionAvoidanceVector(), config.collisionAvoidance);

        int neighborCount = 0;

        foreach(GameObject neighbor in neighbors) {
            if(neighbor != this) {
                float distance = Vector3.Distance(neighbor.transform.position, this.transform.position);
                if(distance <= config.visionRange) {
                    // Add the vector to the neighbor to be averaged later for the cohesion force
                    vCohesion += neighbor.transform.position - transform.position;
					if (isDebugBoid) {
                        Debug.DrawRay(transform.position, neighbor.transform.position - transform.position, Color.red);
					}
                    // Add the forward direction of the neighbor to be averaged later for the alignment force
                    vAlignment += neighbor.transform.forward;

                    neighborCount++;

                    //TODO: config.separation does not reflect a distance directly. Rewrite this to not be an if, but just scale the separation vector based on distance raised to a power sth sth
                    // If the neighbor is too close, steer away
                    if(distance < config.separation) {
                        vSeparation += (this.transform.position - neighbor.transform.position);
					}
				}
			}
		}

        if (neighborCount > 0) {
            // Calculate average cohesion force from the summed vectors
            vCohesion /= neighborCount;
            vCohesion = steerTowards(vCohesion, config.cohesion);
            // Calculate average alignment force from the summed vectors
            vAlignment /= neighborCount;
            vAlignment = steerTowards(vAlignment, config.alignment);
            // Calculate targeting force
            vTarget = steerTowards((target.transform.position - this.transform.position), config.targeting);

            if(isDebugBoid) {
                Debug.DrawRay(transform.position, vCohesion, Color.magenta);
                Debug.DrawRay(transform.position, vAlignment, Color.blue);
            }

        }

        Vector3 heading = Vector3.zero;
        heading += vSeparation;
        heading += vCohesion;
        heading += vAlignment;
        heading += vCollisionAvoidance;
        heading += vTarget;


        // The previous frame's velocity vector plus the heading calculated above is used 
        velocity += heading * Time.deltaTime;
        speed = velocity.magnitude;
        Vector3 dir = velocity / speed;
        speed = Mathf.Clamp(speed, config.minSpeed, config.maxSpeed);
        velocity = dir * speed;

        transform.position += velocity * Time.deltaTime;
        transform.forward = dir;
    }

    /**
     * Gives a vector steering the boid to the target position. `configMultiplier` is a config variable like the separation strength parameter defined for all boids.
     */
    Vector3 steerTowards(Vector3 target, float configMultiplier = 1) {
        Vector3 v = target.normalized * (config.maxSpeed * configMultiplier);
        return Vector3.ClampMagnitude(v, config.rotationSpeed);
    }

    private Vector3 getCollisionAvoidanceVector() {
        //TODO: probably not use visionRange for collision avoidance but something a bit closer, dont want to start avoiding as soon as you can see something that might be a bit much.
        if (Physics.SphereCast(transform.position, visionConeWidth / 2, transform.forward, out hit, config.visionRange)) {
            return velocity + (hit.normal * 2);
        }
        else return transform.forward;
    }

	private void OnDrawGizmos() {
        //config = FindObjectOfType<FlockManager>();
        //DebugPainter.drawPoint(hit.point, 0.1f, Color.red);
        //DebugPainter.drawPoint(transform.position, 0.05f, Color.black);
    }
}