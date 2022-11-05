using System;
using UnityEngine;

public class QxShape : QxPersistableObject
{
    private static int colorPropertyId = Shader.PropertyToID("_Color");
    private static MaterialPropertyBlock sharedPropertyBlock;
    
    private int shapeId = int.MinValue;
    private Color color;

    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public int ShapeId
    {
        get
        {
            return shapeId;
        }
        set
        {
            if (shapeId == int.MinValue && value != int.MinValue)
            {
                shapeId = value;
            }
            else
            {
                Debug.LogError("Not allowed to change shapeid");
            }
        }
    }

    public void SetMaterial(Material material, int materialId)
    {
        meshRenderer.material = material;
        MaterialId = materialId;
    }

    public void SetColor(Color color)
    {
        this.color = color;
        // meshRenderer.material.color = color;
        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock  = new MaterialPropertyBlock();
        }
        sharedPropertyBlock.SetColor(colorPropertyId, color);
        meshRenderer.SetPropertyBlock(sharedPropertyBlock);
    }

    public override void Save(QxGameDataWriter writer)
    {
        base.Save(writer);
        writer.Write(color);
    }

    public override void Load(QxGameDataReader reader)
    {
        base.Load(reader);
        SetColor(reader.ReadColor());
    }

    public int MaterialId
    {
        get;
        private set;
    }

    
}