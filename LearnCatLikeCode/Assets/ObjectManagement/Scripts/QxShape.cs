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

    public int MaterialId
    {
        get;
        private set;
    }

    public void SetMaterial(Material material, int materialId)
    {
        GetComponent<MeshRenderer>().material = material;
        MaterialId = materialId;
    }
}