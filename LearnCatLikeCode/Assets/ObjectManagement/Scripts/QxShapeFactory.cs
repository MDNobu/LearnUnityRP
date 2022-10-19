using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "QxShape", menuName = "QxShapes", order = 0)]
public class QxShapeFactory : ScriptableObject
{
    [SerializeField]
    QxShape[] Prefabs;

    [SerializeField]
    Material[] Materials;

    public QxShape Get(int shapeId, int materialId = 0)
    {
        // return Instantiate(Prefabs[shapeId]);
        QxShape shape = Instantiate(Prefabs[shapeId]);
        shape.ShapeId = shapeId;
        shape.SetMaterial(Materials[materialId], materialId);
        return shape;
    }

    public QxShape GetRandom()
    {
        return Get(Random.Range(0, Prefabs.Length),
            Random.Range(0, Materials.Length));
    }
    
    
}