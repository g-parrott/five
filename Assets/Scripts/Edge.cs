using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public class Edge : MonoBehaviour
{
    public GameObject _from;
    public GameObject _to;

    public Material _lineMaterial;

    private LineRenderer lineRenderer;

    private void Start()
    {
    }

    private void Update()
    {
        lineRenderer.SetPosition(0, _from.transform.position);
        lineRenderer.SetPosition(1, _to.transform.position);
    }

    public void Init(GameObject from, GameObject to)
    {
        _from = from;
        _to = to;

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.SetPositions(new Vector3[] { _from.transform.position, _to.transform.position });

        if (_lineMaterial != null)
        {
            lineRenderer.material = _lineMaterial;
        }
    }
}
