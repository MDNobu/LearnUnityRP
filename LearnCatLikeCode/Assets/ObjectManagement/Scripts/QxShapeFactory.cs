using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "QxShape", menuName = "QxShapes", order = 0)]
public class QxShapeFactory : ScriptableObject
{
    [SerializeField]
    QxShape[] Prefabs;

    [SerializeField]
    Material[] Materials;

    [SerializeField]
    bool Recycle = true;

    private List<QxShape>[] Pools;

    private Scene PoolScene;
    
    private void CreatePools()
    {
        // 这段编辑器中的处理主要是为了处理重编译、热加载的问题
        if (Application.isEditor)
        {
            PoolScene = SceneManager.GetSceneByName(name);
            if (PoolScene.isLoaded)
            {
                GameObject[] rootGameObjects = PoolScene.GetRootGameObjects();
                foreach (var gameObject in rootGameObjects)
                {
                    QxShape poolShape = gameObject.GetComponent<QxShape>();
                    if (poolShape && !poolShape.gameObject.activeSelf)
                    {
                        Pools[poolShape.ShapeId].Add(poolShape);
                    }
                }
                return;
            }
        }

        PoolScene = SceneManager.CreateScene(name);
        Pools = new List<QxShape>[Prefabs.Length];
        for (int i = 0; i < Pools.Length; i++)
        {
            Pools[i] = new List<QxShape>();
        }   
    }

    public QxShape Get(int shapeId, int materialId = 0)
    {
        QxShape shape = null;
        if (Recycle)
        {
            if (Pools == null)
            {
                CreatePools();
            }

            List<QxShape> pool = Pools[shapeId];
            int lastIndex = pool.Count - 1;
            if (lastIndex >= 0)
            {
                shape = pool[lastIndex];
                shape.gameObject.SetActive(true);
                pool.RemoveAt(lastIndex);
            }
            else
            {
                shape = Instantiate(Prefabs[shapeId]);
                shape.ShapeId = shapeId;
                SceneManager.MoveGameObjectToScene(
                    shape.gameObject,
                    PoolScene
                    );
            }
        }
        else
        {
            shape = Instantiate(Prefabs[shapeId]);
            shape.ShapeId = shapeId;
            
        }
        // return Instantiate(Prefabs[shapeId]);
        shape.SetMaterial(Materials[materialId], materialId);
        
        return shape;
    }


    public void Reclaim(QxShape shapeToRecycle)
    {
        if (Recycle)
        {
            if (Pools == null)
            {
                CreatePools();
            }

            Pools[shapeToRecycle.ShapeId].Add(shapeToRecycle);
            shapeToRecycle.gameObject.SetActive(false);
        }
        else
        {
            Destroy(shapeToRecycle.gameObject);
        }
    }
    
    public QxShape GetRandom()
    {
        return Get(Random.Range(0, Prefabs.Length),
            Random.Range(0, Materials.Length));
    }
    
    
}