using System;
using UnityEngine;

[ExecuteAlways]
public class QxInstanceDebug : MonoBehaviour
{
    public QxInstanceData _instanceData;
    public ComputeShader _cs;
    public Camera _camera;

    public bool useCulling = false;

    private void Update()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_cs == null)
        {
            _cs = Resources.Load<ComputeShader>("Shaders/QxInstanceCulling");
        }

        if (useCulling)
        {
            QxInstanceDrawer.Draw(_instanceData, _camera, _cs);
        }
        else
        {
            QxInstanceDrawer.Draw(_instanceData);
        }
    }
}