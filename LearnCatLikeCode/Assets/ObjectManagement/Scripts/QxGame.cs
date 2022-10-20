using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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

    // Start is called before the first frame update
    void Start()
    {
    }

    private void Awake()
    {
        shapes = new List<QxShape>();
        savePath = Path.Combine(Application.persistentDataPath, "saveFile");
        
    }

    // Update is called once per frame
    void Update()
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
        
    }

    private void DestroyShape()
    {
        if (shapes.Count > 0)
        {
            int index = Random.Range(0, shapes.Count);
            Destroy(shapes[index].gameObject);
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
            Destroy(shapes[i].gameObject);
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
