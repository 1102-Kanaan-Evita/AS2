/*using UnityEngine;
using System.Collections.Generic;

public class PotentialFieldSystem : MonoBehaviour
{
    [Header("Field Weights")]
    public float goalAttractionWeight = 1f;
    public float obstacleRepulsionWeight = 2f;
    public float pathAttractionWeight = 1.5f;
    public float unitRepulsionWeight = 1.5f;

    [Header("Path Following")]
    public int pathLookaheadPoints = 3;

    [Header("Obstacle Detection")]
    public LayerMask obstacleLayer;

    public Vector3 CalculateDesiredHeading(Unit unit)
    {
        if (!unit.usePotentialFields)
        {
            return CalculateDirectPathHeading(unit);
        }

        Vector3 totalForce = Vector3.zero;

        totalForce += CalculateGoalAttraction(unit) * goalAttractionWeight;
        totalForce += CalculateObstacleRepulsion(unit) * obstacleRepulsionWeight;
        totalForce += CalculatePathAttraction(unit) * pathAttractionWeight;
        totalForce += CalculateUnitRepulsion(unit) * unitRepulsionWeight;

        if (totalForce.magnitude > 0.01f)
            return totalForce.normalized;
        
        return unit.currentHeading;
    }

    Vector3 CalculateDirectPathHeading(Unit unit)
    {
        if (unit.currentPath == null || unit.currentPathIndex >= unit.currentPath.Count)
            return unit.currentHeading;

        Vector3 targetPosition = unit.currentPath[unit.currentPathIndex].worldPosition;
        Vector3 direction = targetPosition - unit.transform.position;
        
        if (direction.magnitude > 0.01f)
            return direction.normalized;
        
        return unit.currentHeading;
    }

    Vector3 CalculateGoalAttraction(Unit unit)
    {
        if (unit.currentPath == null || unit.currentPathIndex >= unit.currentPath.Count)
            return Vector3.zero;

        Vector3 goalPosition = unit.currentPath[unit.currentPath.Count - 1].worldPosition;
        Vector3 directionToGoal = goalPosition - unit.transform.position;
        
        if (directionToGoal.magnitude < 0.01f)
            return Vector3.zero;

        return directionToGoal.normalized;
    }

    Vector3 CalculateObstacleRepulsion(Unit unit)
    {
        Vector3 repulsionForce = Vector3.zero;
        
        Collider[] obstacles = Physics.OverlapSphere(
            unit.transform.position, 
            unit.obstacleDetectionRadius, 
            obstacleLayer
        );

        foreach (Collider obstacle in obstacles)
        {
            Vector3 directionFromObstacle = unit.transform.position - obstacle.ClosestPoint(unit.transform.position);
            float distance = directionFromObstacle.magnitude;

            if (distance < unit.obstacleRepulsionRadius && distance > 0.01f)
            {
                float repulsionStrength = 1f - (distance / unit.obstacleRepulsionRadius);
                repulsionStrength = Mathf.Pow(repulsionStrength, 2);
                
                repulsionForce += directionFromObstacle.normalized * repulsionStrength;
            }
        }

        return repulsionForce;
    }

    Vector3 CalculatePathAttraction(Unit unit)
    {
        if (unit.currentPath == null || unit.currentPathIndex >= unit.currentPath.Count)
            return Vector3.zero;

        Vector3 attractionForce = Vector3.zero;
        int lookaheadCount = 0;

        int endIndex = Mathf.Min(unit.currentPathIndex + pathLookaheadPoints, unit.currentPath.Count);
        
        for (int i = unit.currentPathIndex; i < endIndex; i++)
        {
            Vector3 waypointPos = unit.currentPath[i].worldPosition;
            Vector3 directionToWaypoint = waypointPos - unit.transform.position;
            
            if (directionToWaypoint.magnitude > 0.01f)
            {
                float weight = 1f / (i - unit.currentPathIndex + 1);
                attractionForce += directionToWaypoint.normalized * weight;
                lookaheadCount++;
            }
        }

        if (lookaheadCount > 0)
            attractionForce /= lookaheadCount;

        return attractionForce;
    }

    Vector3 CalculateUnitRepulsion(Unit unit)
    {
        Vector3 repulsionForce = Vector3.zero;
        
        // Find all units in the scene
        Unit[] allUnits = FindObjectsOfType<Unit>();
        
        foreach (Unit otherUnit in allUnits)
        {
            // Skip self
            if (otherUnit == unit)
                continue;
            
            Vector3 directionFromOther = unit.transform.position - otherUnit.transform.position;
            float distance = directionFromOther.magnitude;
            
            // Check if within repulsion radius
            float combinedRadius = (unit.unitRepulsionRadius + otherUnit.unitRepulsionRadius) * 0.5f;
            
            if (distance < combinedRadius && distance > 0.01f)
            {
                // Calculate repulsion strength (stronger when closer)
                float repulsionStrength = 1f - (distance / combinedRadius);
                repulsionStrength = Mathf.Pow(repulsionStrength, 2);
                
                // Consider both units' repulsion strengths
                float combinedStrength = (unit.unitRepulsionStrength + otherUnit.unitRepulsionStrength) * 0.5f;
                
                repulsionForce += directionFromOther.normalized * repulsionStrength * combinedStrength;
            }
        }
        
        return repulsionForce;
    }
}
*/