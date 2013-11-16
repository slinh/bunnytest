using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(BroadcastGUI))]
public class BroadcastGUIInspector : Editor 
{
    public override void OnInspectorGUI() 
	{
        BroadcastGUI gui = base.target as BroadcastGUI;
        if (gui == null)
		{
			return;
		}

        gui.UserName = PresentTextField("Username", gui.UserName);
        gui.Password = PresentTextField("Password", gui.Password);

        gui.BroadcastFramesPerSecond = PresentIntField("Broadcast Frames Per Second", gui.BroadcastFramesPerSecond);
        gui.CalculateParamsFromBitrate = PresentBooleanField("Calculate Params From Bitrate", gui.CalculateParamsFromBitrate);

        if (gui.CalculateParamsFromBitrate)
        {
            gui.TargetBitrate = PresentIntField("Target Bitrate", gui.TargetBitrate);
            gui.BroadcastAspectRatio = PresentFloatField("Broadcast Aspect Ratio (w/h)", gui.BroadcastAspectRatio);
            gui.BroadcastBitsPerPixel = PresentFloatField("Broadcast Bits Per Pixel", gui.BroadcastBitsPerPixel);
        }
        else
        {
            gui.BroadcastWidth = PresentIntField("Broadcast Width", gui.BroadcastWidth);
            gui.BroadcastHeight = PresentIntField("Broadcast Height", gui.BroadcastHeight);
        }

        if (GUI.changed)
		{
            EditorUtility.SetDirty(target);
		}
    }

    protected string PresentTextField(string name, string value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(name, new GUILayoutOption[] { GUILayout.Width(180) });
        string result = EditorGUILayout.TextField(value);
        EditorGUILayout.EndHorizontal();
        return result;
    }

    protected float PresentFloatField(string name, float value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(name, new GUILayoutOption[] { GUILayout.Width(180) });
        float result = EditorGUILayout.FloatField(value);
        EditorGUILayout.EndHorizontal();
        return result;
    }

    protected int PresentIntField(string name, int value)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(name, new GUILayoutOption[] { GUILayout.Width(180) });
        int result = EditorGUILayout.IntField(value);
        EditorGUILayout.EndHorizontal();
        return result;
    }
	
	protected bool PresentBooleanField(string name, bool value)
	{
		EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(name, new GUILayoutOption[] { GUILayout.Width(180) });
		bool result = EditorGUILayout.Toggle(value);
		EditorGUILayout.EndHorizontal();
		return result;
	}
}

