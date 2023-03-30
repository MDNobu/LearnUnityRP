using UnityEngine;
using UnityEngine.Rendering;

public class QxLighting 
{
    public void Setup(ScriptableRenderContext context)
    {
        CommandBuffer buffer = new CommandBuffer();
        buffer.name = "SetupLighting";

        buffer.SetGlobalColor("_TestLightColor", Color.red);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}