using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BoidBehaviour : MonoBehaviour { 

	public bool isDebugBoid = false;
    float speed = 1;
    Vector3 velocity = new Vector3();
    static FlockManager config;
    GameObject target;
	RaycastHit hit;

	[HideInInspector]
    public Vector3 flockHeading;
    [HideInInspector]
    public Vector3 flockCenter;
    [HideInInspector]
    public Vector3 separationHeading;
    [HideInInspector]
    public int flockmates = 0;
	Vector3 vCohesionGizmoLoc = Vector3.zero;

	public int fatigue;
    public int hunger;

    private void Awake() {
        config = FindObjectOfType<FlockManager>();
        if (config == null) Debug.LogError("FlockManager script not found in the scene.");
        target = FindObjectOfType<Food>().gameObject; 
        if (target == null) Debug.LogError("Food not found in the scene.");
    }
	void Start() {
        float startSpeed = (config.boidSettings.minSpeed + config.boidSettings.maxSpeed) / 2;
        velocity = transform.forward * startSpeed;

        if (isDebugBoid) {
            GetComponentInChildren<Renderer>().material.SetColor("_Color", Color.black);
        } else GetComponentInChildren<Renderer>().material.SetColor("_Color", new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
    }

	void Update() {
        if(Random.Range(0f, 1f) < .03f)
            fatigue++;
        if (Random.Range(0f, 1f) < config.boidSettings.hungerRate)
            hunger++;
    }

    public void updateBoid() {
		/*
        GameObject[] neighbors = config.boidObjects;
        Vector3 vCohesion = Vector3.zero;
        Vector3 vSeparation = Vector3.zero;
        Vector3 vAlignment = Vector3.zero;

        int neighborCount = 0;

        foreach (GameObject neighbor in neighbors) {
            if (neighbor != this.gameObject) {
                float distance = Vector3.Distance(neighbor.transform.position, this.transform.position);
                if (distance <= config.visionRange && Vector3.Dot(this.transform.forward, neighbor.transform.position - this.transform.position) > config.visionAngleDotProduct) {
                    vCohesion += neighbor.transform.position - transform.position;
                    vAlignment += neighbor.transform.forward;
                    neighborCount++;

                    //TODO: config.separation does not reflect a distance directly. Rewrite this to not be an if, but just scale the separation vector based on distance raised to a power sth sth
                    if (distance < config.separation) {
                        vSeparation += (this.transform.position - neighbor.transform.position);
                    }
                }
            }
        }
        
		//if (neighborCount > 0) {
		//	vCohesion /= neighborCount;
		//	vCohesionGizmoLoc = vCohesion;
		//	vCohesion= steerTowards(vCohesion) * config.cohesion;
		//	vAlignment /= neighborCount;
		//	vAlignment = steerTowards(vAlignment) * config.alignment;
		//}
		*/

		if (flockmates > 0) {
            flockCenter /= flockmates;
            vCohesionGizmoLoc = flockCenter;
            flockCenter = steerTowards(flockCenter - transform.position) * config.flockingSettings.cohesion;
            flockHeading /= flockmates;
            flockHeading = steerTowards(flockHeading) * config.flockingSettings.alignment;
            
            if(isDebugBoid) {
                Debug.DrawRay(transform.position, flockCenter, Color.magenta);
                Debug.DrawRay(transform.position, flockHeading, Color.blue);
                Debug.DrawRay(transform.position, separationHeading, Color.green);
            }

        }

        Vector3 targetHeading = steerTowards((target.transform.position - transform.position)) * config.flockingSettings.targeting;

        //TODO:: Make the hunger threshold configurable and move the calculation to the compute shader SOMEHOW
        Vector3 foodHeading = getHungerHeading();

        Vector3 heading = Vector3.zero;
		if (LagueIsHeadingForCollision()) {
            /*
             * Alternative approach recommended by Reddit. This is not working as intended, but I'm keeping it here for reference. Might be useful later to increase performance instead of Spherecasting lots per boid per frame.
            //Vector3 collisionAvoidDir = hit.normal - velocity;
            //float distance = Vector3.Distance(hit.point, transform.position);
            //Vector3 collisionAvoidForce = steerTowards(collisionAvoidDir) * config.collisionAvoidance * (1 / distance );
            //heading += collisionAvoidForce;
            */
            Vector3 collisionAvoidDir = LagueObstacleRays();
            heading = steerTowards(collisionAvoidDir) * config.flockingSettings.collisionAvoidance;

		}
        /*
         * For local direction computation without compute shader
		//heading += vCohesion;
		//heading += vAlignment;
		//heading += vSeparation;
		//if(isDebugBoid) {
		//    Debug.Log("Hdg: " + flockHeading);
		//    Debug.Log("Ctr: " + flockCenter);
		//}*/
		heading += separationHeading;
        heading += flockHeading;
        heading += flockCenter;
        heading += targetHeading;
        heading += foodHeading;

		// The previous frame's velocity vector plus the heading calculated above is used
		velocity += heading * Time.deltaTime;
        speed = velocity.magnitude;
        Vector3 dir = velocity / speed;
        speed = Mathf.Clamp(speed, config.boidSettings.minSpeed, config.boidSettings.maxSpeed);
        velocity = dir * speed;

        transform.position += velocity * Time.deltaTime;
        transform.forward = dir;
    }

    /**
     * Gives a vector steering the boid to the target position. `configMultiplier` is a config variable like the separation strength parameter defined for all boids.
     */
    Vector3 steerTowards(Vector3 target) {
        Vector3 v = target.normalized * config.boidSettings.maxSpeed;
        return Vector3.ClampMagnitude(v, config.boidSettings.maxSteerForce);
    }

	bool LagueIsHeadingForCollision() {
        //return Physics.Raycast(transform.position, transform.forward, config.flockingSettings.collisionAvoidDst, config.boidSettings.obstacleMask);
        return Physics.SphereCast(transform.position, config.flockingSettings.collisionAvoidBounds, transform.forward, out hit, config.flockingSettings.collisionAvoidDst, config.boidSettings.obstacleMask);
    }

    Vector3 LagueObstacleRays() {
        Vector3[] rayDirections = config.collisionPoints;
        for (int i = 0; i < rayDirections.Length; i++) {
            // use cached transform here? lague does, it's the previous frame's transform
            Vector3 dir = transform.TransformDirection(rayDirections[i]);
            Ray ray = new Ray(transform.position, dir);
            if (!Physics.SphereCast(ray, config.flockingSettings.collisionAvoidBounds, config.flockingSettings.collisionAvoidDst, config.boidSettings.obstacleMask)) {
                return dir;
            }
        }
        return transform.forward;
    }


    private void OnDrawGizmos() {
        if(isDebugBoid) {
            // Draw the flock center (vCohesion)
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(transform.position + vCohesionGizmoLoc, 0.1f);
        }
    }

    private Vector3 getHungerHeading() {
        Vector3 foodHeading = Vector3.zero;
		if (hunger > 50) {
			// steer towards nearest food with a weight of hunger / 100
			float nearestFoodDistance = Mathf.Infinity;
			Food nearestFood = null;
			for (int i = 0; i < config.foods.Length; i++) {
				if (config.foods[i] != null) {
					float distance = Vector3.Distance(config.foods[i].transform.position, transform.position);
					if (distance < nearestFoodDistance) {
						nearestFoodDistance = distance;
						nearestFood = config.foods[i];
					}
					//TODO: Make the "eating" distance configurable as well
					if (distance < config.boidSettings.foodConsumptionRadius) {
						hunger = 0;
                        nearestFood.foodAmount--;
                        if (nearestFood.foodAmount <= 0) {
                            config.genNewFood(nearestFood);
						}
						break;
					}
				}
			}
			if (nearestFood != null) {
                float hungerFactor = hunger * hunger / config.boidSettings.hungerCoefficient;
                float distanceFactor = 1 / (nearestFoodDistance * nearestFoodDistance * config.boidSettings.distanceToFoodCoefficient);
				foodHeading = steerTowards(nearestFood.transform.position - transform.position) * hungerFactor * distanceFactor;
				if (isDebugBoid) {
					Debug.DrawRay(transform.position, foodHeading, Color.yellow);
                    //Debug.Log("Hunger: " + hunger + " hFactor: " + hungerFactor + " dFactor: " + distanceFactor);
				}
			}
		}
        return foodHeading;
	}
}