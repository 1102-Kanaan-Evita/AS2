using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PathRequestManager : MonoBehaviour
{
    Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    PathRequest currentPathRequest;

    static PathRequestManager instance;
    Pathfinding pathfinding;
    RRTPathfinding rrtPathfinding;
    bool isProcessingPath;

    void Awake()
    {
        instance = this;
        pathfinding = GetComponent<Pathfinding>();
        rrtPathfinding = GetComponent<RRTPathfinding>();
    }

    public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback);
        instance.pathRequestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }
    
    public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback, bool useRRT)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback, useRRT);
        instance.pathRequestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    void TryProcessNext()
    {
        if (!isProcessingPath && pathRequestQueue.Count > 0)
        {
            currentPathRequest = pathRequestQueue.Dequeue();
            isProcessingPath = true;
            
            // Choose pathfinding algorithm
            if (currentPathRequest.useRRT && rrtPathfinding != null)
            {
                rrtPathfinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
            }
            else
            {
                pathfinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
            }
        }
    }
    
    public void FinishedProcessingPath(Vector3[] path, bool success)
    {
        currentPathRequest.callback(path, success);
        isProcessingPath = false;
        TryProcessNext();
    }
    
    struct PathRequest {
        public Vector3 pathStart;
        public Vector3 pathEnd;
        public Action<Vector3[], bool> callback;
        public bool useRRT;

        public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _callback, bool _useRRT = false)
        {
            pathStart = _start;
            pathEnd = _end;
            callback = _callback;
            useRRT = _useRRT;
        }
    }
}