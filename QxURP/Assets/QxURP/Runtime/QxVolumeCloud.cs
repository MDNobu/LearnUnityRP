using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class QxVolumeCloud : ScriptableRendererFeature
{
    // 分帧渲染的块数
    public enum FrameBlock
    {
        _Off = 1,
        _2x2 = 4,
        _4x4 = 16
    }

    [Serializable]
    public class Setting
    {
        // 后处理材质
        public Material cloudMaterial;

        // 渲染队列
        public RenderPassEvent RenderQueue = RenderPassEvent.AfterRenderingSkybox;

        // 蓝噪声 #TODO 弄明白什么是蓝噪声纹理
        public Texture2D BlueNoiseTex;

        // 分辨率缩放
        [Range(0.1f, 1)]
        public float RTScale = 0.5f;

        // 分帧渲染
        public FrameBlock FrameBlocking = FrameBlock._Off;

        // 屏幕相机分辨率宽度，受纹理缩放影响
        [Range(100, 600)]
        public int ShieldWidth = 400;

        // 是否开启分帧测试
        public bool IsFrameDebug = false;

        // 分帧测试
        [Range(1, 16)]
        public int FrameDebug = 1;
    }

    class VolumeCloudRenderPass : ScriptableRenderPass
    {
        
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            throw new System.NotImplementedException();
        }
    }
    
    public override void Create()
    {
        throw new System.NotImplementedException();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        throw new System.NotImplementedException();
    }
}