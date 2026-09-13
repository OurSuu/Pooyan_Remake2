using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIBuilder {
    public static void Build() {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) {
            Debug.LogError("No canvas found!");
            return;
        }
        
        var uiManager = Object.FindAnyObjectByType<UIManager>();
        if (uiManager == null) {
            Debug.LogError("No UIManager found!");
            return;
        }

        // 1. Create Title Screen
        var titlePanel = new GameObject("TitleScreenPanel", typeof(RectTransform));
        titlePanel.transform.SetParent(canvas.transform, false);
        var titleRect = titlePanel.GetComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        var titleTextGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleTextGo.transform.SetParent(titlePanel.transform, false);
        var titleText = titleTextGo.GetComponent<TextMeshProUGUI>();
        titleText.text = "POOYAN";
        titleText.fontSize = 72;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.yellow;
        var ttRect = titleTextGo.GetComponent<RectTransform>();
        ttRect.anchoredPosition = new Vector2(0, 100);
        ttRect.sizeDelta = new Vector2(400, 100);

        var pressStartGo = new GameObject("PressStartText", typeof(RectTransform), typeof(TextMeshProUGUI));
        pressStartGo.transform.SetParent(titlePanel.transform, false);
        var pressStartText = pressStartGo.GetComponent<TextMeshProUGUI>();
        pressStartText.text = "PRESS SPACE TO START";
        pressStartText.fontSize = 36;
        pressStartText.alignment = TextAlignmentOptions.Center;
        pressStartText.color = Color.white;
        var psRect = pressStartGo.GetComponent<RectTransform>();
        psRect.anchoredPosition = new Vector2(0, -50);
        psRect.sizeDelta = new Vector2(600, 50);

        // 2. Create Name Entry Screen
        var nameEntryPanel = new GameObject("NameEntryPanel", typeof(RectTransform), typeof(Image), typeof(NameEntryUI));
        nameEntryPanel.transform.SetParent(canvas.transform, false);
        var nameRect = nameEntryPanel.GetComponent<RectTransform>();
        nameRect.anchorMin = Vector2.zero;
        nameRect.anchorMax = Vector2.one;
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        nameEntryPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);

        var namePromptGo = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
        namePromptGo.transform.SetParent(nameEntryPanel.transform, false);
        var promptText = namePromptGo.GetComponent<TextMeshProUGUI>();
        promptText.text = "ENTER INITIALS";
        promptText.fontSize = 48;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.cyan;
        var npRect = namePromptGo.GetComponent<RectTransform>();
        npRect.anchoredPosition = new Vector2(0, 150);
        npRect.sizeDelta = new Vector2(600, 60);

        TextMeshProUGUI[] chars = new TextMeshProUGUI[3];
        for (int i=0; i<3; i++) {
            var charGo = new GameObject("Char" + i, typeof(RectTransform), typeof(TextMeshProUGUI));
            charGo.transform.SetParent(nameEntryPanel.transform, false);
            chars[i] = charGo.GetComponent<TextMeshProUGUI>();
            chars[i].text = "_";
            chars[i].fontSize = 72;
            chars[i].alignment = TextAlignmentOptions.Center;
            chars[i].color = Color.yellow;
            var cRect = charGo.GetComponent<RectTransform>();
            cRect.anchoredPosition = new Vector2(-100 + (i * 100), 0);
            cRect.sizeDelta = new Vector2(80, 80);
        }

        var nameUI = nameEntryPanel.GetComponent<NameEntryUI>();
        var nameUI_type = typeof(NameEntryUI);
        nameUI_type.GetField("charTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(nameUI, chars);
        nameUI_type.GetField("promptText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(nameUI, promptText);


        // 3. Create Leaderboard Screen
        var lbPanel = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(Image), typeof(LeaderboardUI));
        lbPanel.transform.SetParent(canvas.transform, false);
        var lbRect = lbPanel.GetComponent<RectTransform>();
        lbRect.anchorMin = Vector2.zero;
        lbRect.anchorMax = Vector2.one;
        lbRect.offsetMin = Vector2.zero;
        lbRect.offsetMax = Vector2.zero;
        lbPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.9f);

        var lbTitleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        lbTitleGo.transform.SetParent(lbPanel.transform, false);
        var lbTitleText = lbTitleGo.GetComponent<TextMeshProUGUI>();
        lbTitleText.text = "RANKING";
        lbTitleText.fontSize = 48;
        lbTitleText.alignment = TextAlignmentOptions.Center;
        lbTitleText.color = Color.red;
        var lbtRect = lbTitleGo.GetComponent<RectTransform>();
        lbtRect.anchoredPosition = new Vector2(0, 200);
        lbtRect.sizeDelta = new Vector2(600, 60);

        TextMeshProUGUI[] ranks = new TextMeshProUGUI[5];
        TextMeshProUGUI[] scores = new TextMeshProUGUI[5];
        TextMeshProUGUI[] names = new TextMeshProUGUI[5];
        TextMeshProUGUI[] stages = new TextMeshProUGUI[5];

        for (int i=0; i<5; i++) {
            float yPos = 100 - (i * 60);
            
            var rGo = new GameObject("Rank" + i, typeof(RectTransform), typeof(TextMeshProUGUI));
            rGo.transform.SetParent(lbPanel.transform, false);
            ranks[i] = rGo.GetComponent<TextMeshProUGUI>();
            ranks[i].text = (i+1) + "ST";
            ranks[i].fontSize = 32;
            ranks[i].color = Color.yellow;
            var rR = rGo.GetComponent<RectTransform>();
            rR.anchoredPosition = new Vector2(-250, yPos);
            rR.sizeDelta = new Vector2(100, 40);

            var sGo = new GameObject("Score" + i, typeof(RectTransform), typeof(TextMeshProUGUI));
            sGo.transform.SetParent(lbPanel.transform, false);
            scores[i] = sGo.GetComponent<TextMeshProUGUI>();
            scores[i].text = "00000";
            scores[i].fontSize = 32;
            scores[i].color = Color.cyan;
            var sR = sGo.GetComponent<RectTransform>();
            sR.anchoredPosition = new Vector2(-100, yPos);
            sR.sizeDelta = new Vector2(150, 40);

            var nGo = new GameObject("Name" + i, typeof(RectTransform), typeof(TextMeshProUGUI));
            nGo.transform.SetParent(lbPanel.transform, false);
            names[i] = nGo.GetComponent<TextMeshProUGUI>();
            names[i].text = "AAA";
            names[i].fontSize = 32;
            names[i].color = Color.white;
            var nR = nGo.GetComponent<RectTransform>();
            nR.anchoredPosition = new Vector2(100, yPos);
            nR.sizeDelta = new Vector2(100, 40);

            var stGo = new GameObject("Stage" + i, typeof(RectTransform), typeof(TextMeshProUGUI));
            stGo.transform.SetParent(lbPanel.transform, false);
            stages[i] = stGo.GetComponent<TextMeshProUGUI>();
            stages[i].text = "STAGE 1";
            stages[i].fontSize = 32;
            stages[i].color = Color.green;
            var stR = stGo.GetComponent<RectTransform>();
            stR.anchoredPosition = new Vector2(250, yPos);
            stR.sizeDelta = new Vector2(150, 40);
        }

        var lbUI = lbPanel.GetComponent<LeaderboardUI>();
        var lbUI_type = typeof(LeaderboardUI);
        lbUI_type.GetField("rankTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(lbUI, ranks);
        lbUI_type.GetField("scoreTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(lbUI, scores);
        lbUI_type.GetField("nameTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(lbUI, names);
        lbUI_type.GetField("stageTexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(lbUI, stages);

        // Hide initially
        titlePanel.SetActive(false);
        nameEntryPanel.SetActive(false);
        lbPanel.SetActive(false);

        // Link to UIManager
        var uim_type = typeof(UIManager);
        uim_type.GetField("titleScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiManager, titlePanel);
        uim_type.GetField("nameEntryScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiManager, nameEntryPanel);
        uim_type.GetField("leaderboardScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(uiManager, lbPanel);

        // Save scene if needed
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}

UIBuilder.Build();
