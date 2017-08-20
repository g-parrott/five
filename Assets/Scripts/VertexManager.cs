using System.Collections.Generic;
using UnityEngine;

public class VertexManager : MonoBehaviour
{
    public int _desiredDegree = 4;
    public float[] _desiredAngles = {36f, 72f, 108f};

    public float _searchSpeed = 1f;
    public float _optimizeSpeed = 1f;

    public GameObject _edge;

    // for convenience and consistency, I store a string tag to grab all objects that are tagged with
    // "vertex"
    public const string VertexTag = "Vertex";

    // container to represent graph via adjacency lists
    private Dictionary<GameObject, List<GameObject>> _vertexMap
        = new Dictionary<GameObject, List<GameObject>>();

    // queue of vertices that will be connected after computation
    private Queue<KeyValuePair<GameObject, GameObject>> _connectQueue =
        new Queue<KeyValuePair<GameObject, GameObject>>();

    private Dictionary<GameObject, Vector3> _influenceMap = new Dictionary<GameObject, Vector3>();

    // Collider Buffer for faster collision checking
    private Collider[] _buffer = new Collider[512];

	// Use this for initialization
	private void Start()
    {
	}

    public void DeferredStart()
    {
        foreach (var gobj in GameObject.FindGameObjectsWithTag(VertexTag))
        {
            _vertexMap.Add(gobj, new List<GameObject>());
        }
    }

	// Update is called once per frame
	private void Update()
    {
	}

    public void DeferredUpdate()
    {
        // compute directions
        foreach (var v in _vertexMap.Keys)
        {
            Vector3 nextDirection = Vector3.zero;
            switch (v.GetComponent<Vertex>()._currentState)
            {
                case VertexState.Search:
                    nextDirection += _searchSpeed * Search(v);
                    break;
                case VertexState.Optimize:
                    nextDirection = _optimizeSpeed * Optimize(v);
                    break;
                case VertexState.Constrain:
                    break;
            }

            _influenceMap[v] = nextDirection;
        }

        // connect any gameobjects close enough to connect (in the _connectQueue)
        while (_connectQueue.Count != 0)
        {
            var newEdge = _connectQueue.Dequeue();
            Connect(newEdge.Key, newEdge.Value);
        }

        // translate the gameobjects
        foreach (var v in _vertexMap)
        {
            Vector3 translation = _influenceMap[v.Key];

            foreach (var e in v.Value)
            {
                switch (e.GetComponent<Vertex>()._currentState)
                {
                    case VertexState.Search:
                        translation += _searchSpeed * _influenceMap[e];
                        break;
                    case VertexState.Optimize:
                        translation += _optimizeSpeed * _influenceMap[e];
                        break;
                    case VertexState.Constrain:
                        break;
                }
                translation += _influenceMap[e];
            }

            translation += Repulse(v.Key);

            v.Key.transform.Translate(translation * Time.deltaTime);
        }
    }

    ///////// Functions related to modifying the graph

    public void Add(GameObject vertex)
    {
        _vertexMap.Add(vertex, new List<GameObject>());
        _influenceMap.Add(vertex, Vector3.zero);
    }

    public void Remove(GameObject vertex)
    {
        _vertexMap.Remove(vertex);
        foreach (var v in _vertexMap)
        {
            if (v.Value.Contains(vertex))
            {
                v.Value.Remove(vertex);
            }
        }
        Destroy(vertex);
    }

    // FUnctions related to accessing Vertex objects 

    public List<GameObject> GetConnected(GameObject vertex)
    {
        return _vertexMap[vertex];
    }

    public GameObject GetRandomVertex()
    {
        foreach (var v in _vertexMap.Keys)
        {
            return v;
        }

        return null;
    }

    ///////// Functions related to connecting vertices

    // connect two vertices
    private void Connect(GameObject v1, GameObject v2)
    {
        // can only connect vertices that are in a search state
        if (Degree(v1) < _desiredDegree && Degree(v2) < _desiredDegree)
        {
            _vertexMap[v1].Add(v2);
            _vertexMap[v2].Add(v1);

            if (Degree(v1) == _desiredDegree)
            {
                ChangeState(v1, VertexState.Optimize);
            }

            if (Degree(v2) == _desiredDegree)
            {
                ChangeState(v2, VertexState.Optimize);
            }

            // make a visible edge
            Instantiate<GameObject>(_edge, Vector3.zero, Quaternion.identity).GetComponent<Edge>().Init(v1, v2);
        }
    }

    // check if two vertices are connected
    private bool Connected(GameObject v1, GameObject v2)
    {
        return _vertexMap[v1].Contains(v2) && _vertexMap[v2].Contains(v1);
    }

    ///////// Functions related to graph properties of vertices

    // get the degree of this vertex
    private int Degree(GameObject vertex)
    {
        return _vertexMap[vertex].Count;
    }

    //////// Functions related to geometric properties of vertices

    // get the angle in degrees between two vertices relative to the x-axis
    private float AngleX(GameObject v1, GameObject v2)
    {
        return Vector3.Angle((v2.transform.position - v1.transform.position).normalized, Vector3.right);
    }

    // get the angle in degrees between two vertices relative to the y-axis
    private float AngleY(GameObject v1, GameObject v2)
    {
        return Vector3.Angle((v2.transform.position - v1.transform.position).normalized, Vector3.up);
    }

    // get the angle in degrees between two vertices relative to the z-axis
    private float AngleZ(GameObject v1, GameObject v2)
    {
        return Vector3.Angle((v2.transform.position - v1.transform.position).normalized, Vector3.forward);
    }

    // return the angle between the line from refVertex to v1 and the line from refVertex to v2
    private float AngleRelative(GameObject refVertex, GameObject v1, GameObject v2)
    {
        var v1Dir = (v1.transform.position - refVertex.transform.position).normalized;
        var v2Dir = (v2.transform.position - refVertex.transform.position).normalized;
        return Vector3.Angle(v1Dir, v2Dir);
    }

    private float AngleRelative(Vector3 refVertex, Vector3 v1, Vector3 v2)
    {
        return Vector3.Angle((v1 - refVertex).normalized, (v2 - refVertex).normalized);
    }

    //////// Functions related to manipulating the properties of vertices as behavioral objects

    private void ChangeState(GameObject vertex, VertexState newState)
    {
        vertex.GetComponent<Vertex>()._currentState = newState;
    }

    //////// Behavioral functions

    private Vector3 Search(GameObject vertex)
    {
        Vector3 nextDirection = Vector3.zero;

        // get Vertex component
        var vc = vertex.GetComponent<Vertex>();

        // sanity check to make sure we don't call this function when it isn't relative to the vertex
        if (vc._currentState != VertexState.Search)
        {
            Debug.Log("VertexManager Warning: Calling Search on Vertex that is not in Search state");
        }

        // look for nearby vertices
        int collisions =
            Physics.OverlapSphereNonAlloc(vertex.transform.position, vc._sightRadius, _buffer);

        // if there are any, check if at least one of them is searching as well
        if (collisions > 0)
        {
            for (int i = 0; i < collisions; i += 1)
            {
                // if we achieve the desired number of connections, this vertex is done searching
                // and the rest of this function is unnecessary
                if (vertex.GetComponent<Vertex>()._currentState != VertexState.Search)
                {
                    return nextDirection;
                }

                var c = _buffer[i];
                if (c.tag == VertexTag)
                {
                    if (c.GetComponent<Vertex>()._currentState == VertexState.Search)
                    {
                        var distance = Vector3.Distance(vertex.transform.position, c.transform.position);

                        // if the vertices can connect, and they aren't already connected, connect them
                        if (!Connected(c.gameObject, vertex))
                        {
                            // if the vertices cannot connect, this vertex will try to move towards the other
                            if (distance > vc._connectionRadius)
                            {
                                nextDirection += (c.transform.position - vertex.transform.position).normalized;
                            }

                            // if they can connect, add them to the connection queue
                            else
                            {
                                _connectQueue.Enqueue(new KeyValuePair<GameObject, GameObject>(c.gameObject, vertex));
                            }
                        }
                    }
                }
            }
        }

        return nextDirection.normalized;
    }

    private Vector3 Optimize(GameObject vertex)
    {
        Vector3 nextDirection = Vector3.zero;
        var possibleDirections = new List<Vector3>();
        float minAngle = Mathf.Infinity;

        foreach (var c in _vertexMap[vertex])
        {
            possibleDirections.Add((c.transform.position - vertex.transform.position).normalized);
        }

        // make a buffer so we can modify the list while "accessing" it
        var buffer = new List<Vector3>();
        foreach (var v in possibleDirections)
        {
            buffer.Add(v);
        }

        // add the vectors in-between
        for (int i = 0; i < buffer.Count - 1; i += 1)
        {
            possibleDirections.Add(((buffer[i] + buffer[i + 1]) * (1/2)).normalized);
        }

        // find the direction which optimizes the angle
        for (int i = 0; i < _vertexMap[vertex].Count; i += 1)
        {
            for (int k = 0; k < _vertexMap[vertex].Count; k += 1)
            {
                foreach (var v in possibleDirections)
                {
                    var angle = AngleRelative(vertex.transform.position + v,
                                              _vertexMap[vertex][i].transform.position,
                                              _vertexMap[vertex][k].transform.position);

                    float diff = Mathf.Infinity;
                    foreach (var a in _desiredAngles)
                    {
                        if (angle - a < diff)
                        {
                            diff = angle - a;
                        }
                    }
                    if (diff < minAngle)
                    {
                        nextDirection = v;
                        minAngle = diff;
                    }
                }
            }
        }

        return (nextDirection + new Vector3(0, 0, Random.value)).normalized;
    }

    private Vector3 Constrain(GameObject vertex)
    {
        Vector3 nextDirection = Vector3.zero;

        return nextDirection;
    }

    private Vector3 Repulse(GameObject vertex)
    {
        Vector3 result = Vector3.zero;
        int collisions = Physics.OverlapSphereNonAlloc(vertex.transform.position, vertex.GetComponent<Vertex>()._repulsionRadius, _buffer);
        if (collisions > 0)
        {
            for (int i = 0; i < collisions; i += 1)
            {
                var v = _buffer[i].GetComponent<Transform>();
                if (v.tag == VertexTag)
                {
                    result += (vertex.transform.position - v.position);
                }
            }
        }
        return result.normalized * vertex.GetComponent<Vertex>()._repulsionStrength;
    }
}
