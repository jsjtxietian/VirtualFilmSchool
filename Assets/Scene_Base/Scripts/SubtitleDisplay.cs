using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SubTitles
{
    public int minStart;
    public float secStart;
    public int minEnd;
    public float secEnd;
    public float secStartTotal, secEndTotal;
    public string subtitle;
    public void convertToTotal()
    {
        secStartTotal = minStart * 60 + secStart;
        secEndTotal = minEnd * 60 + secEnd;
    }
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(SubTitles))]
public class subTitleDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        var minStartRect = new Rect(position.x, position.y, 30, position.height);
        var secStartRect = new Rect(position.x + 35, position.y, 30, position.height);
        var minEndRect = new Rect(position.x + 75, position.y, 30, position.height);
        var secEndRect = new Rect(position.x + 110, position.y, 30, position.height);
        var subRect = new Rect(position.x + 150, position.y, position.width - 150, position.height);

        // Draw fields - passs GUIContent.none to each so they are drawn without labels
        EditorGUI.PropertyField(minStartRect, property.FindPropertyRelative("minStart"), GUIContent.none);
        EditorGUI.PropertyField(secStartRect, property.FindPropertyRelative("secStart"), GUIContent.none);
        EditorGUI.PropertyField(minEndRect, property.FindPropertyRelative("minEnd"), GUIContent.none);
        EditorGUI.PropertyField(secEndRect, property.FindPropertyRelative("secEnd"), GUIContent.none);
        EditorGUI.PropertyField(subRect, property.FindPropertyRelative("subtitle"), GUIContent.none);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}

#endif

public class SubtitleDisplay : MonoBehaviour {

    public SubTitles[] subs;
    public float passedTime;
    public TextMesh targetText,targetText2;

    // Use this for initialization
    void Start () {
        foreach (var temp in subs) temp.convertToTotal();
    }

    // Update is called once per frame
    void Update () {
        passedTime += Time.deltaTime;
        targetText.text = targetText2.text="";
        foreach (var temp in subs)
        { 
            if (passedTime >= temp.secStartTotal && passedTime <= temp.secEndTotal) targetText.text = targetText2.text=temp.subtitle;
        }
    }
}
