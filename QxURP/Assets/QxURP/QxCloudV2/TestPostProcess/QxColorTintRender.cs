using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using ColorParameter = UnityEngine.Rendering.PostProcessing.ColorParameter;
using FloatParameter = UnityEngine.Rendering.PostProcessing.FloatParameter;


[Serializable]
[PostProcess(typeof(QxColorTintRenderer), PostProcessEvent.AfterStack, "Unity/QxColorTint")]
public class QxColorTint : PostProcessEffectSettings
{
    [Tooltip("ColorTint")] public ColorParameter color = new ColorParameter() { value = new Color(1f, 1f, 1f, 1f) };

    [Range(0f, 1f), Tooltip("ColorTint intensity")]
    public FloatParameter blend = new FloatParameter() { value = 0.5f };
}

public sealed class QxColorTintRenderer : PostProcessEffectRenderer<QxColorTint>
{
    public override void Render(PostProcessRenderContext context)
    {
        CommandBuffer cmd = context.command;
        cmd.BeginSample("QxScreenColorTint");

        var sheet = context.propertySheets.Get(Shader.Find("QxCustom/PostProcessing/QxColorTint"));
        sheet.properties.SetColor("_Color", settings.color);
        sheet.properties.SetFloat("_BlendMultiply", settings.blend);
        
        context.command.BlitFullscreenTriangle(context.source, context.destination,
           sheet, 0 );
        
        
        cmd.EndSample("QxScreenColorTint");
    }
}