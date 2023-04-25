using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QxTestPostProcess : MonoBehaviour
{
    private Shader testShader;
    private Material testMaterial;
    
    // Start is called before the first frame update
    void Start()
    {
        testShader = Shader.Find("Custom/QxTestIntegrate");
        testMaterial = new Material(testShader);
    }

    // Update is called once per frame
    void OnRenderImage(RenderTexture InImg, RenderTexture OutImg)
    {
        if (testShader != null)
        {
            Graphics.Blit(InImg, OutImg, testMaterial);
        }
    }
}
