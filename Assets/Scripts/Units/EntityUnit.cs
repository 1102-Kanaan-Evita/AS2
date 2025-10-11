using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EntityUnit : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 5f;
    public float maxForce = 10f;
    public float arrivalRadius = 0.5f;
    public float rotationSpeed = 5f;
    
    [Header("Potential Fields")]
    public float obstacleRepulsionRange = 3f;
    public float obstacleRepulsionStrength = 5f;
    public float entityRepulsionRange = 4f;
    public float entityRepulsionStrength = 6f;
    public float targetAttractionStrength = 1f;
    public float personalSpaceRadius = 1f; // Minimum distance to maintain from other entities
    
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
    
    // Pathfinding
    private Vector3[] currentPath;
    private int currentWaypointIndex = 0;
    private bool isMoving = false;
    private bool usePotentialFields = false;
    private List<Vector3> waypointChain = new List<Vector3>();
    private int currentChainIndex = 0;
    
    // Following system
    private EntityUnit followTarget = null;
    private Vector3 followOffset = Vector3.zero;
    private float followUpdateInterval = 0.3f;
    private float followUpdateTimer = 0f;
    
    // Command queue
    private Queue<MovementCommand> commandQueue = new Queue<MovementCommand>();
    private bool isExecutingQueue = false;
    
    // Movement command structure
    private struct MovementCommand
    {
        public Vector3 destination;
        public EntityUnit followTarget;
        public Vector3 followOffset;
        public bool usePF;
        
        public MovementCommand(Vector3 dest, EntityUnit target, Vector3 offset, bool pf)
        {
            destination = dest;
            followTarget = target;
            followOffset = offset;
            usePF = pf;
        }
    }
    
    void Start()
    {
        // Setup line renderer for path visualization
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
        
        // Get renderer for selection color (check children too)
        unitRenderer = GetComponent<Renderer>();
        if (unitRenderer == null)
        {
            unitRenderer = GetComponentInChildren<Renderer>();
        }
        
        // Set initial color if renderer exists
        if (unitRenderer != null)
        {
            // Create a new material instance to avoid changing shared material
            unitRenderer.material = new Material(unitRenderer.material);
            unitRenderer.material.color = normalColor;
        }
        
        // Initialize heading to current forward
        heading = transform.forward;
    }
    
    void Update()
    {
        if (isMoving)
        {
            // Update follow target position periodically
            if (followTarget != null)
            {
                followUpdateTimer += Time.deltaTime;
                if (followUpdateTimer >= followUpdateInterval)
                {
                    Vector3 newDestination = followTarget.transform.position + followOffset;
                    PathRequestManager.RequestPath(transform.position, newDestination, OnPathFound);
                    followUpdateTimer = 0f;
                }
            }
            
            if (usePotentialFields)
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
            // Execute next queued command
            ExecuteNextCommand();
        }
    }
    
    public void MoveTo(Vector3 destination, bool usePF)
    {
        MoveTo(destination, usePF, false);
    }
    
    public void MoveTo(Vector3 destination, bool usePF, bool queueCommand)
    {
        if (queueCommand)
        {
            commandQueue.Enqueue(new MovementCommand(destination, null, Vector3.zero, usePF));
        }
        else
        {
            // Clear queue and execute immediately
            commandQueue.Clear();
            followTarget = null;
            usePotentialFields = usePF;
            waypointChain.Clear();
            currentChainIndex = 0;
            PathRequestManager.RequestPath(transform.position, destination, OnPathFound);
        }
    }
    
    public void FollowEntity(EntityUnit target, bool usePF)
    {
        FollowEntity(target, usePF, false, Vector3.zero);
    }
    
    public void FollowEntity(EntityUnit target, bool usePF, bool queueCommand)
    {
        FollowEntity(target, usePF, queueCommand, Vector3.zero);
    }
    
    public void FollowEntity(EntityUnit target, bool usePF, bool queueCommand, Vector3 offset)
    {
        if (queueCommand)
        {
            commandQueue.Enqueue(new MovementCommand(Vector3.zero, target, offset, usePF));
        }
        else
        {
            // Clear queue and execute immediately
            commandQueue.Clear();
            followTarget = target;
            followOffset = offset;
            usePotentialFields = usePF;
            waypointChain.Clear();
            currentChainIndex = 0;
            followUpdateTimer = 0f;
            
            Vector3 destination = target.transform.position + offset;
            PathRequestManager.RequestPath(transform.position, destination, OnPathFound);
        }
    }
    
    public Vector3 PredictPosition(float timeAhead)
    {
        // Predict where this entity will be in 'timeAhead' seconds
        return transform.position + velocity * timeAhead;
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
            usePotentialFields = cmd.usePF;
            followUpdateTimer = 0f;
            
            Vector3 destination = cmd.followTarget.transform.position + cmd.followOffset;
            PathRequestManager.RequestPath(transform.position, destination, OnPathFound);
        }
        else
        {
            followTarget = null;
            usePotentialFields = cmd.usePF;
            PathRequestManager.RequestPath(transform.position, cmd.destination, OnPathFound);
        }
    }
    
    public void SetWaypointChain(List<Vector3> waypoints, bool usePF)
    {
        usePotentialFields = usePF;
        waypointChain = new List<Vector3>(waypoints);
        currentChainIndex = 0;
        
        if (waypointChain.Count > 0)
        {
            PathRequestManager.RequestPath(transform.position, waypointChain[0], OnPathFound);
        }
    }
    
    void OnPathFound(Vector3[] newPath, bool pathSuccessful)
    {
        if (pathSuccessful && newPath.Length > 0)
        {
            currentPath = newPath;
            currentWaypointIndex = 0;
            isMoving = true;
            
            // Visualize path
            lineRenderer.positionCount = newPath.Length + 1;
            lineRenderer.SetPosition(0, transform.position + Vector3.up * 0.2f);
            for (int i = 0; i < newPath.Length; i++)
            {
                lineRenderer.SetPosition(i + 1, newPath[i] + Vector3.up * 0.2f);
            }
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
        Vector3 direction = (targetWaypoint - transform.position).normalized;
        
        // Update heading smoothly
        heading = Vector3.Lerp(heading, direction, Time.deltaTime * rotationSpeed);
        
        // Move in heading direction
        transform.position += heading * maxSpeed * Time.deltaTime;
        
        // Rotate to face heading
        if (heading != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(heading);
        }
        
        // Check if reached waypoint
        float distToWaypoint = Vector3.Distance(transform.position, targetWaypoint);
        if (distToWaypoint < arrivalRadius)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= currentPath.Length)
            {
                CheckWaypointChain();
            }
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
        
        // Calculate forces
        Vector3 attractiveForce = CalculateAttractiveForce(targetWaypoint);
        Vector3 obstacleRepulsive = CalculateObstacleRepulsion();
        Vector3 entityRepulsive = CalculateEntityRepulsion();
        
        // Combine forces
        Vector3 totalForce = attractiveForce + obstacleRepulsive + entityRepulsive;
        totalForce = Vector3.ClampMagnitude(totalForce, maxForce);
        
        // Update velocity
        velocity += totalForce * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
        
        // Update heading from velocity
        if (velocity.magnitude > 0.1f)
        {
            heading = velocity.normalized;
        }
        
        // Move
        transform.position += velocity * Time.deltaTime;
        
        // Rotate to face heading
        if (heading != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(heading);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
        
        // Check if reached waypoint
        float distToWaypoint = Vector3.Distance(transform.position, targetWaypoint);
        if (distToWaypoint < arrivalRadius)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= currentPath.Length)
            {
                CheckWaypointChain();
            }
        }
    }
    
    Vector3 CalculateAttractiveForce(Vector3 target)
    {
        Vector3 desired = target - transform.position;
        float distance = desired.magnitude;
        desired.Normalize();
        
        // Arrival behavior - slow down as we approach
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
                
                // Stronger repulsion when very close (within personal space)
                float strength;
                if (distance < personalSpaceRadius)
                {
                    // Very strong repulsion if within personal space
                    strength = entityRepulsionStrength * 3f * (1f - distance / personalSpaceRadius);
                }
                else
                {
                    // Normal repulsion
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
            PathRequestManager.RequestPath(transform.position, waypointChain[currentChainIndex], OnPathFound);
        }
        else
        {
            // Check if we're following and need to continue
            if (followTarget != null)
            {
                // Keep following - path will update in Update()
                return;
            }
            
            // Check if there are queued commands
            if (commandQueue.Count > 0)
            {
                isExecutingQueue = false; // Allow next command to execute
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
        else
        {
            Debug.LogWarning("EntityUnit: No renderer found for selection color on " + gameObject.name);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw detection ranges
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, obstacleRepulsionRange);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, entityRepulsionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, personalSpaceRadius);
        
        // Draw heading
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, heading * 2f);
        
        // Draw follow target connection
        if (followTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, followTarget.transform.position);
            Gizmos.DrawWireSphere(followTarget.transform.position + followOffset, 0.5f);
        }
        
        // Draw predicted position
        if (velocity.magnitude > 0.1f)
        {
            Vector3 predicted = PredictPosition(2f);
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(predicted, 0.3f);
            Gizmos.DrawLine(transform.position, predicted);
        }
    }
}