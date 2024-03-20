using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class SetEditor : MonoBehaviour
{
    bool started = false;

    static void SetGameView(int sizeIndex)
    {
#if UNITY_EDITOR
        var assembly = typeof(Editor).Assembly;
        var gameView = assembly.GetType("UnityEditor.GameView");
        var instance = EditorWindow.GetWindow(gameView);
        gameView.GetMethod("SizeSelectionCallback").Invoke(instance, new object[] { sizeIndex, null });
#endif
    }

    void OnEnable()
    {
        if (!started)
        {
            SetGameView(1); // option 1 is 16:9 aspect
            started = true;
        }
    }
}
