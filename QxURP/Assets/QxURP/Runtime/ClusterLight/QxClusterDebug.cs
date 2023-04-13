using System;
using UnityEngine;

[ExecuteAlways]
public class QxClusterDebug : MonoBehaviour
{
    private QxClusterLight _clusterLightLight;

    public bool generateClusterLight = false;

    private void Update()
    {
        if (_clusterLightLight == null || generateClusterLight)
        {
            _clusterLightLight = new QxClusterLight();
        }
        
        // 更新光源
        Light[] lights = FindObjectsOfType<Light>();
        _clusterLightLight.UpdateLightBuffer(lights);
        
        // 划分cluster
        Camera camera = Camera.main;
        _clusterLightLight.GenerateCluster(camera);
        
        // 分配光源
        // _clusterLightLight.LightAssign();
        
        // debug
        _clusterLightLight.DebugCluster();
        
    }
}