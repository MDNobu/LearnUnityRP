using System;
using UnityEngine;

// 
[ExecuteAlways]
public class QxShadowCameraDebug : MonoBehaviour
{
    private QxCSM _csm;

    public int a;
    // public int 

    private void Update()
    {
        Camera mainCam = Camera.main;
        
        // 获得光源信息
        Light light = RenderSettings.sun;
        Vector3 lightDir = light.transform.rotation * Vector3.forward;
        
        // 更新shadowmap
        if (null == _csm)
        {
            _csm = new QxCSM();
        }
        
        _csm.Update(mainCam, lightDir);
        _csm.DebugDraw();
    }
}