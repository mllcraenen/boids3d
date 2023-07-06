using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BoidBehaviour : MonoBehaviour
{
    public bool isDebugBoid = false;
    float speed = 1;
    Vector3 velocity = new Vector3();
    static FlockManager config;
    Food target;

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
        target = FindObjectOfType<Food>(); 
        if (target == null) Debug.LogError("Food not found in the scene.");
    }
	void Start() {
        float startSpeed = (config.minSpeed + config.maxSpeed) / 2;
        velocity = transform.forward * startSpeed;

        if (isDebugBoid) {
            GetComponentInChildren<Renderer>().material.SetColor("_Color", Color.black);
        } else GetComponentInChildren<Renderer>().material.SetColor("_Color", new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)));
    }

	void Update()
    {
        if(Random.Range(0f, 1f) < .01f)
            fatigue++;
        if (Random.Range(0f, 1f) < .01f)
            hunger++;

		updateBoid();
    }

    public void updateBoid() {
        /**/
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
		/**/

		if (neighborCount > 0) {
			vCohesion /= neighborCount;
			vCohesionGizmoLoc = vCohesion;
			vCohesion= steerTowards(vCohesion) * config.cohesion;
			vAlignment /= neighborCount;
			vAlignment = steerTowards(vAlignment) * config.alignment;
			/*
            if(isDebugBoid) {
                Debug.DrawRay(transform.position, vCohesion, Color.magenta);
                Debug.DrawRay(transform.position, vAlignment, Color.blue);
                Debug.DrawRay(transform.position, vSeparation, Color.green);
                Debug.DrawRay(transform.position, vTarget, Color.red);
            }
            */
		}

		//if (flockmates > 0) {
		//	flockCenter /= flockmates;
		//	vCohesionGizmoLoc = flockCenter;
		//	flockCenter = steerTowards(flockCenter) * config.cohesion;
		//	flockHeading /= flockmates;
		//	flockHeading = steerTowards(flockHeading) * config.alignment;
		//	/*
  //          if(isDebugBoid) {
  //              Debug.DrawRay(transform.position, vCohesion, Color.magenta);
  //              Debug.DrawRay(transform.position, vAlignment, Color.blue);
  //              Debug.DrawRay(transform.position, vSeparation, Color.green);
  //              Debug.DrawRay(transform.position, vTarget, Color.red);
  //          }
  //          */
		//}

		Vector3 targetHeading = steerTowards((target.transform.position - transform.position)) * config.targeting;

		Vector3 heading = Vector3.zero;
		if (LagueIsHeadingForCollision()) {
            Vector3 collisionAvoidDir = LagueObstacleRays();
            Vector3 collisionAvoidForce = steerTowards(collisionAvoidDir) * config.collisionAvoidance;
            heading += collisionAvoidForce;
        }

        heading += vCohesion;
        heading += vAlignment;
        heading += vSeparation;
        //heading += separationHeading;
        //heading += flockHeading;
        //heading += flockCenter;
        heading += targetHeading;

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
    Vector3 steerTowards(Vector3 target) {
        Vector3 v = target.normalized * config.maxSpeed;
        return Vector3.ClampMagnitude(v, config.maxSteerForce);
    }

	bool LagueIsHeadingForCollision() {
        RaycastHit hit;
        return Physics.SphereCast(transform.position, config.collisionAvoidBounds, transform.forward, out hit, config.collisionAvoidDst);
    }

    Vector3 LagueObstacleRays() {
        Vector3[] rayDirections = config.collisionPoints;
        for (int i = 0; i < rayDirections.Length; i++) {
            // use cached transform here? lague does, it's the previous frame's transform
            Vector3 dir = transform.TransformDirection(rayDirections[i]);
            Ray ray = new Ray(transform.position, dir);
            if (!Physics.SphereCast(ray, config.collisionAvoidBounds, config.collisionAvoidDst)) {
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
}