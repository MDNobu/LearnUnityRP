using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class QxGame : PersistableObject
{

    public QxShapeFactory shapeFactory;
    public PersistentStorage storage;
    
    public PersistableObject prefab;

    public KeyCode createKey = KeyCode.C;
    public KeyCode newGameKey = KeyCode.N;
    public KeyCode saveGamekey = KeyCode.S;
    public KeyCode loadGameKey = KeyCode.L;
    public KeyCode destoryKey = KeyCode.X;

    private string savePath;
    private List<QxShape> shapes;

    private const int saveVersion = 1;

    public float CreationSpeed { get; set; }
    
    public float DestructionSpeed { get; set; }

    public int LevelCount = 2;
    
    private float creationProgress = 0;
    private float destructionProgress = 0;
    private int LoadedLevelBuildIndex = -1;

    private void Awake()
    {

    }
    // Start is called before the first frame update
    void Start()
    {
        shapes = new List<QxShape>();
        savePath = Path.Combine(Application.persistentDataPath, "saveFile");

        

        if (Application.isEditor)
        {
            // Scene loadedLevel = SceneManager.GetSceneByName("Level1");
            // if (loadedLevel.isLoaded)
            // {
            //     SceneManager.SetActiveScene(loadedLevel);
            //     return;
            // }
            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.name.Contains("Level"))
                {
                    SceneManager.SetActiveScene(loadedScene);
                    LoadedLevelBuildIndex = loadedScene.buildIndex;
                    return;
                }
            }
        }
        StartCoroutine(LoadLevel(1));
    }



    private void HandleInput()
    {
        if (Input.GetKeyDown(createKey))
        {
            CreateShape();
        } else if (Input.GetKeyDown(newGameKey))
        {
            BeginNewGame();
        } else if (Input.GetKeyDown(saveGamekey))
        {
            SaveGame();
            
            // shapeFactory.Save(this);
            storage.Save(this);
        } else if (Input.GetKeyDown(loadGameKey))
        {
            // LoadGame();
            BeginNewGame();
            storage.Load(this);
        } else if (Input.GetKeyDown(destoryKey))
        {
            DestroyShape();
        }
        else
        {
            for (int i = 1; i <= LevelCount; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    BeginNewGame();
                    StartCoroutine(LoadLevel(i));
                    return;
                }
            }
        }

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

    private void DestroyShape()
    {
        if (shapes.Count > 0)
        {
            int index = Random.Range(0, shapes.Count);
            // Destroy(shapes[index].gameObject);
            shapeFactory.Reclaim(shapes[index]);
            // shapes.RemoveAt(index);
            int lastIndex = shapes.Count - 1;
            shapes[index] = shapes[lastIndex];
            shapes.RemoveAt(lastIndex);
        }
    }

    public override void Save(GameDataWriter writer)
    {
        writer.Write(saveVersion);
        writer.Write(shapes.Count);
        for (int i = 0; i < shapes.Count; i++)
        {
            writer.Write(shapes[i].ShapeId);
            writer.Write(shapes[i].MaterialId);
            shapes[i].Save(writer);
        }
    }

    public IEnumerator LoadLevelDeprecated()
    {
        SceneManager.LoadScene("Level1", LoadSceneMode.Additive);
        // 这里这样做的目的是等一帧，因为set active scene在场景加载后才能起作用，而loadScene调用这一帧场景还没加载,下一帧场景才加载
        yield return null;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName("Level1"));
    }
    
    public IEnumerator LoadLevel(int levelBuildIndex)
    {
        if (levelBuildIndex <= 0)
        {
            Debug.LogError("Try to load ilegal level");
            yield break;
        }

        if (LoadedLevelBuildIndex > 0 && LoadedLevelBuildIndex == levelBuildIndex)
        {
            Debug.LogWarning("level:" + levelBuildIndex + " already loaded ");
            yield break;
        }
        // 这里的目的是为了防止场景加载完成前，tick/update等逻辑执行
        enabled = false;
        if (LoadedLevelBuildIndex > 0)
        {
            yield return SceneManager.UnloadSceneAsync(LoadedLevelBuildIndex);
        }
        yield return SceneManager.LoadSceneAsync(
            levelBuildIndex, LoadSceneMode.Additive
        );
        SceneManager.SetActiveScene(SceneManager.GetSceneAt(levelBuildIndex));
        LoadedLevelBuildIndex = levelBuildIndex;
        enabled = true;
    }

    public override void Load(GameDataReader reader)
    {
        int version = reader.ReadInt();
        int count = reader.ReadInt();
        for (int i = 0; i < count; i++)
        {
            int shapeId = reader.ReadInt();
            int materialId = reader.ReadInt();
            QxShape o = shapeFactory.Get(shapeId, materialId);
            // PersistableObject o = Instantiate(prefab);
            o.Load(reader);
            shapes.Add(o);
        }
    }

    private void LoadGame()
    {
        BeginNewGame();
        using (BinaryReader testReader =
               new BinaryReader(File.Open(savePath, FileMode.Open)))
        {
            int count = testReader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                // Vector3 p;
                // p.x = testReader.ReadSingle();
                // p.y = testReader.ReadSingle();
                // p.z = testReader.ReadSingle();
                // Transform t = Instantiate(prefab);
                // t.position = p;
                // _objects.Add(t);
            }
        }
    }
    

    private void SaveGame()
    {
        
       
        using ( BinaryWriter testWriter =
               new BinaryWriter(File.Open(savePath, FileMode.Create)))
        {
            testWriter.Write(shapes.Count);
            for (int i = 0; i < shapes.Count; i++)
            {
                // Transform t = _objects[i];
                // testWriter.Write(t.position.x);
                // testWriter.Write(t.position.y);
                // testWriter.Write(t.position.z);
                
            }
        }   
    }

    private void BeginNewGame()
    {
        // foreach (var testObject in _objects)
        // {
            // Destroy(testObject.gameObject);
        // }

        for (int i = 0; i < shapes.Count; i++)
        {
            // Destroy(shapes[i].gameObject);
            shapeFactory.Reclaim(shapes[i]);
        }
        shapes.Clear();
    }

    private void CreateShape()
    {
        // Transform t = Instantiate(prefab);
        // t.localPosition = Random.insideUnitSphere * 5f;
        // t.localRotation = Random.rotation;
        // t.localScale = Vector3.one * Random.Range(0.1f, 1f);
        // _objects.Add(t);
        // PersistableObject o = Instantiate(prefab);
        QxShape o = shapeFactory.GetRandom();
        Transform t = o.transform;
        t.localPosition = Random.insideUnitSphere * 5f;
        t.localRotation = Random.rotation;
        t.localScale = Vector3.one * Random.Range(0.1f, 1f);
        
        o.SetColor(Random.ColorHSV(
            0f, 1f, 
            0.5f, 1f,
            0.25f, 1f, 
            1f, 1f));
        shapes.Add(o);
    }
}
