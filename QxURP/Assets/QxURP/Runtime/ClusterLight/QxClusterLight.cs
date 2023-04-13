

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Rendering;
using LightType = UnityEngine.LightType;

public class QxClusterLight
{
    public static int maxNumLights = 1024;
    public static int numClusterX = 16;
    public static int numClusterY = 16;
    public static int numClusterZ = 16;
    public static int maxNumLightsPerCluster = 128;

    private static int SIZE_OF_LIGHT = 32;

    struct PointLight
    {
        public Vector3 color;
        public float intensity;
        public Vector3 position;
        public float radius;
    }

    private static int SIZE_OF_CLUSTERTBOX = 8 * 3 * 4;

    // 每个cluster 平截头体的8个顶点
    struct ClusterFrustum
    {
        public Vector3 p0, p1, p2, p3, p4, p5, p6, p7;
    }

    private static int SIZE_OF_INDEX = sizeof(int) * 2;

    struct LightIndex
    {
        public int count;
        public int start;
    }

    // 生成cluster的cs
    private ComputeShader clusterGenerateCS;

    // 给cluster分配light的cs
    private ComputeShader lightAssignCS;

    public ComputeBuffer lightBuffer; //光源列表
    public ComputeBuffer clusterBuffer; //簇列表
    public ComputeBuffer lightAssignBuffer; //光源分配结果
    public ComputeBuffer assignTable; // 光源分配索引表

    public QxClusterLight()
    {
        int numClusters = numClusterX * numClusterY * numClusterZ;

        // Assert.AreEqual(sizeof(PointLight), SIZE_OF_LIGHT);
        lightBuffer = new ComputeBuffer(maxNumLights, SIZE_OF_LIGHT);//, ComputeBufferType.Default, ComputeBufferMode.Dynamic);
        clusterBuffer = new ComputeBuffer(numClusters, SIZE_OF_CLUSTERTBOX);//, ComputeBufferType.Default, ComputeBufferMode.Dynamic);
        lightAssignBuffer = new ComputeBuffer(numClusters * maxNumLightsPerCluster, sizeof(uint));//, ComputeBufferType.Default, ComputeBufferMode.Dynamic);
        assignTable = new ComputeBuffer(numClusters, SIZE_OF_INDEX);//, ComputeBufferType.Default, ComputeBufferMode.Dynamic);

        clusterGenerateCS = Resources.Load<ComputeShader>("Shaders/QxClusterGenerate");
        lightAssignCS = Resources.Load<ComputeShader>("Shaders/QxLightAssign");
        if (clusterGenerateCS == null || lightAssignCS == null)
        {
            Debug.LogError("Do not find shader");
        }
    }

    ~QxClusterLight()
    {
        lightBuffer.Release();
        clusterBuffer.Release();
        lightAssignBuffer.Release();
        assignTable.Release();
    }
    
    public void UpdateLightBuffer(VisibleLight[] inLights)
    {
        PointLight[] pointLights = new PointLight[maxNumLights];

        int cunt = 0;
        for (int i = 0; i < maxNumLights && i < inLights.Length; i++)
        {
            Light curLight = inLights[i].light;
            if (curLight.type == LightType.Point)
            {
                PointLight pl;
                pl.color = new Vector3(curLight.color.r, curLight.color.g, curLight.color.b);
                pl.intensity = curLight.intensity;
                pl.position = curLight.transform.position;
                pl.radius = curLight.range;

                pointLights[cunt++] = pl;
            }
        }
        lightBuffer.SetData(pointLights);
        
        // 设置光源数量
        lightAssignCS.SetInt("_numLights", cunt);
    }
    
    public void UpdateLightBuffer(Light[] inLights)
    {
        PointLight[] pointLights = new PointLight[maxNumLights];

        int cunt = 0;
        for (int i = 0; i < maxNumLights && i < inLights.Length; i++)
        {
            Light curLight = inLights[i];
            if (curLight.type == LightType.Point)
            {
                PointLight pl;
                pl.color = new Vector3(curLight.color.r, curLight.color.g, curLight.color.b);
                pl.intensity = curLight.intensity;
                pl.position = curLight.transform.position;
                pl.radius = curLight.range;

                pointLights[cunt++] = pl;
            }
        }
        lightBuffer.SetData(pointLights);
        
        // 设置光源数量
        lightAssignCS.SetInt("_numLights", cunt);
    }

    public void GenerateCluster(Camera camera)
    {
        // 设置参数
        Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
        Matrix4x4 viewMatrixInv = viewMatrix.inverse;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 vpMatrix = projMatrix * viewMatrix;
        Matrix4x4 vpMatrixInv = vpMatrix.inverse;
        
        clusterGenerateCS.SetMatrix("_viewMatrix", viewMatrix);
        clusterGenerateCS.SetMatrix("_viewMatrixInv", viewMatrixInv);
        clusterGenerateCS.SetMatrix("_vpMatrix", vpMatrix);
        clusterGenerateCS.SetMatrix("_vpMatrixInv", vpMatrixInv);
        clusterGenerateCS.SetFloat("_near", camera.nearClipPlane);
        clusterGenerateCS.SetFloat("_far", camera.farClipPlane);
        clusterGenerateCS.SetFloat("_fovh", camera.fieldOfView);
        clusterGenerateCS.SetFloat("_numClusterX", numClusterX);
        clusterGenerateCS.SetFloat("_numClusterY", numClusterY);
        clusterGenerateCS.SetFloat("_numClusterZ", numClusterZ);

        int kid = clusterGenerateCS.FindKernel("ClusterGenerate");
        clusterGenerateCS.SetBuffer(kid, "_clusterBuffer", clusterBuffer);
        clusterGenerateCS.Dispatch(kid, numClusterZ, 1, 1);
    }

    public void LightAssign()
    {
        lightAssignCS.SetInt("_maxNumLightsPerCluster", maxNumLightsPerCluster);
        lightAssignCS.SetFloat("_numClusterX", numClusterX);
        lightAssignCS.SetFloat("_numClusterY", numClusterY);
        lightAssignCS.SetFloat("_numClusterZ", numClusterZ);

        int kid = lightAssignCS.FindKernel("LightAssign");
        lightAssignCS.SetBuffer(kid, "_clusterBuffer", clusterBuffer);
        lightAssignCS.SetBuffer(kid, "_lightBuffer", lightBuffer);
        lightAssignCS.SetBuffer(kid, "_lightAssignBuffer", lightAssignBuffer);
        lightAssignCS.SetBuffer(kid, "_assignTable", assignTable);
        
        lightAssignCS.Dispatch(kid, numClusterZ, 1, 1);
    }

    public void SetupShaderPrameters()
    {
        Shader.SetGlobalFloat("_numClusterX", numClusterX);
        Shader.SetGlobalFloat("_numClusterY", numClusterY);
        Shader.SetGlobalFloat("_numClusterZ", numClusterZ);
        
        Shader.SetGlobalBuffer("_lightBuffer", lightBuffer);
        Shader.SetGlobalBuffer("_lightAssignBuffer", lightAssignBuffer);
        Shader.SetGlobalBuffer("_assignTable", assignTable);
    }

    public void DebugCluster()
    {
        int numClusters = numClusterX * numClusterY * numClusterZ;
        ClusterFrustum[] frustums = new ClusterFrustum[numClusters];
        clusterBuffer.GetData(frustums, 0, 0, numClusters);

        foreach (var frustum in frustums)
        {
            DrawFrustum(frustum, Color.grey);
        }
    }

    private void DrawFrustum(ClusterFrustum box, Color color)
    {
        Debug.DrawLine(box.p0, box.p1, color);
        Debug.DrawLine(box.p0, box.p2, color);
        Debug.DrawLine(box.p0, box.p4, color);
        
        Debug.DrawLine(box.p6, box.p2, color);
        Debug.DrawLine(box.p6, box.p7, color);
        Debug.DrawLine(box.p6, box.p4, color);

        Debug.DrawLine(box.p5, box.p1, color);
        Debug.DrawLine(box.p5, box.p7, color);
        Debug.DrawLine(box.p5, box.p4, color);

        Debug.DrawLine(box.p3, box.p1, color);
        Debug.DrawLine(box.p3, box.p2, color);
        Debug.DrawLine(box.p3, box.p7, color);
    }

    public void DebugLightAssign()
    {
        int numClusters = numClusterX * numClusterY * numClusterZ;

        ClusterFrustum[] frustums = new ClusterFrustum[numClusters];
        clusterBuffer.GetData(frustums, 0, 0, numClusters);

        LightIndex[] indices = new LightIndex[numClusters];
        assignTable.GetData(indices, 0, 0, numClusters);

        uint[] assignBuffContents = new uint[numClusters * maxNumLightsPerCluster];
        lightAssignBuffer.GetData(assignBuffContents, 0, 0, numClusters * maxNumLightsPerCluster);

        Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow };

        for (int i = 0; i < indices.Length; i++)
        {
            if (indices[i].count > 0)
            {
                uint firstLightId = assignBuffContents[indices[i].start];
                DrawFrustum(frustums[i], colors[firstLightId%4]);
            }
        }
    }
}