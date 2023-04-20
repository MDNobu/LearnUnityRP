using System;
using UnityEngine;
using UnityEngine.Rendering;

public class QxInstanceDrawer
{

    public static void Draw(QxInstanceData instanceData, 
        Camera inCamera, ComputeShader cs,
        RenderTexture hizBuffer,
        Matrix4x4 vpMatrix, ref CommandBuffer cmdBuffer)
    {
        if (instanceData == null ||
            inCamera == null ||
            cs == null)
        {
            return;
        }
        CheckAndInit(instanceData);
        
        // 清空args buffer中instance的计数
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        instanceData.argsBuffer.GetData(args);
        args[1] = 0;
        instanceData.argsBuffer.SetData(args);
        instanceData.validMatrixBuffer.SetCounterValue(0);

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(inCamera);
        Vector4[] planes = new Vector4[6];
        for (int i = 0; i < 6; i++)
        {
            planes[i] = new Vector4(
                frustumPlanes[i].normal.x, frustumPlanes[i].normal.y, frustumPlanes[i].normal.z,
                frustumPlanes[i].distance);
        }

        Vector4[] bounds = BoundToPoints(instanceData.instanceMesh.bounds);

        int kid = cs.FindKernel("InstanceCulling");
        cs.SetMatrix("_vpMatrix", vpMatrix);
        cs.SetVectorArray("_bounds", bounds);
        cs.SetVectorArray("_planes", planes);
        cs.SetInt("_size", hizBuffer.width);
        cs.SetInt("_instanceCount", instanceData.instanceCount);
        cs.SetBuffer(kid, "_matrixBuffer", instanceData.matrixBuffer);
        cs.SetBuffer(kid, "_validMatrixBuffer", instanceData.validMatrixBuffer);
        cs.SetBuffer(kid, "_argsBuffer", instanceData.argsBuffer);
        cs.SetTexture(kid, "_hizBuffer", hizBuffer);

        // 剔除
        int nDispatch = (int)MathF.Ceiling((float)instanceData.instanceCount / 128.0f);
        cs.Dispatch(kid, nDispatch, 1, 1);
        cmdBuffer.DrawMeshInstancedIndirect(
            instanceData.instanceMesh,
            instanceData.subMeshIndex,
            instanceData.instanceMaterial,
            -1, 
            instanceData.argsBuffer);
    }
    
    public static void Draw(QxInstanceData instanceData)
    {
        if (instanceData == null)
        {
            return;
        }
        CheckAndInit(instanceData);

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        instanceData.argsBuffer.GetData(args);
        args[1] = (uint)instanceData.instanceCount;
        instanceData.argsBuffer.SetData(args);
        
        instanceData.instanceMaterial.SetBuffer("_validaMatrixBuffer", instanceData.validMatrixBuffer);
        
        Graphics.DrawMeshInstancedIndirect(
            instanceData.instanceMesh,
            instanceData.subMeshIndex,
            instanceData.instanceMaterial,
            new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)),
            instanceData.argsBuffer
            );
    }

    public static void Draw(QxInstanceData instanceData, Camera inCamera, ComputeShader cs)
    {
        if (instanceData == null || inCamera == null || cs == null)
        {
            return;
        }

        CheckAndInit(instanceData);
        
        // 清空绘制计数
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        instanceData.argsBuffer.GetData(args);
        args[1] = 0;
        instanceData.argsBuffer.SetData(args);
        instanceData.validMatrixBuffer.SetCounterValue(0);
        
        // 计算视锥的平面
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(inCamera);
        Vector4[] planes = new Vector4[6];
        for (int i = 0; i < 6; i++)
        {
            planes[i] = new Vector4(frustumPlanes[i].normal.x, frustumPlanes[i].normal.y, frustumPlanes[i].normal.z,
                frustumPlanes[i].distance);
        }
        
        // 计算bounding box
        Vector4[] bounds = BoundToPoints(instanceData.instanceMesh.bounds);

        int kid = cs.FindKernel("InstanceCulling");
        cs.SetVectorArray("_bounds", bounds);
        cs.SetVectorArray("_planes", planes);
        cs.SetInt("_instanceCount", instanceData.instanceCount);
        cs.SetBuffer(kid, "_matrixBuffer", instanceData.matrixBuffer);
        cs.SetBuffer(kid, "_validMatrixBuffer", instanceData.validMatrixBuffer);
        cs.SetBuffer(kid, "_argsBuffer", instanceData.argsBuffer);
        
        // 视锥剔除
        int nDispatch = (instanceData.instanceCount / 128) + 1; // 128个instance 一个thread group
        cs.Dispatch(kid, nDispatch, 1, 1);
        
        instanceData.instanceMaterial.SetBuffer("_validMatrixBuffer", instanceData.validMatrixBuffer);
        
        Graphics.DrawMeshInstancedIndirect(
            instanceData.instanceMesh,
            instanceData.subMeshIndex,
            instanceData.instanceMaterial,
            new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)),
            instanceData.argsBuffer
            );
    }

    // 视锥的8个顶点
    private static Vector4[] BoundToPoints(Bounds b)
    {
        Vector4[] boundingBox = new Vector4[8];
        boundingBox[0] = new Vector4(b.min.x, b.min.y, b.min.z, 1);
        boundingBox[1] = new Vector4(b.max.x, b.max.y, b.max.z, 1);
        boundingBox[2] = new Vector4(boundingBox[0].x, boundingBox[0].y, boundingBox[1].z, 1);
        boundingBox[3] = new Vector4(boundingBox[0].x, boundingBox[1].y, boundingBox[0].z, 1);
        boundingBox[4] = new Vector4(boundingBox[1].x, boundingBox[0].y, boundingBox[0].z, 1);
        boundingBox[5] = new Vector4(boundingBox[0].x, boundingBox[1].y, boundingBox[1].z, 1);
        boundingBox[6] = new Vector4(boundingBox[1].x, boundingBox[0].y, boundingBox[1].z, 1);
        boundingBox[7] = new Vector4(boundingBox[1].x, boundingBox[1].y, boundingBox[0].z, 1);
        return boundingBox;
    }

    // 如果GPU Buffer还没有创建，那么创建它
    private static void CheckAndInit(QxInstanceData instanceData)
    {
        if (instanceData.matrixBuffer != null &&
            instanceData.validMatrixBuffer != null &&
            instanceData.argsBuffer != null)
        {
            return;
        }

        int sizeofMatrix4x4 = 4 * 4 * 4;
        instanceData.matrixBuffer = new ComputeBuffer(instanceData.instanceCount, sizeofMatrix4x4);
        instanceData.validMatrixBuffer =
            new ComputeBuffer(instanceData.instanceCount, sizeofMatrix4x4, ComputeBufferType.Append);
        instanceData.argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        
        // 传变换矩阵到GPU
        instanceData.matrixBuffer.SetData(instanceData.mats);

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 }; // 绘制参数
        if (instanceData.instanceMesh != null)
        {
            args[0] = (uint)instanceData.instanceMesh.GetIndexCount(instanceData.subMeshIndex);
            args[1] = (uint)0;
            args[2] = (uint)instanceData.instanceMesh.GetIndexStart(instanceData.subMeshIndex);
            args[3] = (uint)instanceData.instanceMesh.GetBaseVertex(instanceData.subMeshIndex);
        }
        instanceData.argsBuffer.SetData(args);
    }
}