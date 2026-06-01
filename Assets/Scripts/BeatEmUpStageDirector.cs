using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatEmUpStageDirector : MonoBehaviour
{
    public static BeatEmUpStageDirector Instance { get; private set; }

    private const int FirstCombatSectionIndex = 1;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera stageCamera;
    [SerializeField] private BeatEmUpCameraFollow cameraFollow;

    [Header("Stage")]
    [SerializeField] private bool useAutomaticSectionEncounters = true;
    [SerializeField, Min(1)] private int totalSections = 5;
    [SerializeField] private float fallbackSectionWidth = 354f;
    [SerializeField] private float horizontalScreenPadding = 2f;
    [SerializeField] private bool disablePreplacedEnemiesOnStart = true;
    [SerializeField] private bool usePlayerStartYAsGround = true;
    [SerializeField] private float combatGroundY = -34.8f;

    [Header("Enemies")]
    [SerializeField] private List<GameObject> automaticEnemyPrefabs = new List<GameObject>();
    [SerializeField, Min(1)] private int enemiesPerSection = 8;
    [SerializeField, Min(1)] private int maxSimultaneousSectionEnemies = 2;
    [SerializeField, Min(0.1f)] private float enemySpawnInterval = 4f;
    [SerializeField] private float enemySpawnScreenPadding = 3f;
    [SerializeField] private Vector3 enemyScale = new Vector3(25f, 25f, 25f);
    [SerializeField, Min(0f)] private float thirdSectionSpawnDelay = 3f;
    [SerializeField, Min(0f)] private float fourthSectionSpawnDelay = 2f;

    [Header("Boss")]
    [SerializeField] private bool startWithBossEncounter = false;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Vector3 bossScale = new Vector3(42f, 42f, 42f);
    [SerializeField] private bool overrideBossHealth = false;
    [SerializeField, Min(1)] private int bossHealth = 300;
    [SerializeField] private bool spawnBossMinions = true;
    [SerializeField, Min(0.1f)] private float bossMinionSpawnInterval = 8f;
    [SerializeField, Min(1)] private int bossMinionsPerWave = 3;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private readonly HashSet<int> triggeredSections = new HashSet<int>();
    private readonly List<float> sectionCenters = new List<float>();

    private Coroutine spawnRoutine;
    private Coroutine bossMinionRoutine;
    private GameObject finalBoss;
    private int pendingSpawnCount;
    private float sectionEncounterForwardDirection = 1f;
    private bool encounterLocked;
    private bool finalEncounter;

    public bool IsEncounterLocked => encounterLocked;
    public bool UsesAutomaticSectionEncounters => useAutomaticSectionEncounters;
    public float CombatGroundY => combatGroundY;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Multiple {nameof(BeatEmUpStageDirector)} objects found. Using {Instance.name}.");
            enabled = false;
            return;
        }

        Instance = this;
        ResolveReferences();

        if (usePlayerStartYAsGround && player != null)
        {
            combatGroundY = player.position.y;
        }

        BuildSectionCenters();
    }

    private void Start()
    {
        if (useAutomaticSectionEncounters && disablePreplacedEnemiesOnStart)
        {
            DisablePreplacedEnemies();
        }

        if (startWithBossEncounter)
        {
            StartBossEncounter();
        }
    }

    private void Update()
    {
        if (encounterLocked)
        {
            UpdateActiveEncounter();
            return;
        }

        if (useAutomaticSectionEncounters)
        {
            TryStartSectionEncounter();
        }
    }

    public float FilterHorizontalVelocity(float currentX, float desiredVelocity)
    {
        Vector2 cameraLimits = GetCameraHorizontalLimits();

        if (desiredVelocity < 0f && currentX <= cameraLimits.x)
        {
            return 0f;
        }

        if (desiredVelocity > 0f && currentX >= cameraLimits.y)
        {
            return 0f;
        }

        return desiredVelocity;
    }

    public Vector2 ClampPlayerPosition(Vector2 position)
    {
        Vector2 cameraLimits = GetCameraHorizontalLimits();
        position.x = Mathf.Clamp(position.x, cameraLimits.x, cameraLimits.y);

        if (useAutomaticSectionEncounters)
        {
            Vector2 stageLimits = GetStageHorizontalLimits();
            position.x = Mathf.Clamp(position.x, stageLimits.x, stageLimits.y);
        }

        return position;
    }

    public Vector2 ClampCombatantPosition(Vector2 position)
    {
        position = ClampPlayerPosition(position);
        position.y = combatGroundY;
        return position;
    }

    public float GetRecommendedJumpVelocity(float gravity, float clearancePadding)
    {
        float bossHeight = GetBossWorldHeight();
        if (bossHeight <= 0f)
        {
            return 0f;
        }

        float requiredHeight = bossHeight + Mathf.Max(0f, clearancePadding);
        return Mathf.Sqrt(2f * Mathf.Max(1f, gravity) * requiredHeight);
    }

    public void StartEncounter(
        IReadOnlyList<GameObject> existingEnemies,
        IReadOnlyList<GameObject> enemyPrefabs,
        int spawnCount,
        bool isFinalEncounter)
    {
        StartLegacyEncounter(existingEnemies, enemyPrefabs, spawnCount, isFinalEncounter);
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                playerObject = GameObject.Find("Player");
            }

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (stageCamera == null)
        {
            stageCamera = GetComponent<Camera>();
        }

        if (stageCamera == null)
        {
            stageCamera = Camera.main;
        }

        if (cameraFollow == null)
        {
            cameraFollow = GetComponent<BeatEmUpCameraFollow>();
        }
    }

    private void UpdateActiveEncounter()
    {
        RemoveDefeatedEnemies();

        if (finalEncounter && IsFinalBossDefeated() && bossMinionRoutine != null)
        {
            StopCoroutine(bossMinionRoutine);
            bossMinionRoutine = null;
        }

        if (activeEnemies.Count == 0 && pendingSpawnCount == 0)
        {
            if (!finalEncounter || IsFinalBossDefeated())
            {
                EndEncounter();
            }
        }
    }

    private void TryStartSectionEncounter()
    {
        int sectionIndex = GetCurrentSectionIndex();
        if (sectionIndex < FirstCombatSectionIndex || triggeredSections.Contains(sectionIndex))
        {
            return;
        }

        triggeredSections.Add(sectionIndex);

        if (sectionIndex >= totalSections - 1)
        {
            StartBossEncounter();
            return;
        }

        StartSectionEnemyEncounter(sectionIndex);
    }

    private void StartSectionEnemyEncounter(int sectionIndex)
    {
        encounterLocked = true;
        finalEncounter = false;
        finalBoss = null;
        activeEnemies.Clear();
        sectionEncounterForwardDirection = GetPlayerFacingDirection();
        pendingSpawnCount = enemiesPerSection;

        StopSpawnRoutines();

        if (cameraFollow != null)
        {
            cameraFollow.LockToCurrentPosition();
        }

        spawnRoutine = StartCoroutine(SpawnSectionEnemies(sectionIndex));
    }

    private IEnumerator SpawnSectionEnemies(int sectionIndex)
    {
        if (automaticEnemyPrefabs == null || automaticEnemyPrefabs.Count == 0 || pendingSpawnCount <= 0)
        {
            pendingSpawnCount = 0;
            yield break;
        }

        float initialDelay = GetSectionSpawnDelay(sectionIndex);
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        int spawnedCount = 0;
        while (pendingSpawnCount > 0)
        {
            RemoveDefeatedEnemies();

            if (activeEnemies.Count >= maxSimultaneousSectionEnemies)
            {
                yield return null;
                continue;
            }

            GameObject prefab = automaticEnemyPrefabs[spawnedCount % automaticEnemyPrefabs.Count];
            if (prefab != null)
            {
                float spawnSide = GetSectionSpawnSide(sectionIndex, spawnedCount);
                GameObject enemy = Instantiate(prefab, GetSpawnPosition(spawnedCount, spawnSide), Quaternion.identity);
                PrepareEnemy(enemy, spawnedCount, enemyScale, 0);
                activeEnemies.Add(enemy);
            }

            pendingSpawnCount--;
            spawnedCount++;

            if (pendingSpawnCount > 0)
            {
                yield return new WaitForSeconds(enemySpawnInterval);
            }
        }

        spawnRoutine = null;
    }

    private float GetSectionSpawnDelay(int sectionIndex)
    {
        return sectionIndex switch
        {
            2 => thirdSectionSpawnDelay,
            3 => fourthSectionSpawnDelay,
            _ => 0f
        };
    }

    private float GetSectionSpawnSide(int sectionIndex, int spawnIndex)
    {
        if (sectionIndex == 2 || sectionIndex == 3)
        {
            return spawnIndex % 2 == 0
                ? sectionEncounterForwardDirection
                : -sectionEncounterForwardDirection;
        }

        return sectionEncounterForwardDirection;
    }

    private void StartBossEncounter()
    {
        encounterLocked = true;
        finalEncounter = true;
        finalBoss = null;
        activeEnemies.Clear();
        pendingSpawnCount = 0;

        StopSpawnRoutines();

        GameObject prefab = bossPrefab != null ? bossPrefab : GetFallbackEnemyPrefab();
        if (prefab == null)
        {
            Debug.LogWarning($"{nameof(BeatEmUpStageDirector)} could not start boss encounter because no boss or enemy prefab is assigned.");
            EndEncounter();
            return;
        }

        finalBoss = Instantiate(prefab, GetBossSpawnPosition(), Quaternion.identity);
        PrepareEnemy(finalBoss, 0, bossScale, overrideBossHealth ? bossHealth : 0);
        activeEnemies.Add(finalBoss);

        if (cameraFollow != null)
        {
            cameraFollow.LockToCurrentPosition();
        }

        if (spawnBossMinions)
        {
            bossMinionRoutine = StartCoroutine(SpawnBossMinions());
        }
    }

    private IEnumerator SpawnBossMinions()
    {
        while (!IsFinalBossDefeated())
        {
            yield return new WaitForSeconds(bossMinionSpawnInterval);

            if (IsFinalBossDefeated())
            {
                break;
            }

            SpawnEnemyPack(automaticEnemyPrefabs, bossMinionsPerWave);
        }

        bossMinionRoutine = null;
    }

    private void StartLegacyEncounter(
        IReadOnlyList<GameObject> existingEnemies,
        IReadOnlyList<GameObject> enemyPrefabs,
        int spawnCount,
        bool isFinalEncounter)
    {
        encounterLocked = true;
        finalEncounter = isFinalEncounter;
        activeEnemies.Clear();
        sectionEncounterForwardDirection = GetPlayerFacingDirection();

        StopSpawnRoutines();
        AddExistingEnemies(existingEnemies);

        int enemiesToSpawn = Mathf.Max(0, spawnCount - activeEnemies.Count);
        pendingSpawnCount = enemiesToSpawn;
        if (enemiesToSpawn > 0)
        {
            spawnRoutine = StartCoroutine(SpawnLegacyEnemiesOverTime(enemyPrefabs, enemiesToSpawn, activeEnemies.Count > 0));
        }

        if (activeEnemies.Count == 0 && pendingSpawnCount == 0)
        {
            EndEncounter();
            return;
        }

        if (cameraFollow != null)
        {
            cameraFollow.LockToCurrentPosition();
        }
    }

    private IEnumerator SpawnLegacyEnemiesOverTime(IReadOnlyList<GameObject> enemyPrefabs, int spawnCount, bool delayBeforeFirstSpawn)
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0 || spawnCount <= 0)
        {
            pendingSpawnCount = 0;
            yield break;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            if (i > 0 || delayBeforeFirstSpawn)
            {
                yield return new WaitForSeconds(enemySpawnInterval);
            }

            GameObject prefab = enemyPrefabs[i % enemyPrefabs.Count];
            if (prefab == null)
            {
                pendingSpawnCount--;
                continue;
            }

            GameObject enemy = Instantiate(prefab, GetSpawnPosition(i, sectionEncounterForwardDirection), Quaternion.identity);
            PrepareEnemy(enemy, i, enemyScale, 0);
            activeEnemies.Add(enemy);
            pendingSpawnCount--;
        }

        spawnRoutine = null;
    }

    private void AddExistingEnemies(IReadOnlyList<GameObject> existingEnemies)
    {
        if (existingEnemies == null)
        {
            return;
        }

        for (int i = 0; i < existingEnemies.Count; i++)
        {
            if (existingEnemies[i] != null && existingEnemies[i].activeInHierarchy)
            {
                PrepareEnemy(existingEnemies[i], i, enemyScale, 0);
                activeEnemies.Add(existingEnemies[i]);
            }
        }
    }

    private void SpawnEnemyPack(IReadOnlyList<GameObject> enemyPrefabs, int count)
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0 || count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = enemyPrefabs[i % enemyPrefabs.Count];
            if (prefab == null)
            {
                continue;
            }

            float side = i % 2 == 0 ? 1f : -1f;
            GameObject enemy = Instantiate(prefab, GetSpawnPosition(i, side), Quaternion.identity);
            PrepareEnemy(enemy, i, enemyScale, 0);
            activeEnemies.Add(enemy);
        }
    }

    private Vector3 GetSpawnPosition(int enemyIndex, float sideDirection)
    {
        Vector2 cameraLimits = GetCameraHorizontalLimits(0f);
        float sidePadding = Mathf.Max(0f, enemySpawnScreenPadding);
        float spawnX = sideDirection >= 0f
            ? cameraLimits.y - sidePadding
            : cameraLimits.x + sidePadding;

        spawnX = Mathf.Clamp(spawnX, cameraLimits.x, cameraLimits.y);
        return new Vector3(spawnX, combatGroundY, 0f);
    }

    private Vector3 GetBossSpawnPosition()
    {
        Vector2 cameraLimits = GetCameraHorizontalLimits(0f);
        float sidePadding = Mathf.Max(0f, enemySpawnScreenPadding);
        float spawnX = GetPlayerFacingDirection() >= 0f
            ? cameraLimits.y - sidePadding
            : cameraLimits.x + sidePadding;

        return new Vector3(spawnX, combatGroundY, 0f);
    }

    private void PrepareEnemy(GameObject enemy, int enemyIndex, Vector3 scale, int maxHealth)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.transform.localScale = new Vector3(
            Mathf.Abs(scale.x),
            Mathf.Abs(scale.y),
            Mathf.Abs(scale.z));

        Vector3 position = enemy.transform.position;
        position.y = combatGroundY;
        enemy.transform.position = position;

        EnemyCombatStateController combat = enemy.GetComponent<EnemyCombatStateController>();
        if (combat == null)
        {
            combat = enemy.AddComponent<EnemyCombatStateController>();
        }

        combat.SetGroundY(combatGroundY);
        if (maxHealth > 0)
        {
            combat.SetMaxHealth(maxHealth);
        }
    }

    private void EndEncounter()
    {
        bool completedFinalEncounter = finalEncounter;

        encounterLocked = false;
        finalEncounter = false;
        finalBoss = null;
        pendingSpawnCount = 0;

        StopSpawnRoutines();

        if (cameraFollow != null)
        {
            cameraFollow.Unlock();
        }

        if (completedFinalEncounter)
        {
            GameWinController.ShowWin();
        }
    }

    private void StopSpawnRoutines()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (bossMinionRoutine != null)
        {
            StopCoroutine(bossMinionRoutine);
            bossMinionRoutine = null;
        }
    }

    private void BuildSectionCenters()
    {
        sectionCenters.Clear();

        List<float> backgroundCenters = new List<float>();
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].gameObject.name.StartsWith("Background"))
            {
                backgroundCenters.Add(renderers[i].bounds.center.x);
            }
        }

        backgroundCenters.Sort();

        if (backgroundCenters.Count > 0)
        {
            float startCenter = GetClosestBackgroundCenterToPlayer(backgroundCenters);
            int startIndex = Mathf.Max(0, backgroundCenters.IndexOf(startCenter));

            for (int i = startIndex; i < backgroundCenters.Count && sectionCenters.Count < totalSections; i++)
            {
                sectionCenters.Add(backgroundCenters[i]);
            }
        }

        while (sectionCenters.Count < totalSections)
        {
            float nextCenter = sectionCenters.Count == 0
                ? (player != null ? player.position.x : 0f)
                : sectionCenters[sectionCenters.Count - 1] + GetSectionWidth();
            sectionCenters.Add(nextCenter);
        }
    }

    private float GetClosestBackgroundCenterToPlayer(List<float> backgroundCenters)
    {
        float playerX = player != null ? player.position.x : 0f;
        float closest = backgroundCenters[0];
        float closestDistance = Mathf.Abs(playerX - closest);

        for (int i = 1; i < backgroundCenters.Count; i++)
        {
            float distance = Mathf.Abs(playerX - backgroundCenters[i]);
            if (distance < closestDistance)
            {
                closest = backgroundCenters[i];
                closestDistance = distance;
            }
        }

        return closest;
    }

    private int GetCurrentSectionIndex()
    {
        if (player == null)
        {
            return 0;
        }

        float startX = GetStageStartX();
        float distanceIntoStage = player.position.x - startX;
        return Mathf.Clamp(Mathf.FloorToInt(distanceIntoStage / GetSectionWidth()), 0, totalSections - 1);
    }

    private float GetSectionWidth()
    {
        if (sectionCenters.Count >= 2)
        {
            float width = Mathf.Abs(sectionCenters[1] - sectionCenters[0]);
            if (width > 0f)
            {
                return width;
            }
        }

        return Mathf.Max(1f, fallbackSectionWidth);
    }

    private float GetStageStartX()
    {
        float firstCenter = sectionCenters.Count > 0
            ? sectionCenters[0]
            : (player != null ? player.position.x : 0f);

        return firstCenter - GetSectionWidth() * 0.5f;
    }

    private Vector2 GetStageHorizontalLimits()
    {
        float width = GetSectionWidth();
        float startX = GetStageStartX();
        return new Vector2(
            startX + horizontalScreenPadding,
            startX + width * totalSections - horizontalScreenPadding);
    }

    private Vector2 GetCameraHorizontalLimits(float paddingOverride = -1f)
    {
        if (stageCamera == null)
        {
            float playerX = player != null ? player.position.x : transform.position.x;
            return new Vector2(playerX - 100f, playerX + 100f);
        }

        float padding = paddingOverride >= 0f ? paddingOverride : horizontalScreenPadding;
        float halfWidth = stageCamera.orthographicSize * stageCamera.aspect;
        float minX = stageCamera.transform.position.x - halfWidth + padding;
        float maxX = stageCamera.transform.position.x + halfWidth - padding;
        return new Vector2(minX, maxX);
    }

    private float GetPlayerFacingDirection()
    {
        if (player == null)
        {
            return 1f;
        }

        return player.localScale.x >= 0f ? 1f : -1f;
    }

    private bool IsFinalBossDefeated()
    {
        if (finalBoss == null || !finalBoss.activeInHierarchy)
        {
            return true;
        }

        EnemyCombatStateController combat = finalBoss.GetComponent<EnemyCombatStateController>();
        return combat != null && combat.IsDead;
    }

    private GameObject GetFallbackEnemyPrefab()
    {
        if (automaticEnemyPrefabs == null || automaticEnemyPrefabs.Count == 0)
        {
            return null;
        }

        return automaticEnemyPrefabs[0];
    }

    private float GetBossWorldHeight()
    {
        SpriteRenderer renderer = bossPrefab != null ? bossPrefab.GetComponent<SpriteRenderer>() : null;
        if (renderer != null && renderer.sprite != null)
        {
            return renderer.sprite.bounds.size.y * Mathf.Abs(bossScale.y);
        }

        return Mathf.Abs(bossScale.y) * 1.5f;
    }

    private void DisablePreplacedEnemies()
    {
        EnemyCombatStateController[] enemies = FindObjectsByType<EnemyCombatStateController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i] != null)
            {
                enemies[i].gameObject.SetActive(false);
            }
        }

        GameObject[] sceneObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < sceneObjects.Length; i++)
        {
            if (sceneObjects[i] != null && sceneObjects[i].name.Trim().StartsWith("Boss"))
            {
                sceneObjects[i].SetActive(false);
            }
        }
    }

    private void RemoveDefeatedEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = activeEnemies[i];
            EnemyCombatStateController combat = enemy != null ? enemy.GetComponent<EnemyCombatStateController>() : null;
            if (enemy == null || !enemy.activeInHierarchy || (combat != null && combat.IsDead))
            {
                activeEnemies.RemoveAt(i);
            }
        }
    }
}
