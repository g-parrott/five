using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

public class CameraTraverser : MonoBehaviour
{
    public VertexManager _vertexManager;

    private GameObject _target;

    private GameObject _next;

    private int _nextIndex = -1;

    private void Start()
    {
        _target = _vertexManager.GetRandomVertex();

        //if (_vertexManager.GetConnected(_target).Count > 0)
        //{
        //    _next = _vertexManager.GetConnected(_target)[_nextIndex];
        //    Camera.main.transform.LookAt(_next.transform);
        //}
    }

    private void Update()
    {
        if (_target == null)
        {
            _target = _vertexManager.GetRandomVertex();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_nextIndex != -1 && _vertexManager.GetConnected(_target).Count > 0)
            {
                MoveToNext();
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            CycleNextBackward();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            CycleNextForward();
        }
    }

    private void UpdateNext()
    {
        _next = _vertexManager.GetConnected(_target)[_nextIndex];
        Camera.main.transform.LookAt(_next.transform);
    }

    private void CycleNextForward()
    {
        _nextIndex = (_nextIndex + 1 == _vertexManager.GetConnected(_target).Count) ? 0 : _nextIndex + 1;
        UpdateNext(); 
    }

    private void CycleNextBackward()
    {
        _nextIndex = (_nextIndex - 1 < 0) ? _vertexManager.GetConnected(_target).Count - 1 : _nextIndex - 1;
        UpdateNext();
    }

    private void MoveToNext()
    {
        Camera.main.transform.position = _next.transform.position;
        _target = _next;
        _nextIndex = -1;
        CycleNextForward();
    }
}