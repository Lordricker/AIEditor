using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum GameMode
{
    Singleplayer,
    Multiplayer
}

public class ArenaManager : MonoBehaviour
{    
    [Header("Spawn Points")]
    public Transform[] spawnPoints = new Transform[15]; // Increased to support more spawn points
    public GameObject tankPrefab; // Assign modular tank prefab in Inspector
    public TankSlotData[] tankSlots = new TankSlotData[10]; // Assign ScriptableObjects in Inspector
    
    [Header("Dynamic Arena Configuration")]
    [Tooltip("Automatically load enemy tanks based on selected arena")]
    public bool useDynamicArenaLoading = true;
    [Tooltip("Current league (set by workshop UI)")]
    public string currentLeague = "League1";
    [Tooltip("Current round (set by workshop UI)")]
    public string currentRound = "Round1";
    
    [Header("Enemy Tank Configuration")]
    [Tooltip("Pre-configured enemy tanks for singleplayer mode")]
    public TankSlotData[] enemyTankSlots = new TankSlotData[10]; // Enemy-only tank configurations
    [Tooltip("Enemy spawn points (if different from player spawn points)")]
    public Transform[] enemySpawnPoints = new Transform[10];
    [Tooltip("Manually set enemy folder path (overrides dynamic loading)")]
    public string manualEnemyFolderPath = "";
    
    [Header("Game Mode Configuration")]
    [SerializeField] private GameMode gameMode = GameMode.Singleplayer;
    [SerializeField] private int playerCount = 1; // For multiplayer modes
    
    [Header("Legacy Team Layer Configuration (Deprecated)")]
    [Tooltip("Unity layer for Team A tanks - DEPRECATED: Use SimpleTeamManager instead")]
    public int teamALayer = 10; // Layer 10 for allies
    [Tooltip("Unity layer for Team B tanks - DEPRECATED: Use SimpleTeamManager instead")]
    public int teamBLayer = 11; // Layer 11 for enemies
    
    void Start()
    {
        // Load game mode configuration from PlayerPrefs (set by TeamConfigUI)
        LoadGameModeSettings();
        
        // Load arena-specific configuration
        LoadArenaConfiguration();
        
        // Load enemy tanks for this arena
        if (useDynamicArenaLoading)
        {
            LoadEnemyTanksForCurrentArena();
        }
        
        // Ensure time scale is reset to normal when arena starts (fixes pause bug)
        Time.timeScale = 1f;
        
        SpawnActiveTanks();
        
        // Assign teams after all tanks are spawned
        AssignTeams();
        
        // Refresh camera anchors after all tanks have spawned
        var camController = Object.FindFirstObjectByType<CameraController>();
        if (camController != null)
            camController.RefreshAnchors();
    }
    
    /// <summary>
    /// Simple team assignment using SimpleTeamManager
    /// </summary>
    void AssignTeams()
    {
        SimpleTeamManager teamManager = FindFirstObjectByType<SimpleTeamManager>();
        if (teamManager == null)
        {
            GameObject teamManagerObj = new GameObject("SimpleTeamManager");
            teamManager = teamManagerObj.AddComponent<SimpleTeamManager>();
        }
        
        teamManager.AssignTeamsFromBattleMode();
        Debug.Log("[ArenaManager] Teams assigned to all spawned tanks");
    }
    
    /// <summary>
    /// Load arena configuration from PlayerPrefs (set by workshop league/round selection)
    /// </summary>
    void LoadArenaConfiguration()
    {
        if (PlayerPrefs.HasKey("SelectedLeague"))
        {
            currentLeague = PlayerPrefs.GetString("SelectedLeague");
        }
        
        if (PlayerPrefs.HasKey("SelectedRound"))
        {
            currentRound = PlayerPrefs.GetString("SelectedRound");
        }
        
        Debug.Log($"[ArenaManager] Arena Configuration: {currentLeague}/{currentRound}");
    }
    
    /// <summary>
    /// Load enemy tanks specific to the current league and round
    /// </summary>
    void LoadEnemyTanksForCurrentArena()
    {
#if UNITY_EDITOR
        // Determine folder path
        string enemyFolderPath;
        if (!string.IsNullOrEmpty(manualEnemyFolderPath))
        {
            enemyFolderPath = manualEnemyFolderPath;
        }
        else
        {
            enemyFolderPath = $"Workshop/TankSlotData/Enemies/{currentLeague}/{currentRound}";
        }
        
        string fullPath = $"Assets/{enemyFolderPath}";
        
        // Clear existing enemy tanks
        System.Array.Clear(enemyTankSlots, 0, enemyTankSlots.Length);
        
        // Find all TankSlotData assets in the enemy folder
        string[] guids = AssetDatabase.FindAssets("t:TankSlotData", new[] { fullPath });
        
        if (guids.Length == 0)
        {
            Debug.LogWarning($"[ArenaManager] No enemy tanks found in {fullPath}. Make sure enemy tanks exist for {currentLeague}/{currentRound}");
            return;
        }
        
        System.Collections.Generic.List<TankSlotData> loadedEnemies = new System.Collections.Generic.List<TankSlotData>();
        
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            TankSlotData enemyTank = AssetDatabase.LoadAssetAtPath<TankSlotData>(assetPath);
            
            if (enemyTank != null && !enemyTank.isPlayerControlled)
            {
                // Ensure enemy tank is properly configured
                enemyTank.isActive = true;
                enemyTank.teamId = 1; // Enemy team
                
                loadedEnemies.Add(enemyTank);
                Debug.Log($"[ArenaManager] Loaded enemy tank: {enemyTank.displayName} ({enemyTank.name})");
            }
        }
        
        // Sort enemies by name for consistent ordering
        loadedEnemies.Sort((a, b) => string.Compare(a.name, b.name));
        
        // Assign loaded enemies to enemyTankSlots array
        for (int i = 0; i < enemyTankSlots.Length && i < loadedEnemies.Count; i++)
        {
            enemyTankSlots[i] = loadedEnemies[i];
        }
        
        Debug.Log($"[ArenaManager] Loaded {loadedEnemies.Count} enemy tanks for {currentLeague}/{currentRound}");
#else
        Debug.LogWarning("[ArenaManager] Dynamic enemy loading only works in the Unity Editor. Please manually assign enemy tanks for builds.");
#endif
    }
    
    void LoadGameModeSettings()
    {
        if (PlayerPrefs.HasKey("GameMode"))
        {
            gameMode = (GameMode)PlayerPrefs.GetInt("GameMode");
        }
        
        if (PlayerPrefs.HasKey("PlayerCount"))
        {
            playerCount = PlayerPrefs.GetInt("PlayerCount");
        }
        
        Debug.Log($"[ArenaManager] Loaded settings: Mode={gameMode}, Players={playerCount}");
    }
    
    void SpawnActiveTanks()
    {
        if (gameMode == GameMode.Singleplayer)
        {
            SpawnSingleplayerTanks();
        }
        else
        {
            SpawnMultiplayerTanks();
        }
    }
    
    void SpawnSingleplayerTanks()
    {
        // Spawn player tanks (they will get teamId from their TankSlotData)
        SpawnTankArray(tankSlots, spawnPoints, "Player");
        
        // Spawn enemy tanks (they will get teamId from their TankSlotData)
        Transform[] enemySpawns = enemySpawnPoints.Length > 0 && enemySpawnPoints[0] != null ? enemySpawnPoints : spawnPoints;
        SpawnTankArray(enemyTankSlots, enemySpawns, "Enemy", spawnPoints.Length);
    }
    
    void SpawnMultiplayerTanks()
    {
        // Just spawn all active tanks - they'll get teamId from TankSlotData
        SpawnTankArray(tankSlots, spawnPoints, "Player");
    }
    
    void SpawnTankArray(TankSlotData[] slots, Transform[] spawns, string tankType, int spawnIndexOffset = 0)
    {
        for (int i = 0; i < slots.Length && i < spawns.Length; i++)
        {
            if (slots[i] != null && slots[i].isActive && slots[i].engineFramePrefab != null)
            {
                int spawnIndex = (i + spawnIndexOffset) % spawns.Length;
                if (spawns[spawnIndex] == null) continue;
                
                GameObject tank = Instantiate(tankPrefab, spawns[spawnIndex].position, spawns[spawnIndex].rotation);
                
                // Set the tank's name to include team and type information
                string tankName = !string.IsNullOrEmpty(slots[i].displayName) ? slots[i].displayName : $"{tankType}Tank_{i}";
                tank.name = $"{tankName}_Team{slots[i].teamId}";
                
                TankAssembly assembly = tank.GetComponent<TankAssembly>();
                if (assembly != null)   
                {
                    assembly.Assemble(slots[i]);
                }
                
                Debug.Log($"Tank {tank.name} ({tankType}, Team {slots[i].teamId}) spawned");
            }
        }
    }
    
    /// <summary>
    /// Get the Unity layer for a specific team - LEGACY METHOD
    /// Use SimpleTeamManager instead for new implementations
    /// </summary>
    private int GetLayerForTeam(int teamId)
    {
        switch (teamId)
        {
            case 0: return teamALayer; // Team A
            case 1: return teamBLayer; // Team B
            default: 
                Debug.LogWarning($"Unknown team ID: {teamId}. Using Team A layer.");
                return teamALayer;
        }
    }
    
    /// <summary>
    /// Recursively assign a layer to a GameObject and all its children - LEGACY METHOD
    /// Use SimpleTeamManager instead for new implementations
    /// </summary>
    private void AssignLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            AssignLayerRecursively(child.gameObject, layer);
        }
    }
}

