using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "QxShape", menuName = "QxShapes", order = 0)]
public class QxShapeFactory : ScriptableObject
{
    [SerializeField]
    QxShape[] Prefabs;

    public QxShape Get(int shapeId)
    {
        return Instantiate(Prefabs[shapeId]);
    }

    public QxShape GetRandom()
    {
        return Get(Random.Range(0, Prefabs.Length));
    }
    
    
}