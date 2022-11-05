using System;
using System.IO;
using UnityEngine;
public class QxPersistentStorage : MonoBehaviour
{
    void Awake()
    {
        savePath = Path.Combine(Application.persistentDataPath, "saveFile");
    }

    public void Save(QxPersistableObject o)
    {
        using (
            var writer = new BinaryWriter(File.Open(savePath, FileMode.Create))
            )
        {
            o.Save(new QxGameDataWriter(writer));
        }
    }

    public void Load(QxPersistableObject o)
    {
        using (
            var reader = new BinaryReader(File.Open(savePath, FileMode.Open))
            )
        {
            o.Load(new QxGameDataReader(reader));
        }
    }

    private string savePath;
}