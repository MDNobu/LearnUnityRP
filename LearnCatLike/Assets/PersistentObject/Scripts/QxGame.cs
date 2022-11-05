using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class QxGame : QxPersistableObject
{
    public KeyCode createKey = KeyCode.C;
    public KeyCode newGameKey = KeyCode.N;
    public KeyCode saveKey = KeyCode.S;
    public KeyCode loadKey = KeyCode.L;
    public KeyCode destroyKey = KeyCode.X;

    public QxPersistableObject prefab;
    private List<QxShape> shapes;

    public QxPersistentStorage storage;
    public QxShapeFactory shapeFactory;

    private float creationProgress, destructionProgress;
    // private const int saveVersion = 1;

    public float CreationSpeed { get; set; }
    public float DestructionSpeed { get; set; }

    public int levelCount;

    private int loadedLevelBuildIndex = -1;
    
    void Start()
    {
        shapes = new List<QxShape>();

        if (Application.isEditor)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene targetLevel = SceneManager.GetSceneAt(i);
                if (targetLevel.name.Contains("Level "))
                {
                    SceneManager.SetActiveScene(targetLevel);
                    loadedLevelBuildIndex = targetLevel.buildIndex;
                    return;
                }
            }
            
        }
        
        StartCoroutine(LoadeLevel(1));
    }


    
    
    // Update is called once per frame
    void Update()
    {
        HandleInput();

        creationProgress += Time.deltaTime * CreationSpeed;
        while (creationProgress >= 1f)
        {
            creationProgress--;
            CreateShape();
        }

        destructionProgress += Time.deltaTime * DestructionSpeed;
        while (destructionProgress >= 1f)
        {
            destructionProgress--;
            DestroyShape();
        }
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(createKey))
        {
            CreateShape();
        } else if (Input.GetKeyDown(newGameKey))
        {
            BeginNewGame();
        }
        else if (Input.GetKeyDown(saveKey))
        {
            storage.Save(this);
        } else if (Input.GetKeyDown(loadKey))
        {
            BeginNewGame();
            storage.Load(this);
        } else if (Input.GetKeyDown(destroyKey))
        {
            DestroyShape();
        }
        else
        {
            for (int i = 0; i < levelCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    BeginNewGame();
                    StartCoroutine(LoadeLevel(i));
                    return;
                }
            }
        }
    }

    private void DestroyShape()
    {
        if (shapes.Count > 0)
        {
            int index = Random.Range(0, shapes.Count);
            // Destroy(shapes[index].gameObject);
            shapeFactory.Reclaim(shapes[index]);
            int lastIndex = shapes.Count - 1;
            shapes[index] = shapes[lastIndex];
            shapes.RemoveAt(lastIndex);
        }
    }

    IEnumerator LoadeLevel(int levelBuildIndex)
    {
        enabled = false;
        // SceneManager.LoadScene("Level 1", LoadSceneMode.Additive);
        // yield return null;
        if (loadedLevelBuildIndex > 0)
        {
            yield return SceneManager.UnloadSceneAsync(loadedLevelBuildIndex);
        }
        
        yield return SceneManager.LoadSceneAsync(
            levelBuildIndex, LoadSceneMode.Additive
        );
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(levelBuildIndex));
        loadedLevelBuildIndex = levelBuildIndex;
        enabled = true;
    }

    private void BeginNewGame()
    {
        for (int i = 0; i < shapes.Count; i++)
        {
            // Destroy(shapes[i].gameObject);
            shapeFactory.Reclaim(shapes[i]);
        }
        shapes.Clear();
    }

    private void CreateShape()
    {
        // QxPersistableObject o = Instantiate(prefab);
        
        QxShape instance = shapeFactory.GetRandom();
        Transform t = instance.transform;
        t.localPosition = Random.insideUnitSphere * 5f;
        t.localRotation = Random.rotation;
        t.localScale = Random.Range(0.1f, 1f) * Vector3.one;
        instance.SetColor(Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.25f, 1f, 1f, 1f));
        shapes.Add(instance);
    }

    public override void Save(QxGameDataWriter writer)
    {
        // writer.Write(saveVersion);
        writer.Write(shapes.Count);
        writer.Write(loadedLevelBuildIndex);
        for (int i = 0; i < shapes.Count; i++)
        {
            writer.Write(shapes[i].ShapeId);
            writer.Write(shapes[i].MaterialId);
            shapes[i].Save(writer);
        }
    }

    public override void Load(QxGameDataReader reader)
    {
        int count = reader.ReadInt();
        int levelIndex = reader.ReadInt();
        StartCoroutine(LoadeLevel(levelIndex));
        for (int i = 0; i < count; i++)
        {
            int shapeId = reader.ReadInt();
            int materialId = reader.ReadInt();
            QxShape instance = shapeFactory.Get(shapeId);
            instance.Load(reader);
            shapes.Add(instance);
        }
    }
}
