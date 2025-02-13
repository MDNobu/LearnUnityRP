﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class QxGraphicUtils
{
    private static Mesh m_mesh;
    private static Mesh mesh {
        get {
            if (m_mesh != null)
                return m_mesh;
            m_mesh = new Mesh();
            m_mesh.vertices = new Vector3[] {
                new Vector3(-1,-1,0),
                new Vector3(-1,1,0),
                new Vector3(0,1,0),
                new Vector3(0,-1,0)
            };
            m_mesh.uv = new Vector2[] {
                new Vector2(0,1),
                new Vector2(0,0),
                new Vector2(0.5f,0),
                new Vector2(0.5f,1)
            };
            m_mesh.SetIndices(new int[] { 0, 1, 2, 3 }, MeshTopology.Quads, 0);
            return m_mesh;
        }
    }
    
    public static void BlitSRT(
        this CommandBuffer buffer,
        RenderTargetIdentifier source,
        RenderTargetIdentifier destination,
        Material mat,
        int pass
        )
    {
        buffer.SetGlobalTexture(QxShaderIDs._MainTex, source);
        buffer.SetRenderTarget(destination);
        buffer.DrawMesh(mesh, Matrix4x4.identity, mat, 0, pass);
    }

    public static void BlitStencil(
        this CommandBuffer buffer,
        RenderTargetIdentifier colorSrc,
        RenderTargetIdentifier colorBuffer,
        RenderTargetIdentifier depthStencilBuffer,
        Material mat,
        int pass
        )
    {
        buffer.SetGlobalTexture(QxShaderIDs._MainTex, colorSrc);
        buffer.SetRenderTarget(colorBuffer, depthStencilBuffer);
        buffer.DrawMesh(mesh, Matrix4x4.identity, mat, 0, pass);
    }
}