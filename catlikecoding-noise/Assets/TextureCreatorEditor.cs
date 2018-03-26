using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TextureCreator))]
public class TextureCreatorInspector : Editor
{
    private TextureCreator _creator;

    private void OnEnable()
    {
        _creator = (TextureCreator) target;
        Undo.undoRedoPerformed += RefreshCreator;
    }

    private void OnDisable()
    {
        // ReSharper disable once DelegateSubtraction
        Undo.undoRedoPerformed -= RefreshCreator;
    }

    private void RefreshCreator()
    {
        if (Application.isPlaying)
        {
            _creator.FillTexture();
        }
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck()) RefreshCreator();
    }
}