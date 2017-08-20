using System.Collections.Generic;
using UnityEngine;

public class Initializer : MonoBehaviour
{
    public enum Configuration
    {
        Grid,
        Circle,
        Box,
        Sphere
    }

    public GameObject _vertexPrefab;

    public VertexManager _vertexManager;

    public Configuration _config;

    private void Start()
    {
        if (_vertexManager == null)
        {
            Debug.Log("Initializer Warning: No VertexManager has been assigned to this component. This script will break");
        }

        //MakeParticleGrid(32, 32, 5);
        //
        MakeRandomMess(50, 10000);

       //_vertexManager.DeferredStart();
    }

    private void Update()
    {
        _vertexManager.DeferredUpdate();
    }

    private void MakeRandomMess(float radius, int count)
    {
        for (int i = 0; i < count; i += 1)
        {
            var position = Random.insideUnitSphere * radius;
            var o = Instantiate<GameObject>(_vertexPrefab, position, Quaternion.identity);
            _vertexManager.Add(o);
        }
    }

    private void MakeParticleGrid(int width, int height, float spacing)
    {
        Vector3 position = Vector3.zero;
        for (int i = 0; i < width; i += 1)
        {
            for (int k = 0; k < height; k += 1)
            {
                position = new Vector3(i * spacing, k * spacing, 0);
                var o = Instantiate<GameObject>(_vertexPrefab, position, Quaternion.identity);
                _vertexManager.Add(o);
            }
        }
    }
}
