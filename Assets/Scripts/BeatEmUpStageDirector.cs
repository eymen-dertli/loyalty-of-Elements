using System.Collections.Generic;
using UnityEngine;

public class BeatEmUpStageDirector : MonoBehaviour
{
    public static BeatEmUpStageDirector Instance { get; private set; }

    [SerializeField] private Transform player;
    [SerializeField] private Camera stageCamera;
    [SerializeField] private BeatEmUpCameraFollow cameraFollow;
    [SerializeField] private float horizontalScreenPadding = 2f;
    [SerializeField] private float enemySpawnScreenPadding = 3f;

    private readonly List<GameObject> activeEnemies = new List<GameObject>();
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

        if (activeEnemies.Count == 0)
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

    public void StartEncounter(
        IReadOnlyList<GameObject> existingEnemies,
        IReadOnlyList<GameObject> enemyPrefabs,
        int spawnCount,
        bool isFinalEncounter)
    {
        encounterLocked = true;
        finalEncounter = isFinalEncounter;
        activeEnemies.Clear();

        AddExistingEnemies(existingEnemies);
        SpawnEnemies(enemyPrefabs, spawnCount);

        if (activeEnemies.Count == 0)
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
                activeEnemies.Add(existingEnemies[i]);
            }
        }
    }

    private void SpawnEnemies(IReadOnlyList<GameObject> enemyPrefabs, int spawnCount)
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0 || spawnCount <= 0)
        {
            return;
        }

        Vector2 cameraLimits = GetCameraHorizontalLimits(0f);

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = enemyPrefabs[i % enemyPrefabs.Count];
            if (prefab == null)
            {
                continue;
            }

            float spawnX = i % 2 == 0
                ? cameraLimits.x - enemySpawnScreenPadding
                : cameraLimits.y + enemySpawnScreenPadding;

            Vector3 spawnPosition = player != null
                ? new Vector3(spawnX, player.position.y, 0f)
                : new Vector3(spawnX, transform.position.y, 0f);

            GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
            activeEnemies.Add(enemy);
        }
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
