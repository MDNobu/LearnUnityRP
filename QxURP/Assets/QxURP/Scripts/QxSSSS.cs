using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;


// 这是是用在Built in管线的SSSS，完成这个之后再考虑URP
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class QxSSSS : MonoBehaviour
{
    [Range(0, 3)] public float Scaler = 0.1f;
    public Color MainColor;
    public Color Falloff;

    private Camera _renderCamera;
    private CommandBuffer _buffer;
    private Material _material;
    private List<Vector4> _KernelArray = new List<Vector4>();

    private static int SceneColorID = Shader.PropertyToID("_SceneColor");
    private static int Kernel = Shader.PropertyToID("_Kernel");
    private static int SSSScaler = Shader.PropertyToID("_SSSScale");
    
    

    private void OnEnable()
    {
        _renderCamera = GetComponent<Camera>();
        _renderCamera.depthTextureMode |= DepthTextureMode.Depth;
        _material = new Material(Shader.Find("QxRP/QxSSSS"));

        _buffer = new CommandBuffer();
        _buffer.name = "Qx Separable Subsurface Scatter";
        _renderCamera.clearStencilAfterLightingPass = true;
        _renderCamera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, _buffer);
    }

    private void OnDisable()
    {
        ClearBuffer();
    }

    private void OnPreRender()
    {
        
    }

    // 实现参考https://github.com/luxuia/separable-sss-unity
    void UpdateSubsurface()
    {
        Vector3 sssc = Vector3.Normalize(new Vector3(MainColor.r, MainColor.g, MainColor.b));
        Vector3 sssfc = Vector3.Normalize(new Vector3(Falloff.r, Falloff.g, Falloff.b));
        QxKernelCalculator.CalculateKernel(_KernelArray, 25, sssc, sssfc);
        _material.SetVectorArray(Kernel, _KernelArray);
        _material.SetFloat(SSSScaler, Scaler);
        
        _buffer.Clear();
        _buffer.GetTemporaryRT(SceneColorID, _renderCamera.pixelWidth, _renderCamera.pixelHeight,
            0, FilterMode.Trilinear, RenderTextureFormat.DefaultHDR);
        _buffer.BlitStencil(BuiltinRenderTextureType.CameraTarget, SceneColorID,
            BuiltinRenderTextureType.CameraTarget, _material, 0);
        _buffer.BlitSRT(BuiltinRenderTextureType.CameraTarget,
            BuiltinRenderTextureType.CameraTarget,
            _material, 1);
    }

    private void ClearBuffer()
    {
        _buffer.ReleaseTemporaryRT(SceneColorID);
        _renderCamera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, _buffer);
        _buffer.Release();
        _buffer.Dispose();
    }
}