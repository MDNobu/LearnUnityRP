using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QxInstanceData))]
public class QxInstanceDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        QxInstanceData instanceData = target as QxInstanceData;
        if (GUILayout.Button("Generate data randomly", GUILayout.Height(40)))
        {
            instanceData.GenerateRandomData();
        }
    }
}