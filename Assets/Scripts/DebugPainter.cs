using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DebugPainter
{

    public static void drawPoint(Vector3 pos, float radius, Color color) {
        Gizmos.color = color;
        Gizmos.DrawSphere(pos, radius);
    }

    public static void drawDebugArrow(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f) {
        Debug.DrawRay(pos, direction, color);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Debug.DrawRay(pos + direction, right * arrowHeadLength, color);
        Debug.DrawRay(pos + direction, left * arrowHeadLength, color);
    }

    public static void drawGizmoArrow(Vector3 pos, Vector3 direction, Color color, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f) {
        Gizmos.color = color;
        Gizmos.DrawRay(pos, direction);

        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
        Gizmos.DrawRay(pos + direction, right * arrowHeadLength);
        Gizmos.DrawRay(pos + direction, left * arrowHeadLength);
    }


    //private void OnDrawGizmos() {
    //	FlockManager f;
    //	for(int i = 0; i < f.collisionPointCount; i++) {
    //		DebugPainter.drawPoint(transform.position + new Vector3(x, y, z), 0.05f, Color.white);
    //	}
    //}

    //private void OnDrawGizmos() {

    //}
}
