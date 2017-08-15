using System.Collections.Generic;
using UnityEngine;

public class VertexManager : MonoBehaviour
{
    public int _desiredDegree = 4;
    public float[] _desiredAngles = {36f, 72f, 108f};

    // for convenience and consistency, I store a string tag to grab all objects that are tagged with
    // "vertex"
    public const string VertexTag = "vertex";

    // container to represent graph via adjacency lists
    private Dictionary<GameObject, List<GameObject>> _vertexMap
        = new Dictionary<GameObject, List<GameObject>>();

    // Collider Buffer for faster collision checking
    private Collider[] _buffer = new Collider[512];

	// Use this for initialization
	private void Start()
    {
        // find all initial vertices in the scene and add them to the map
        foreach (var gobj in GameObject.FindGameObjectsWithTag(VertexTag))
        {
            _vertexMap.Add(gobj, new List<GameObject>());
        }
	}

	// Update is called once per frame
	private void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            Debug.Log("space pressed");
        }
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
            GameObject toConnect = null;
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

                            // if they can connect, connect them
                            else
                            {
                                Connect(c.gameObject, vertex);
                            }
                        }
                    }
                }
            }
        }

        return nextDirection;
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
        for (int i = 1; i < buffer.Count; i += 1)
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
                    var angle = AngleRelative(vertex.transform.position + v, _vertexMap[vertex][i], _vertexMap[vertex][k]);
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

        return nextDirection;
    }

    private Vector3 Constrain(GameObject vertex)
    {
        Vector3 nextDirection = Vector3.zero;

        return nextDirection;
    }
}
