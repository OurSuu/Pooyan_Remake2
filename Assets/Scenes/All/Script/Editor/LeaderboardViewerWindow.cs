using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class LeaderboardViewerWindow : EditorWindow
{
    private LeaderboardData currentData;
    private Vector2 scrollPos;

    [MenuItem("Pooyan/Leaderboard Database Viewer")]
    public static void ShowWindow()
    {
        GetWindow<LeaderboardViewerWindow>("Leaderboard DB");
    }

    private void OnEnable()
    {
        RefreshData();
    }

    private void RefreshData()
    {
        if (PlayerPrefs.HasKey("Pooyan_Leaderboard"))
        {
            string json = PlayerPrefs.GetString("Pooyan_Leaderboard");
            currentData = JsonUtility.FromJson<LeaderboardData>(json);
        }
        else
        {
            currentData = null;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Local Leaderboard Database", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Refresh Data", GUILayout.Height(30)))
        {
            RefreshData();
        }

        if (currentData == null || currentData.entries == null || currentData.entries.Count == 0)
        {
            EditorGUILayout.HelpBox("No leaderboard data found in Registry (PlayerPrefs).", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Rank", GUILayout.Width(50));
        GUILayout.Label("Name", GUILayout.Width(100));
        GUILayout.Label("Score", GUILayout.Width(100));
        GUILayout.Label("Stage", GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < currentData.entries.Count; i++)
        {
            var entry = currentData.entries[i];
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"#{i + 1}", GUILayout.Width(50));
            entry.playerName = EditorGUILayout.TextField(entry.playerName, GUILayout.Width(100));
            entry.score = EditorGUILayout.IntField(entry.score, GUILayout.Width(100));
            entry.stage = EditorGUILayout.IntField(entry.stage, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);
        if (GUILayout.Button("Save Changes to Database", GUILayout.Height(30)))
        {
            string json = JsonUtility.ToJson(currentData);
            PlayerPrefs.SetString("Pooyan_Leaderboard", json);
            PlayerPrefs.Save();
            Debug.Log("Leaderboard updated via Editor Window.");
        }
        
        if (GUILayout.Button("Clear All Data (Reset to Default)", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Warning", "Are you sure you want to clear the leaderboard?", "Yes", "Cancel"))
            {
                PlayerPrefs.DeleteKey("Pooyan_Leaderboard");
                PlayerPrefs.Save();
                RefreshData();
            }
        }
    }
}

