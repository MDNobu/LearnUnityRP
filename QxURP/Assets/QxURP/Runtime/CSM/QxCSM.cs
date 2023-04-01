
using UnityEngine;

public class QxCSM
{
    // csm 按view space分割的比例
    public float[] splits = {0.07f, 0.13f, 0.25f, 0.55f};
    // 4个光源视锥的宽度,
    public float[] orthoWidths = new float[4];
    
    // 主相机视锥的8个顶点
    private Vector3[] _nearCorners = new Vector3[4];
    private Vector3[] _farCorners = new Vector3[4];
    
    // 主相机划分为4个视锥体
    private Vector3[] f0_near = new Vector3[4];
    private Vector3[] f0_far = new Vector3[4];
    private Vector3[] f1_near = new Vector3[4];
    private Vector3[] f1_far = new Vector3[4];
    private Vector3[] f2_near = new Vector3[4];
    private Vector3[] f2_far = new Vector3[4];
    private Vector3[] f3_near = new Vector3[4];
    private Vector3[] f3_far = new Vector3[4];

    // 分割好的视锥的包围盒, 8个顶点组成
    private Vector3[] box0;
    private Vector3[] box1;
    private Vector3[] box2;
    private Vector3[] box3;

    struct MainCameraSettings
    {
        public Vector3 position;
        public Quaternion rotation;
        public float nearClipPlane;
        public float farClipPlane;
        public float aspect;
    }

    private MainCameraSettings mainCamSettings;
    
    // 
    Vector3 matTransform(Matrix4x4 m, Vector3 v, float w)
    {
        Vector4 v4 = new Vector4(v.x, v.y, v.z, w);
        v4 = m * v4;
        return new Vector3(v4.x, v4.y, v4.z);
    }
    
    // 计算光源方向包围盒的世界坐标
    // 现在这里的实现是 fit to scene的
    Vector3[] LightSpaceAABB(Vector3[] nearCorners, Vector3[] farCorners, Vector3 lightDir)
    {
        // lightDir = Vector3.forward;
        // Matrix4x4 worldToShadowSpace =  Matrix4x4.LookAt(lightDir, Vector3.zero,  Vector3.up);
        // Matrix4x4 shadowSpaceToWorld = worldToShadowSpace.inverse;
        Debug.DrawLine(Vector3.zero, lightDir * 100, Color.red);
        Matrix4x4 shadowSpaceToWorld =  Matrix4x4.LookAt( Vector3.zero,lightDir,  Vector3.up);
        Matrix4x4 worldToShadowSpace = shadowSpaceToWorld.inverse;
        
        // 视锥顶点转到shadow space
        for (int i = 0; i < 4; i++)
        {
            nearCorners[i] = matTransform(worldToShadowSpace, nearCorners[i], 1.0f);
            farCorners[i] = matTransform(worldToShadowSpace, farCorners[i], 1.0f);
        }
        
        // 计算AABB包围盒
        float[] xs = new float[8];
        float[] ys = new float[8];
        float[] zs = new float[8];
        for (int i = 0; i < 4; i++)
        {
            xs[i] = nearCorners[i].x;
            xs[i + 4] = farCorners[i].x;
            ys[i] = nearCorners[i].y;
            ys[i + 4] = farCorners[i].y;
            zs[i] = nearCorners[i].z;
            zs[i + 4] = farCorners[i].z;
        }

        float xmin = Mathf.Min(xs);
        float xmax = Mathf.Max(xs);
        float ymin = Mathf.Min(ys);
        float ymax = Mathf.Max(ys);
        float zmin = Mathf.Min(zs);
        float zmax = Mathf.Max(zs);

        Vector3[] points =
        {
            new Vector3(xmin, ymin, zmin), new Vector3(xmin, ymin, zmax), new Vector3(xmin, ymax, zmin), new Vector3(xmin, ymax, zmax),
            new Vector3(xmax, ymin, zmin), new Vector3(xmax, ymin, zmax), new Vector3(xmax, ymax, zmin), new Vector3(xmax, ymax, zmax)
        };

        for (int i = 0; i < 8; i++)
        {
            points[i] = matTransform(shadowSpaceToWorld, points[i], 1.0f);
        }


        // 视锥顶点还原到world space
        for (int i = 0; i < 4; i++)
        {
            nearCorners[i] = matTransform(shadowSpaceToWorld, nearCorners[i], 1.0f);
            farCorners[i] = matTransform(shadowSpaceToWorld, farCorners[i], 1.0f);
        }
        return points;
    }

    // 用主相机和光源方向更新csm划分
    public void Update(Camera mainCam, Vector3 lightDir)
    {
        // 获取主相机视锥
        mainCam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), mainCam.farClipPlane,
            Camera.MonoOrStereoscopicEye.Mono, _farCorners);
        mainCam.CalculateFrustumCorners(new Rect(0, 0, 1, 1), mainCam.nearClipPlane,
            Camera.MonoOrStereoscopicEye.Mono, _nearCorners
            );

        // 视锥顶点转世界坐标系
        for (int i = 0; i < 4; i++)
        {
            _nearCorners[i] = mainCam.transform.TransformVector(_nearCorners[i]) + mainCam.transform.position;
            _farCorners[i] = mainCam.transform.TransformVector(_farCorners[i]) + mainCam.transform.position;
        }
        
        // 按照比例划分视锥体
        for (int i = 0; i < 4; i++)
        {
            Vector3 dir = _farCorners[i] - _nearCorners[i];

            f0_near[i] = _nearCorners[i];
            f0_far[i] = f0_near[i] + dir * splits[0];

            f1_near[i] = f0_far[i];
            f1_far[i] = f1_near[i] + dir * splits[1];

            f2_near[i] = f1_far[i];
            f2_far[i] = f2_near[i] + dir * splits[2];

            f3_near[i] = f2_far[i];
            f3_far[i] = f3_near[i] + dir * splits[3];
        }
        
        // 计算包围盒
        box0 = LightSpaceAABB(f0_near, f0_far, lightDir);
        box1 = LightSpaceAABB(f1_near, f1_far, lightDir);
        box2 = LightSpaceAABB(f2_near, f2_far, lightDir);
        box3 = LightSpaceAABB(f3_near, f3_far, lightDir);

        orthoWidths[0] = Vector3.Magnitude(f0_far[2] - f0_near[0]);
        orthoWidths[1] = Vector3.Magnitude(f1_far[2] - f1_near[0]);
        orthoWidths[2] = Vector3.Magnitude(f2_far[2] - f2_near[0]);
        orthoWidths[3] = Vector3.Magnitude(f3_far[2] - f3_near[0]);
    }


    public void DrawFrustum(Vector3[] nearCorners, Vector3[] farCorners, Color color)
    {
        for (int i = 0; i < 4; i++)
        {
            Debug.DrawLine(nearCorners[i], farCorners[i], color);
        }
        
        Debug.DrawLine(farCorners[0], farCorners[1], color);
        Debug.DrawLine(farCorners[0], farCorners[3], color);
        Debug.DrawLine(farCorners[2], farCorners[1], color);
        Debug.DrawLine(farCorners[2], farCorners[3], color);
        Debug.DrawLine(nearCorners[0], nearCorners[1], color);
        Debug.DrawLine(nearCorners[0], nearCorners[3], color);
        Debug.DrawLine(nearCorners[2], nearCorners[1], color);
        Debug.DrawLine(nearCorners[2], nearCorners[3], color);
    }

    public void CacheCameraSettings(ref Camera camera)
    {
        mainCamSettings.position = camera.transform.position;
        mainCamSettings.rotation = camera.transform.rotation;
        mainCamSettings.nearClipPlane = camera.nearClipPlane;
        mainCamSettings.farClipPlane = camera.farClipPlane;
        mainCamSettings.aspect = camera.aspect;
        camera.orthographic = true;
    }

    
    // 画光源方向的AABB
    public void DrawAABB(Vector3[] points, Color color)
    {
        // 画线
        Debug.DrawLine(points[0], points[1], color);
        Debug.DrawLine(points[0], points[2], color);
        Debug.DrawLine(points[0], points[4], color);
        
        Debug.DrawLine(points[6], points[2], color);
        Debug.DrawLine(points[6], points[7], color);
        Debug.DrawLine(points[6], points[4], color);

        Debug.DrawLine(points[5], points[1], color);
        Debug.DrawLine(points[5], points[7], color);
        Debug.DrawLine(points[5], points[4], color);

        Debug.DrawLine(points[3], points[1], color);
        Debug.DrawLine(points[3], points[2], color);
        Debug.DrawLine(points[3], points[7], color);    
    }

    
    public void DebugDraw()
    {
        DrawFrustum(_nearCorners, _farCorners, Color.white);
        DrawAABB(box0, Color.yellow);
        DrawAABB(box1, Color.magenta);
        DrawAABB(box2, Color.green);
        DrawAABB(box3, Color.cyan);
    }

    // 还原到原来的主相机参数
    public void RevertMainCameraSettings(ref Camera camera)
    {
        camera.transform.position = mainCamSettings.position;
        camera.transform.rotation = mainCamSettings.rotation;
        camera.nearClipPlane = mainCamSettings.nearClipPlane;
        camera.farClipPlane = mainCamSettings.farClipPlane;
        camera.aspect = mainCamSettings.aspect;
        camera.orthographic = false;
    }

    public void ConfigCameraToShadowSpace(ref Camera camera, Vector3 lightDir, int level, float distance, int shadowMapResolution)
    {
        Vector3[] box = new Vector3[8];
        var f_near = new Vector3[4];
        var f_far = new Vector3[4];
        #region Select box by level
        if (0 == level)
        {
            box = box0;
            f_near = f0_near;
            f_far = f0_far;
        }
        else if (1 == level)
        {
            box = box1;
            f_near = f1_near;
            f_far = f1_far;
        }
        else if (2 == level)
        {
            box = box2;
            f_near = f2_near;
            f_far = f2_far;
        }
        else if (3 == level)
        {
            box = box3;
            f_near = f3_near;
            f_far = f3_far;
        }
        else
        {
            Debug.LogError("level not legal");
            return;
        }
        #endregion


        Vector3 center = (box[3] + box[4]) / 2;
        float width = Vector3.Magnitude(box[0] - box[4]);
        float height = Vector3.Magnitude(box[0] - box[2]);
        //选择这里作为len的原因是为了防止相机旋转时，shadow volume split快速的变化
        float len = Vector3.Magnitude(f_far[2] - f_near[0]);//Mathf.Max(height, width);//
        float disPerPixel = len / shadowMapResolution;

        Matrix4x4 shadowToWorld = Matrix4x4.LookAt(Vector3.zero, lightDir, Vector3.up);
        Matrix4x4 worldToShadow = shadowToWorld.inverse;

        // #TODO 这里这段的意图是???

        #region 修改 center position
        center = matTransform(worldToShadow, center, 1.0f);
        for (int i = 0; i < 3; i++)
        {
            center[i] = Mathf.Floor(center[i] / disPerPixel) * disPerPixel;
        }
        center = matTransform(shadowToWorld, center, 1.0f);
        #endregion
        
        
        // 配置相机
        camera.transform.rotation = Quaternion.LookRotation(lightDir);
        camera.transform.position = center;
        camera.nearClipPlane = -distance;
        camera.farClipPlane = distance;
        camera.aspect = width / height;
        camera.orthographicSize = height * 0.5f;
    }
    
}