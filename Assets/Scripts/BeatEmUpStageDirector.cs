using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class BeatEmUpStageDirector : MonoBehaviour
{
    public static BeatEmUpStageDirector Instance { get; private set; }

    [SerializeField] private Transform player;
    [SerializeField] private Camera stageCamera;
    [SerializeField] private BeatEmUpCameraFollow cameraFollow;
    [SerializeField] private float horizontalScreenPadding = 2f;
    [SerializeField] private float enemySpawnScreenPadding = 3f;
    [SerializeField] private float enemySpawnInterval = 10f;
    [SerializeField] private Vector3 enemyScale = new Vector3(25f, 25f, 25f);

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private Coroutine spawnRoutine;
    private int pendingSpawnCount;
    private bool encounterLocked;
    private bool finalEncounter;

    public bool IsEncounterLocked => encounterLocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Multiple {nameof(BeatEmUpStageDirector)} objects found. Using {Instance.name}.");
            enabled = false;
            return;
        }

        Instance = this;

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

    private void Start()
    {
    }

    private void Update()
    {
        if (!encounterLocked)
        {
            return;
        }

        RemoveDefeatedEnemies();

        if (activeEnemies.Count == 0 && pendingSpawnCount == 0)
        {
            if (!finalEncounter)
            {
                EndEncounter();
            }
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

        return position;
    }

    public Vector2 ClampCombatantPosition(Vector2 position)
    {
        return ClampPlayerPosition(position);
    }

    public void StartEncounter(
        IReadOnlyList<GameObject> existingEnemies,
        IReadOnlyList<GameObject> enemyPrefabs,
        int spawnCount,
        bool isFinalEncounter)
    {
        encounterLocked = true;
        finalEncounter = isFinalEncounter;
        activeEnemies.Clear();

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        AddExistingEnemies(existingEnemies);
        ArrangeEnemiesAroundPlayer();

        int enemiesToSpawn = Mathf.Max(0, spawnCount - activeEnemies.Count);
        pendingSpawnCount = enemiesToSpawn;
        if (enemiesToSpawn > 0)
        {
            spawnRoutine = StartCoroutine(SpawnEnemiesOverTime(enemyPrefabs, enemiesToSpawn, activeEnemies.Count > 0));
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

    private void EndEncounter()
    {
        encounterLocked = false;
        pendingSpawnCount = 0;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (cameraFollow != null)
        {
            cameraFollow.Unlock();
        }
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
                PrepareEnemy(existingEnemies[i], i);
                activeEnemies.Add(existingEnemies[i]);
            }
        }
    }

    private IEnumerator SpawnEnemiesOverTime(IReadOnlyList<GameObject> enemyPrefabs, int spawnCount, bool delayBeforeFirstSpawn)
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

            int enemyIndex = activeEnemies.Count + i;
            Vector3 spawnPosition = GetSpawnPosition(enemyIndex);
            GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
            PrepareEnemy(enemy, enemyIndex);
            activeEnemies.Add(enemy);
            pendingSpawnCount--;
        }

        spawnRoutine = null;
    }

    private void ArrangeEnemiesAroundPlayer()
    {
        if (activeEnemies.Count == 0)
        {
            return;
        }

        Vector2 cameraLimits = GetCameraHorizontalLimits();
        Vector3 center = player != null ? player.position : transform.position;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            GameObject enemy = activeEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            float side = i % 2 == 0 ? -1f : 1f;
            float lane = i / 2;
            float x = Mathf.Clamp(center.x + side * (35f + lane * 18f), cameraLimits.x, cameraLimits.y);
            float y = center.y + GetSpawnVerticalOffset(i);
            enemy.transform.position = new Vector3(x, y, enemy.transform.position.z);
            PrepareEnemy(enemy, i);
        }
    }

    private Vector3 GetSpawnPosition(int enemyIndex)
    {
        Vector2 cameraLimits = GetCameraHorizontalLimits(0f);
        float sidePadding = Mathf.Max(0f, enemySpawnScreenPadding);
        float spawnX = enemyIndex % 2 == 0
            ? cameraLimits.x + sidePadding
            : cameraLimits.y - sidePadding;

        spawnX = Mathf.Clamp(spawnX, cameraLimits.x, cameraLimits.y);

        Vector3 center = player != null ? player.position : transform.position;
        return new Vector3(spawnX, center.y + GetSpawnVerticalOffset(enemyIndex), 0f);
    }

    private void PrepareEnemy(GameObject enemy, int enemyIndex)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.transform.localScale = new Vector3(
            Mathf.Abs(enemyScale.x),
            Mathf.Abs(enemyScale.y),
            Mathf.Abs(enemyScale.z));
    }

    private float GetSpawnVerticalOffset(int enemyIndex)
    {
        return (enemyIndex % 4) switch
        {
            1 => 8f,
            2 => -8f,
            3 => 16f,
            _ => 0f
        };
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

    private void RemoveDefeatedEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = activeEnemies[i];
            if (enemy == null || !enemy.activeInHierarchy)
            {
                activeEnemies.RemoveAt(i);
            }
        }
    }
}
