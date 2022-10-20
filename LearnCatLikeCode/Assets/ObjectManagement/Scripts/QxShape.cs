using System;
using UnityEngine;

public class QxShape : PersistableObject
{
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
                Debug.LogError("Not allow to change shapeId");
            }
        }
    }

    private int shapeId = int.MinValue;

    private Color color;


    private MeshRenderer meshRender;
    
    public int MaterialId
    {
        get;
        private set;
    }

    private void Awake()
    {
        meshRender = GetComponent<MeshRenderer>();
    }

    public void SetMaterial(Material material, int materialId)
    {
        meshRender.material = material;
        MaterialId = materialId;
    }

    private static int colorPropertyId = Shader.PropertyToID("_Color");

    private static MaterialPropertyBlock sharedPropertyBlock;

    public void SetColor(Color color)
    {
        this.color = color;
        // meshRender.material.color = color;
        // 上面和下面这两种更新参数的方式有什么不同
        // var propertyBlock = new MaterialPropertyBlock();
        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }
        sharedPropertyBlock.SetColor(colorPropertyId, color);
        meshRender.SetPropertyBlock(sharedPropertyBlock);
    }

    public override void Save(GameDataWriter writer)
    {
        base.Save(writer);
        writer.Write(color);
    }

    public override void Load(GameDataReader reader)
    {
        base.Load(reader);
        SetColor(reader.ReadColor());
    }
}