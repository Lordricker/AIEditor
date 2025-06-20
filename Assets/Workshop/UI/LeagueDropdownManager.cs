using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LeagueDropdownManager : MonoBehaviour
{
    [System.Serializable]
    public class LeagueDropdown
    {
        public Button leagueButton; // The top-level league button
        public GameObject arenaListPanel; // The panel containing arena buttons for this league
        public Transform roundButtonContainer; // Container for dynamically created round buttons
        public Button roundButtonPrefab; // Prefab for round buttons
        public string leagueName; // League1, League2, etc.
    }

    [Header("UI Configuration")]
    public List<LeagueDropdown> leagues; // Assign in Inspector
    public string arenaSceneName = "Arena1"; // Scene to load for battles
    
    [Header("Enemy Data Path")]
    public string enemyDataPath = "Workshop/TankSlotData/Enemies";
    
    private string selectedLeague = "";
    private string selectedRound = "";

    void Start()
    {
        SetupLeagueButtons();
        PopulateRounds();
    }
    
    void SetupLeagueButtons()
    {
        for (int i = 0; i < leagues.Count; i++)
        {
            int index = i; // Capture index for closure
            leagues[i].leagueButton.onClick.AddListener(() => OnLeagueButtonClicked(index));
            // Start with all collapsed
            if (leagues[i].arenaListPanel != null)
                leagues[i].arenaListPanel.SetActive(false);
        }
    }
    
    void PopulateRounds()
    {
        foreach (var league in leagues)
        {
            PopulateRoundsForLeague(league);
        }
    }
    
    void PopulateRoundsForLeague(LeagueDropdown league)
    {
        if (league.roundButtonContainer == null || league.roundButtonPrefab == null)
        {
            Debug.LogWarning($"[LeagueDropdownManager] Missing roundButtonContainer or roundButtonPrefab for {league.leagueName}");
            return;
        }
            
        // Clear existing round buttons
        foreach (Transform child in league.roundButtonContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Find available rounds for this league
        string leaguePath = $"Assets/{enemyDataPath}/{league.leagueName}";
        
        #if UNITY_EDITOR
        if (Directory.Exists(leaguePath))
        {
            string[] roundFolders = Directory.GetDirectories(leaguePath);
            
            if (roundFolders.Length == 0)
            {
                Debug.LogWarning($"[LeagueDropdownManager] No rounds found in {leaguePath}");
                return;
            }
            
            foreach (string roundFolder in roundFolders)
            {
                string roundName = Path.GetFileName(roundFolder);
                
                // Count enemy tanks in this round
                string[] enemyTanks = AssetDatabase.FindAssets("t:TankSlotData", new[] { roundFolder });
                
                if (enemyTanks.Length == 0)
                {
                    Debug.LogWarning($"[LeagueDropdownManager] No enemy tanks found in {roundFolder}");
                    continue;
                }
                
                // Create round button
                Button roundButton = Instantiate(league.roundButtonPrefab, league.roundButtonContainer);
                
                // Set button text
                Text buttonText = roundButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = $"{roundName}\n({enemyTanks.Length} enemies)";
                }
                
                // Add click listener
                string capturedLeague = league.leagueName;
                string capturedRound = roundName;
                roundButton.onClick.AddListener(() => OnRoundSelected(capturedLeague, capturedRound));
                
                Debug.Log($"[LeagueDropdownManager] Created round button for {capturedLeague}/{capturedRound} with {enemyTanks.Length} enemies");
            }
        }
        else
        {
            Debug.LogWarning($"[LeagueDropdownManager] League folder not found: {leaguePath}");
        }
        #else
        Debug.LogWarning("[LeagueDropdownManager] Dynamic round loading only works in the Unity Editor. Please manually assign rounds for builds.");
        #endif
    }

    void OnLeagueButtonClicked(int clickedIndex)
    {
        for (int i = 0; i < leagues.Count; i++)
        {
            if (leagues[i].arenaListPanel != null)
                leagues[i].arenaListPanel.SetActive(i == clickedIndex && !leagues[i].arenaListPanel.activeSelf);
        }
    }
    
    void OnRoundSelected(string leagueName, string roundName)
    {
        selectedLeague = leagueName;
        selectedRound = roundName;
        
        Debug.Log($"[LeagueDropdownManager] Selected {leagueName}/{roundName}");
        
        // Set singleplayer mode and selected round
        PlayerPrefs.SetString("BattleMode", "singleplayer");
        PlayerPrefs.SetString("SelectedLeague", leagueName);
        PlayerPrefs.SetString("SelectedRound", roundName);
        PlayerPrefs.Save();
        
        // Load arena scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(arenaSceneName);
    }

    // Legacy method - now loads with selected league/round
    public void OnArenaButtonClicked(string sceneName)
    {
        if (!string.IsNullOrEmpty(selectedLeague) && !string.IsNullOrEmpty(selectedRound))
        {
            OnRoundSelected(selectedLeague, selectedRound);
        }
        else
        {
            // Fallback to direct scene load
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
    
    /// <summary>
    /// Refresh round buttons (useful when enemy tanks are added/removed)
    /// </summary>
    [ContextMenu("Refresh Rounds")]
    public void RefreshRounds()
    {
        PopulateRounds();
    }
}
