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
    public float turnFraction = 1.61803399f;
    float speed = 1;
    Vector3 velocity = new Vector3();
    static FlockManager config;
    Food target;

	private void Awake() {
        config = FindObjectOfType<FlockManager>();
        if (config == null) Debug.LogError("FlockManager script not found in the scene.");
        target = FindObjectOfType<Food>(); 
        if (target == null) Debug.LogError("Food not found in the scene.");
    }
	void Start() {
        float startSpeed = (config.minSpeed + config.maxSpeed) / 2;
        velocity = transform.forward * startSpeed;

        if (isDebugBoid) {
            GetComponent<Renderer>().material.SetColor("_Color", Color.magenta);
        }
    }

	void Update()
    {
        //applyForces();
        resetSpeed();
        calculateNeighbors();
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    void calculateNeighbors() {
        GameObject[] neighbors = config.boids;
        Vector3 vCohesion = Vector3.zero;
        Vector3 vSeparation = Vector3.zero;
        Vector3 vAlignment = Vector3.zero;
        Vector3 vTarget = Vector3.zero;

        float groupSpeed = 0.01f;
        int neighborCount = 0;

        foreach(GameObject neighbor in neighbors) {
            if(neighbor != this) {
                float distance = Vector3.Distance(neighbor.transform.position, this.transform.position);
                if(distance <= config.visionRange) {
                    // Add the location of the neighbor to be averaged later for the cohesion force
                    vCohesion += neighbor.transform.position;
                    vAlignment += neighbor.transform.forward;

                    neighborCount++;

                    //TODO: config.separation does not reflect a distance directly. Rewrite this to not be an if, but just scale the separation vector based on distance raised to a power sth sth
                    // If the neighbor is too close, steer away
                    if(distance < config.separation) {
                        vSeparation += (this.transform.position - neighbor.transform.position);
					}

                    // Get the speed to be averaged later to match group speed
                    BoidBehaviour otherBoid = neighbor.GetComponent<BoidBehaviour>();
                    groupSpeed += otherBoid.speed;
				}
			}
		}

        //	// Avoidance vector
        Vector3 vCollisionAvoidance = Vector3.zero;
        if (isDebugBoid) {
            Debug.DrawRay(transform.position, transform.forward * 5, Color.green);
            Vector3 cavDir = onCollisionCourse();
            if (onCollisionCourse() != transform.forward) {
                Debug.DrawRay(transform.position, cavDir.normalized * 5, Color.red);
                vCollisionAvoidance = steerTowards(cavDir, config.collisionAvoidance);
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
        }

        Vector3 heading = Vector3.zero;
        //acceleration += forwardVector;
        heading += vSeparation;
        heading += vCohesion;
        heading += vAlignment;
        heading += vCollisionAvoidance;
        heading += vTarget;

        Debug.DrawRay(transform.position, vCollisionAvoidance * 10, Color.red);
        Debug.DrawRay(transform.position, vSeparation + vCohesion + vAlignment, Color.yellow);

        // The previous frame's velocity vector plus the heading calculated above is used 
        velocity += heading * Time.deltaTime;
        speed = velocity.magnitude;
        Vector3 dir = velocity / speed;
        speed = Mathf.Clamp(speed, config.minSpeed, config.maxSpeed);
        velocity = dir * speed;

        transform.position += velocity * Time.deltaTime;
        Debug.DrawRay(transform.position, velocity, Color.blue);
        transform.forward = dir;


        //Vector3 heading = (vSeparation + vAlignment + vCohesion + vTarget + vCollisionAvoidance) - transform.position;
        //if (heading.magnitude > 0.001) {
        //    Debug.DrawRay(transform.position, heading, Color.blue);
        //    transform.rotation = Quaternion.Slerp(transform.rotation,
        //        Quaternion.LookRotation(heading),
        //        config.rotationSpeed * Time.deltaTime);
        //}
    }


    Vector3 steerTowards(Vector3 target) {
        Vector3 v = target.normalized * config.maxSpeed;
        return Vector3.ClampMagnitude(v, config.rotationSpeed);
    }
    /**
     * Gives a vector steering the boid to the target position. `configMultiplier` is a config variable like the separation strength parameter defined for all boids.
     */
    Vector3 steerTowards(Vector3 target, float configMultiplier) {
        Vector3 v = target.normalized * (config.maxSpeed * configMultiplier);
        return Vector3.ClampMagnitude(v, config.rotationSpeed);
    }

    private Vector3 onCollisionCourse() {
        RaycastHit hit;
        float furthestUnobstructedDst = 0;
        Vector3 bestDir = transform.forward;

        //TODO: Factor this out to not be recalculated every frame at some point
        float twoRSquared = (2 * Mathf.Pow(config.visionRange, 2));
        float visionConeWidth = Mathf.Sqrt(twoRSquared - twoRSquared * Mathf.Cos(config.visionAngle));

        int visionCutoffPoint = (int) ((config.visionAngle / 180) * config.collisionPointCount);
        for (int i = 0; i < config.collisionPointCount; i++) {
            if (i <= visionCutoffPoint) {
                Vector3 dir = transform.TransformDirection(config.collisionPoints[i]);
                if (Physics.SphereCast(transform.position, visionConeWidth, dir, out hit, config.visionRange)) {
                    if (hit.distance > furthestUnobstructedDst) {
                        bestDir = dir;
                        furthestUnobstructedDst = hit.distance;
                    }
                }
                else return dir;
            }
        }
        return bestDir;
    }

    private Vector3 getObstacleAvoidanceVector() {
        float twoRSquared = (2 * Mathf.Pow(config.visionRange, 2));
        float visionConeWidth = Mathf.Sqrt(twoRSquared - twoRSquared * Mathf.Cos(config.visionAngle));
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, visionConeWidth, transform.forward, config.visionRange);
        //RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, visionConeWidth, transform.up, config.visionRange, 1 << config.ObstacleLayer);
        Vector3 sumVector = new Vector3();
        for (int i = 0; i < hits.Length; i++) {
            Vector3 v = (transform.position - new Vector3(hits[i].point.x, hits[i].point.y, 0));
            // Set the magnitude of the vector to be the hit distance fraction of the max distance (RaycastHit2D.fraction is 1 at max distance).
            v = v.normalized; //* (1 - hits[i].fraction)
            sumVector += v;

            if (isDebugBoid) {
                Debug.Log("drawing line..");
                Debug.DrawLine(transform.position, hits[i].point, new Color(255, 0, 0, 50));
            }
        }

        return sumVector;
    }

    //TODO:: Make this not necessary this is dumb.
    void resetSpeed() {
        if(Random.Range(0,100) < 10)
            speed = Random.Range(config.minSpeed, config.maxSpeed);
    }

}