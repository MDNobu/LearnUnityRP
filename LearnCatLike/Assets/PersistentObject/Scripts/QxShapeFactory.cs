using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "QxShapeFactory", menuName = "QxAssets", order = 0)]
public class QxShapeFactory : ScriptableObject
{
    [SerializeField]
    private QxShape[] prefabs;

    [SerializeField]
    private Material[] materials;

    [SerializeField]
    private bool recycle;

    private List<QxShape>[] pools;

    private Scene poolScene;

    public QxShape GetRandom()
    {
        return Get(Random.Range(0, prefabs.Length), Random.Range(0, materials.Length));
    }

    public QxShape Get(int shapeId = 0, int materialId = 0)
    {
        QxShape instance = null;
        if (recycle)
        {
            if (pools == null)
            {
                CreatPools();
            }

            List<QxShape> pool = pools[shapeId];
            if (pool.Count > 0)
            {
                int lastIndex = pool.Count - 1;
                instance = pool[lastIndex];
                instance.gameObject.SetActive(true);
                pool.RemoveAt(lastIndex);
            }
            else
            {
                instance = Instantiate(prefabs[shapeId]);
                instance.ShapeId = shapeId;
                SceneManager.MoveGameObjectToScene(
                    instance.gameObject,
                    poolScene
                    );
            }
        }
        else
        {
            instance = Instantiate(prefabs[shapeId]);
            instance.ShapeId = shapeId;
        }
        instance.SetMaterial(materials[materialId], materialId);
        return instance;
    }

    public void Reclaim(QxShape shapeToRecycle)
    {
        if (recycle)
        {
            if (pools == null)
            {
                CreatPools();
            }

            pools[shapeToRecycle.ShapeId].Add(shapeToRecycle);
            shapeToRecycle.gameObject.SetActive(false);
        }
        else
        {
            Destroy(shapeToRecycle.gameObject);
        }
    }

    private void CreatPools()
    {
        pools = new List<QxShape>[prefabs.Length];
        for (int i = 0; i < pools.Length; i++)
        {
            pools[i] = new List<QxShape>();
        }

        if (Application.isEditor)
        {
            poolScene = SceneManager.GetSceneByName(name);
            if (poolScene.isLoaded)
            {
                GameObject[] rootObjects = poolScene.GetRootGameObjects();
                for (int i = 0; i < rootObjects.Length; i++)
                {
                    QxShape pooledShape = rootObjects[i].GetComponent<QxShape>();
                    if (!pooledShape.gameObject.activeSelf)
                    {
                        pools[pooledShape.ShapeId].Add(pooledShape);
                    }
                }
                return; 
            }
        }
        poolScene = SceneManager.CreateScene(name);
    }
}