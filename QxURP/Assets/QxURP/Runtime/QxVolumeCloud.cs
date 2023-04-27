using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class QxVolumeCloud : ScriptableRendererFeature
{
    // 测试切换使用
    public static QxVolumeCloud Oneself;
    
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

        public Setting Set;
        public string name;
        public RenderTargetIdentifier cameraColorTex;
        // 云渲染纹理，通过2张相互迭代完成分帧渲染
        public RenderTexture[] cloudTex;
        
        // 云纹理的宽度
        public int width;
        // 云纹理的高度
        public int height;
        // 帧计数
        public int frameCount;
        // 纹理切换
        public int rtSwitch;

        public VolumeCloudRenderPass(Setting inSet, string inName)
        {
            renderPassEvent = inSet.RenderQueue;
            Set = inSet;
            name = inName;
            frameCount = 0;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(name);
            
            // 设置材质参数
            Set.cloudMaterial.SetTexture("_BlueNoiseTex", Set.BlueNoiseTex);
            Set.cloudMaterial.SetVector("_BlueNoiseTexUV", 
                new Vector4((float)width/(float)Set.BlueNoiseTex.width,
                    (float)height/ (float)Set.BlueNoiseTex.height, 0, 0
                    ));
            Set.cloudMaterial.SetInt("_Width", width - 1); // 这里为什么-1 #TODO
            Set.cloudMaterial.SetInt("_Height", height - 1);
            if (Set.FrameBlocking == FrameBlock._Off)
            {
                Set.cloudMaterial.EnableKeyword("_OFF");
                Set.cloudMaterial.DisableKeyword("_2x2");
                Set.cloudMaterial.DisableKeyword("_4x4");
            } else if (Set.FrameBlocking == FrameBlock._2x2)
            {
                Set.cloudMaterial.DisableKeyword("_OFF");
                Set.cloudMaterial.DisableKeyword("_4x4");
                Set.cloudMaterial.EnableKeyword("_2x2");
            } else if (Set.FrameBlocking == FrameBlock._4x4)
            {
                Set.cloudMaterial.DisableKeyword("_OFF");
                Set.cloudMaterial.DisableKeyword("_2x2");
                Set.cloudMaterial.EnableKeyword("_4x4");
            }

            // 如果不开启分帧渲染，我们将创建临时渲染纹理
            if (Set.FrameBlocking == FrameBlock._Off)
            {
                // 创建临时渲染纹理
                RenderTextureDescriptor tempDescriptor =
                    new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32);
                tempDescriptor.depthBufferBits = 0;
                int tempTextureID = Shader.PropertyToID("_CloudTex");
                cmd.GetTemporaryRT(tempTextureID, tempDescriptor);
                
                cmd.Blit(cameraColorTex, tempTextureID, Set.cloudMaterial, 0);
                cmd.Blit(tempTextureID, cameraColorTex, Set.cloudMaterial, 1);
                
                // 执行
                context.ExecuteCommandBuffer(cmd);
                // 释放资源
                cmd.ReleaseTemporaryRT(tempTextureID);
            }
            else // 如果开启分帧渲染，则进行2张纹理相互迭代，完成分帧渲染
            {
                cmd.Blit(cloudTex[rtSwitch % 2], cloudTex[(rtSwitch + 1) % 2], Set.cloudMaterial, 0);
                cmd.Blit(cloudTex[(rtSwitch + 1) % 2], cameraColorTex, Set.cloudMaterial, 1);
                
                context.ExecuteCommandBuffer(cmd);
            }
            
            // 释放资源
            CommandBufferPool.Release(cmd);
        }
    }


    private VolumeCloudRenderPass _cloudPass;
    public Setting Set = new Setting();
    
    // 云渲染纹理，通过2张相互迭代，完成分帧渲染
    private RenderTexture[] _cloudTex_Game = new RenderTexture[2];
    // 预览窗口和游戏视口要分开
    private RenderTexture[] _cloudTex_sceneView = new RenderTexture[2];
    
    // 上一次纹理分辨率
    private int _width_game;
    private int _height_game;
    private int _width_SceneView;
    private int _height_SceneView;
    
    // 当前帧数
    private int _frameCount_Game;
    private int _frameCount_SceneView;
    
    // 纹理切换
    private int _rtSwitchGame;
    private int _rtSwitch_SceneView;
    
    // 上一次分帧测试数值
    private int _frameDebug = 1;
    
    public override void Create()
    {
        Oneself = this;
        _cloudPass = new VolumeCloudRenderPass(Set, name);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        bool isValidCameraType = renderingData.cameraData.cameraType == CameraType.Game ||
                                 renderingData.cameraData.cameraType == CameraType.Preview;
        if (!(Set.cloudMaterial && isValidCameraType))
        {
            return;
        }
        
        // 云纹理的分辨率
        int width = (int)(renderingData.cameraData.cameraTargetDescriptor.width * Set.RTScale);
        int height = (int)(renderingData.cameraData.cameraTargetDescriptor.height * Set.RTScale);

        
        // 不进行分帧渲染
        if (Set.FrameBlocking == FrameBlock._Off)
        {
            for (int i = 0; i < 2; i++)
            {
                // 重置纹理
                RenderTexture.ReleaseTemporary(_cloudTex_Game[i]);
                RenderTexture.ReleaseTemporary(_cloudTex_sceneView[i]);
                _cloudTex_Game = new RenderTexture[2];
                _cloudTex_sceneView = new RenderTexture[2];
            }

            _cloudPass.width = width;
            _cloudPass.height = height;
            _cloudPass.cameraColorTex = renderer.cameraColorTarget;
            renderer.EnqueuePass(_cloudPass);
            return;
        }
        
        // 分帧渲染//////////////
        // 分帧调试
        if (Set.IsFrameDebug)
        {
            if (Set.FrameDebug != _frameDebug)
            {
                for (int i = 0; i < 2; i++)
                {
                    //重置纹理
                    RenderTexture.ReleaseTemporary(_cloudTex_Game[i]);
                    RenderTexture.ReleaseTemporary(_cloudTex_sceneView[i]);
                    _cloudTex_Game = new RenderTexture[2];
                    _cloudTex_sceneView = new RenderTexture[2];
                }
            }
            _frameDebug = Set.FrameDebug;
            // 分帧测试
            _frameCount_Game = _frameCount_Game % Set.FrameDebug;
            _frameCount_SceneView = _frameCount_SceneView % Set.FrameDebug;
        }
        
        // 对游戏视口和场景视口进行分别处理，内容基本一致
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            // 创建纹理
            for (int i = 0; i < _cloudTex_Game.Length; i++)
            {
                if (_cloudTex_Game[i] != null && _width_game == width && _height_game == height)
                {
                    continue;
                }
                
                //当选中相机时，右下角会有一个预览窗口，他的分辨率与当前game视口不一样，所以会进行打架
                //在这设置阈值，屏蔽掉预览窗口的变化
                if (width < Set.ShieldWidth)
                    continue;
                
                // 创建游戏视口的Render texture
                _cloudTex_Game[i] = RenderTexture.GetTemporary(width, height, 0,
                    RenderTextureFormat.ARGB32);

                _width_game = width;
                _height_game = height;
            }

            _cloudPass.cloudTex = _cloudTex_Game;
            _cloudPass.width = _width_game;
            _cloudPass.height = _height_game;
            _cloudPass.frameCount = _frameCount_Game;
            _cloudPass.rtSwitch = _rtSwitchGame;

            _rtSwitchGame = (++_rtSwitchGame) % 2;
            
            // 增加帧数
            _frameCount_Game = (++_frameCount_Game) % (int)Set.FrameBlocking;
        }
        else
        {
            //创建纹理
            for (int i = 0; i < _cloudTex_sceneView.Length; i++)
            {
                if (_cloudTex_sceneView[i] != null && _width_SceneView == width && _height_SceneView == height)
                    continue;

                //创建场景视口的渲染纹理
                _cloudTex_sceneView[i] = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);//, RenderTextureReadWrite.Default, 1);

                _width_SceneView = width;
                _height_SceneView = height;
            }

            _cloudPass.cloudTex = _cloudTex_sceneView;
            _cloudPass.width = _width_SceneView;
            _cloudPass.height = _height_SceneView;
            _cloudPass.frameCount = _frameCount_SceneView;
            _cloudPass.rtSwitch = _rtSwitch_SceneView;

            _rtSwitch_SceneView = (++_rtSwitch_SceneView) % 2;

            //增加帧数
            _frameCount_SceneView = (++_frameCount_SceneView) % (int)Set.FrameBlocking;
        }
        
        renderer.EnqueuePass(_cloudPass);
        _cloudPass.cameraColorTex = renderer.cameraColorTarget;

    }
}