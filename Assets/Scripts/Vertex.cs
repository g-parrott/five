using UnityEngine;

public class Vertex : MonoBehaviour
{
    public float _sightRadius = 10;
    public float _connectionRadius = 2;
    public float _connectionStrength = 1;
    public float _repulsionRadius = 1;
    public float _repulsionStrength = 1;
    public VertexState _currentState;

    private void Start()
    {
    }

    private void Update()
    {
    }

    public void Init(float sightRadius, float _connectionRadius, float connectionStrength, float repulsionStrength)
    {
    }
}
