using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EntityUnit : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float maxForce = 10f;
    public float arrivalRadius = 0.5f;
    public float waypointRadius = 0.3f;
    public float rotationSpeed = 5f;
    public bool debugMovement = false;
    
    [Header("Advanced")]
    public bool useAdaptiveWaypointRadius = true;
    
    [Header("Potential Fields")]
    public float obstacleRepulsionRange = 5f;
    public float obstacleRepulsionStrength = 10f;
    public float entityRepulsionRange = 3f;
    public float entityRepulsionStrength = 4f;
    public float targetAttractionStrength = 2f;
    public float personalSpaceRadius = 1.5f;
    
    [Header("Visual")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;
    public LayerMask obstacleLayer;
    public LayerMask entityLayer;
    
    private Vector3 velocity = Vector3.zero;
    private Vector3 heading = Vector3.forward;
    private bool isSelected = false;
    private LineRenderer lineRenderer;
    private Renderer unitRenderer;
    
    private Vector3[] currentPath;
    private int currentWaypointIndex = 0;
    private bool isMoving = false;
    private MovementMode currentMovementMode;
    private List<Vector3> waypointChain = new List<Vector3>();
    private int currentChainIndex = 0;
    
    private EntityUnit followTarget = null;
    private Vector3 followOffset = Vector3.zero;
    private float followUpdateInterval = 0.3f;
    private float followUpdateTimer = 0f;
    
    private Queue<MovementCommand> commandQueue = new Queue<MovementCommand>();
    private bool isExecutingQueue = false;
    
    private struct MovementCommand
    {
        public Vector3 destination;
        public EntityUnit followTarget;
        public Vector3 followOffset;
        public MovementMode mode;
        
        public MovementCommand(Vector3 dest, EntityUnit target, Vector3 offset, MovementMode m)
        {
            destination = dest;
            followTarget = target;
            followOffset = offset;
            mode = m;
        }
    }
    
    void Start()
    {
        LineRenderer[] existingRenderers = GetComponents<LineRenderer>();
        if (existingRenderers.Length > 1)
        {
            Debug.LogWarning(gameObject.name + " has multiple LineRenderers! Cleaning up...");
            for (int i = 1; i < existingRenderers.Length; i++)
            {
                Destroy(existingRenderers[i]);
            }
        }
        
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        lineRenderer.startWidth = 0.3f;
        lineRenderer.endWidth = 0.3f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.cyan;
        lineRenderer.endColor = Color.cyan;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
        
        unitRenderer = GetComponent<Renderer>();
        if (unitRenderer == null)
        {
            unitRenderer = GetComponentInChildren<Renderer>();
        }
        
        if (unitRenderer != null)
        {
            unitRenderer.material = new Material(unitRenderer.material);
            unitRenderer.material.color = normalColor;
        }
        
        heading = transform.forward;
    }
    
    void Update()
    {
        if (isMoving)
        {
            if (followTarget != null)
            {
                followUpdateTimer += Time.deltaTime;
                if (followUpdateTimer >= followUpdateInterval)
                {
                    Vector3 newDestination = followTarget.transform.position + followOffset;
                    
                    if (currentMovementMode == MovementMode.PotentialFieldsOnly)
                    {
                        currentPath = new Vector3[] { newDestination };
                        currentWaypointIndex = 0;
                    }
                    else
                    {
                        PathRequestManager.RequestPath(transform.position, newDestination, OnPathFound);
                    }
                    followUpdateTimer = 0f;
                }
            }
            
            if (currentMovementMode == MovementMode.PotentialFieldsOnly)
            {
                MoveWithPotentialFieldsOnly();
            }
            else if (currentMovementMode == MovementMode.AStarPF)
            {
                MoveWithPotentialFields();
            }
            else
            {
                MoveWithSimpleFollowing();
            }
        }
        else if (!isExecutingQueue && commandQueue.Count > 0)
        {
            ExecuteNextCommand();
        }
    }
    
    public void MoveTo(Vector3 destination, MovementMode mode)
    {
        MoveTo(destination, mode, false);
    }
    
    public void MoveTo(Vector3 destination, MovementMode mode, bool queueCommand)
    {
        if (queueCommand)
        {
            commandQueue.Enqueue(new MovementCommand(destination, null, Vector3.zero, mode));
        }
        else
        {
            commandQueue.Clear();
            followTarget = null;
            currentMovementMode = mode;
            waypointChain.Clear();
            currentChainIndex = 0;
            
            if (mode == MovementMode.PotentialFieldsOnly)
            {
                currentPath = new Vector3[] { destination };
                currentWaypointIndex = 0;
                isMoving = true;
                velocity = Vector3.zero;
            }
            else
            {
                PathRequestManager.RequestPath(transform.position, destination, OnPathFound);
            }
        }
    }
    
    public void FollowEntity(EntityUnit target, MovementMode mode)
    {
        FollowEntity(target, mode, false, Vector3.zero);
    }
    
    public void FollowEntity(EntityUnit target, MovementMode mode, bool queueCommand)
    {
        FollowEntity(target, mode, queueCommand, Vector3.zero);
    }
    
    public void FollowEntity(EntityUnit target, MovementMode mode, bool queueCommand, Vector3 offset)
    {
        if (queueCommand)
        {
            commandQueue.Enqueue(new MovementCommand(Vector3.zero, target, offset, mode));
        }
        else
        {
            commandQueue.Clear();
            followTarget = target;
            followOffset = offset;
            currentMovementMode = mode;
            waypointChain.Clear();
            currentChainIndex = 0;
            followUpdateTimer = 0f;
            
            Vector3 destination = target.transform.position + offset;
            
            if (mode == MovementMode.PotentialFieldsOnly)
            {
                currentPath = new Vector3[] { destination };
                currentWaypointIndex = 0;
                isMoving = true;
                velocity = Vector3.zero;
            }
            else
            {
                PathRequestManager.RequestPath(transform.position, destination, OnPathFound);
            }
        }
    }
    
    public Vector3 PredictPosition(float timeAhead)
    {
        return transform.position + velocity * timeAhead;
    }
    
    public void SetWaypointChain(List<Vector3> waypoints, MovementMode mode)
    {
        currentMovementMode = mode;
        waypointChain = new List<Vector3>(waypoints);
        currentChainIndex = 0;
        
        if (waypointChain.Count > 0)
        {
            if (mode == MovementMode.PotentialFieldsOnly)
            {
                currentPath = new Vector3[] { waypointChain[0] };
                currentWaypointIndex = 0;
                isMoving = true;
            }
            else
            {
                PathRequestManager.RequestPath(transform.position, waypointChain[0], OnPathFound);
            }
        }
    }
    
    void ExecuteNextCommand()
    {
        if (commandQueue.Count == 0) return;
        
        isExecutingQueue = true;
        MovementCommand cmd = commandQueue.Dequeue();
        
        if (cmd.followTarget != null)
        {
            followTarget = cmd.followTarget;
            followOffset = cmd.followOffset;
            currentMovementMode = cmd.mode;
            followUpdateTimer = 0f;
            
            Vector3 destination = cmd.followTarget.transform.position + cmd.followOffset;
            
            if (cmd.mode == MovementMode.PotentialFieldsOnly)
            {
                currentPath = new Vector3[] { destination };
                currentWaypointIndex = 0;
                isMoving = true;
            }
            else
            {
                PathRequestManager.RequestPath(transform.position, destination, OnPathFound);
            }
        }
        else
        {
            followTarget = null;
            currentMovementMode = cmd.mode;
            
            if (cmd.mode == MovementMode.PotentialFieldsOnly)
            {
                currentPath = new Vector3[] { cmd.destination };
                currentWaypointIndex = 0;
                isMoving = true;
            }
            else
            {
                PathRequestManager.RequestPath(transform.position, cmd.destination, OnPathFound);
            }
        }
    }
    
    void OnPathFound(Vector3[] newPath, bool pathSuccessful)
    {
        if (pathSuccessful && newPath.Length > 0)
        {
            currentPath = newPath;
            currentWaypointIndex = 0;
            isMoving = true;
            velocity = Vector3.zero;
            
            lineRenderer.positionCount = newPath.Length + 1;
            lineRenderer.SetPosition(0, transform.position + Vector3.up * 0.2f);
            for (int i = 0; i < newPath.Length; i++)
            {
                lineRenderer.SetPosition(i + 1, newPath[i] + Vector3.up * 0.2f);
            }
        }
        else
        {
            Debug.LogWarning(gameObject.name + " failed to find path!");
            StopMoving();
        }
    }
    
    void MoveWithSimpleFollowing()
    {
        if (currentPath == null || currentWaypointIndex >= currentPath.Length)
        {
            CheckWaypointChain();
            return;
        }
        
        Vector3 targetWaypoint = currentPath[currentWaypointIndex];
        Vector3 currentPos2D = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 targetPos2D = new Vector3(targetWaypoint.x, 0, targetWaypoint.z);
        Vector3 directionToWaypoint = targetPos2D - currentPos2D;
        float distToWaypoint = directionToWaypoint.magnitude;
        
        bool isLastWaypoint = (currentWaypointIndex >= currentPath.Length - 1);
        
        float checkRadius;
        if (useAdaptiveWaypointRadius && !isLastWaypoint)
        {
            float minRadius = maxSpeed * Time.deltaTime * 3f;
            checkRadius = Mathf.Max(waypointRadius, minRadius);
        }
        else
        {
            checkRadius = isLastWaypoint ? arrivalRadius : waypointRadius;
        }
        
        Debug.DrawLine(transform.position, targetWaypoint, Color.red, Time.deltaTime);
        
        if (directionToWaypoint.magnitude < 0.01f)
        {
            Debug.LogWarning(gameObject.name + " has zero-length direction to waypoint! Skipping.");
            currentWaypointIndex++;
            return;
        }
        
        bool reachedWaypoint = false;
        
        if (distToWaypoint < checkRadius)
        {
            reachedWaypoint = true;
        }
        else if (currentWaypointIndex < currentPath.Length - 1)
        {
            Vector3 nextWaypoint = currentPath[currentWaypointIndex + 1];
            Vector3 nextPos2D = new Vector3(nextWaypoint.x, 0, nextWaypoint.z);
            float distToNext = (nextPos2D - currentPos2D).magnitude;
            
            if (distToNext < distToWaypoint)
            {
                reachedWaypoint = true;
            }
        }
        
        if (reachedWaypoint)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= currentPath.Length)
            {
                CheckWaypointChain();
                return;
            }
            
            targetWaypoint = currentPath[currentWaypointIndex];
            targetPos2D = new Vector3(targetWaypoint.x, 0, targetWaypoint.z);
            directionToWaypoint = targetPos2D - currentPos2D;
            distToWaypoint = directionToWaypoint.magnitude;
        }
        
        Vector3 desiredDirection = directionToWaypoint.normalized;
        
        float currentSpeed = maxSpeed;
        if (isLastWaypoint && distToWaypoint < arrivalRadius * 3f)
        {
            currentSpeed = maxSpeed * (distToWaypoint / (arrivalRadius * 3f));
            currentSpeed = Mathf.Max(currentSpeed, maxSpeed * 0.2f);
        }
        
        if (desiredDirection.magnitude > 0.01f)
        {
            heading = Vector3.Lerp(heading, desiredDirection, Time.deltaTime * rotationSpeed * 2f);
            heading.y = 0;
            
            if (heading.magnitude > 0.01f)
            {
                heading.Normalize();
            }
            else
            {
                heading = desiredDirection;
            }
        }
        
        Vector3 movement = desiredDirection * currentSpeed * Time.deltaTime;
        movement.y = 0;
        transform.position += movement;
        
        if (desiredDirection.magnitude > 0.01f)
        {
            transform.rotation = Quaternion.Lerp(
                transform.rotation, 
                Quaternion.LookRotation(desiredDirection), 
                Time.deltaTime * rotationSpeed
            );
        }
    }
    
    void MoveWithPotentialFieldsOnly()
    {
        if (currentPath == null || currentPath.Length == 0)
        {
            CheckWaypointChain();
            return;
        }
        
        Vector3 targetPos = currentPath[0];
        Vector3 toTarget = targetPos - transform.position;
        float distToTarget = new Vector3(toTarget.x, 0, toTarget.z).magnitude;
        
        if (distToTarget < arrivalRadius)
        {
            CheckWaypointChain();
            return;
        }
        
        Vector3 attractiveForce = CalculateAttractiveForce(targetPos);
        Vector3 obstacleRepulsive = CalculateObstacleRepulsion();
        Vector3 entityRepulsive = CalculateEntityRepulsion();
        
        Vector3 totalForce = attractiveForce + obstacleRepulsive + entityRepulsive;
        totalForce.y = 0;
        totalForce = Vector3.ClampMagnitude(totalForce, maxForce);
        
        velocity += totalForce * Time.deltaTime;
        velocity.y = 0;
        
        float speedLimit = maxSpeed;
        if (distToTarget < arrivalRadius * 3f)
        {
            speedLimit = maxSpeed * (distToTarget / (arrivalRadius * 3f));
            speedLimit = Mathf.Max(speedLimit, maxSpeed * 0.1f);
        }
        
        velocity = Vector3.ClampMagnitude(velocity, speedLimit);
        
        if (velocity.magnitude > 0.1f)
        {
            heading = velocity.normalized;
        }
        
        transform.position += velocity * Time.deltaTime;
        
        if (heading.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(heading);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
    
    void MoveWithPotentialFields()
    {
        if (currentPath == null || currentWaypointIndex >= currentPath.Length)
        {
            CheckWaypointChain();
            return;
        }
        
        Vector3 targetWaypoint = currentPath[currentWaypointIndex];
        Vector3 toWaypoint = targetWaypoint - transform.position;
        float distToWaypoint = new Vector3(toWaypoint.x, 0, toWaypoint.z).magnitude;
        
        bool isLastWaypoint = (currentWaypointIndex >= currentPath.Length - 1);
        float checkRadius = isLastWaypoint ? arrivalRadius : waypointRadius;
        
        if (distToWaypoint < checkRadius)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= currentPath.Length)
            {
                CheckWaypointChain();
                return;
            }
            targetWaypoint = currentPath[currentWaypointIndex];
            velocity *= 0.5f;
        }
        
        Vector3 attractiveForce = CalculateAttractiveForce(targetWaypoint);
        Vector3 obstacleRepulsive = CalculateObstacleRepulsion();
        Vector3 entityRepulsive = CalculateEntityRepulsion();
        
        Vector3 totalForce = attractiveForce + obstacleRepulsive + entityRepulsive;
        totalForce.y = 0;
        totalForce = Vector3.ClampMagnitude(totalForce, maxForce);
        
        velocity += totalForce * Time.deltaTime;
        velocity.y = 0;
        
        float speedLimit = maxSpeed;
        if (isLastWaypoint && distToWaypoint < arrivalRadius * 3f)
        {
            speedLimit = maxSpeed * (distToWaypoint / (arrivalRadius * 3f));
            speedLimit = Mathf.Max(speedLimit, maxSpeed * 0.1f);
        }
        
        velocity = Vector3.ClampMagnitude(velocity, speedLimit);
        
        if (velocity.magnitude > 0.1f)
        {
            heading = velocity.normalized;
        }
        
        transform.position += velocity * Time.deltaTime;
        
        if (heading.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(heading);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
    
    Vector3 CalculateAttractiveForce(Vector3 target)
    {
        Vector3 desired = target - transform.position;
        float distance = desired.magnitude;
        desired.Normalize();
        
        if (distance < arrivalRadius * 3f)
        {
            desired *= maxSpeed * (distance / (arrivalRadius * 3f));
        }
        else
        {
            desired *= maxSpeed;
        }
        
        Vector3 steer = desired - velocity;
        return steer * targetAttractionStrength;
    }
    
    Vector3 CalculateObstacleRepulsion()
    {
        Vector3 repulsion = Vector3.zero;
        
        Collider[] obstacles = Physics.OverlapSphere(transform.position, obstacleRepulsionRange, obstacleLayer);
        foreach (Collider obs in obstacles)
        {
            Vector3 diff = transform.position - obs.ClosestPoint(transform.position);
            float distance = diff.magnitude;
            
            if (distance > 0 && distance < obstacleRepulsionRange)
            {
                diff.Normalize();
                float strength = obstacleRepulsionStrength * (1f - distance / obstacleRepulsionRange);
                repulsion += diff * strength;
            }
        }
        
        return repulsion;
    }
    
    Vector3 CalculateEntityRepulsion()
    {
        Vector3 repulsion = Vector3.zero;
        
        Collider[] entities = Physics.OverlapSphere(transform.position, entityRepulsionRange, entityLayer);
        foreach (Collider entity in entities)
        {
            if (entity.gameObject == gameObject) continue;
            
            Vector3 diff = transform.position - entity.transform.position;
            float distance = diff.magnitude;
            
            if (distance > 0 && distance < entityRepulsionRange)
            {
                diff.Normalize();
                
                float strength;
                if (distance < personalSpaceRadius)
                {
                    strength = entityRepulsionStrength * 3f * (1f - distance / personalSpaceRadius);
                }
                else
                {
                    strength = entityRepulsionStrength * (1f - distance / entityRepulsionRange);
                }
                
                repulsion += diff * strength;
            }
        }
        
        return repulsion;
    }
    
    void CheckWaypointChain()
    {
        if (waypointChain.Count > 0 && currentChainIndex < waypointChain.Count - 1)
        {
            currentChainIndex++;
            
            if (currentMovementMode == MovementMode.PotentialFieldsOnly)
            {
                currentPath = new Vector3[] { waypointChain[currentChainIndex] };
                currentWaypointIndex = 0;
            }
            else
            {
                PathRequestManager.RequestPath(transform.position, waypointChain[currentChainIndex], OnPathFound);
            }
        }
        else
        {
            if (followTarget != null)
            {
                return;
            }
            
            if (commandQueue.Count > 0)
            {
                isExecutingQueue = false;
            }
            else
            {
                StopMoving();
            }
        }
    }
    
    void StopMoving()
    {
        isMoving = false;
        velocity = Vector3.zero;
        lineRenderer.positionCount = 0;
        followTarget = null;
        isExecutingQueue = false;
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (unitRenderer != null)
        {
            unitRenderer.material.color = selected ? selectedColor : normalColor;
        }
    }
    
    void OnDrawGizmos()
    {
        if (currentPath != null && currentPath.Length > 0)
        {
            for (int i = 0; i < currentWaypointIndex && i < currentPath.Length; i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(currentPath[i], 0.1f);
            }
            
            for (int i = currentWaypointIndex; i < currentPath.Length; i++)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(currentPath[i], 0.15f);
                
                if (i > 0)
                {
                    Gizmos.DrawLine(currentPath[i - 1], currentPath[i]);
                }
            }
            
            if (currentWaypointIndex < currentPath.Length)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(currentPath[currentWaypointIndex], 0.2f);
                Gizmos.DrawLine(transform.position, currentPath[currentWaypointIndex]);
            }
            
            if (isMoving && currentWaypointIndex < currentPath.Length)
            {
                bool isLastWaypoint = (currentWaypointIndex >= currentPath.Length - 1);
                float checkRadius = isLastWaypoint ? arrivalRadius : waypointRadius;
                Gizmos.color = new Color(1, 0, 0, 0.3f);
                Gizmos.DrawWireSphere(currentPath[currentWaypointIndex], checkRadius);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, obstacleRepulsionRange);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, entityRepulsionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, personalSpaceRadius);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, heading * 2f);
        
        if (followTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, followTarget.transform.position);
            Gizmos.DrawWireSphere(followTarget.transform.position + followOffset, 0.5f);
        }
        
        if (velocity.magnitude > 0.1f)
        {
            Vector3 predicted = PredictPosition(2f);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(predicted, 0.3f);
            Gizmos.DrawLine(transform.position, predicted);
        }
    }
}