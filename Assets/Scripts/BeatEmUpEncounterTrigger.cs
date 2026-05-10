using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BeatEmUpEncounterTrigger : MonoBehaviour
{
    private const int EnemiesPerRound = 4;

    [SerializeField] private BeatEmUpStageDirector stageDirector;
    [SerializeField] private bool finalEncounter = false;
    [SerializeField, Min(1)] private int spawnCount = EnemiesPerRound;
    [SerializeField] private List<GameObject> existingEnemies = new List<GameObject>();
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();

    private bool hasTriggered;

    private void Awake()
    {
        spawnCount = EnemiesPerRound;

        BoxCollider2D triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;

        if (stageDirector == null)
        {
            stageDirector = BeatEmUpStageDirector.Instance;
        }
    }

    private void OnValidate()
    {
        spawnCount = EnemiesPerRound;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered || !IsPlayer(other))
        {
            return;
        }

        if (stageDirector == null)
        {
            stageDirector = BeatEmUpStageDirector.Instance;
        }

        if (stageDirector == null)
        {
            Debug.LogWarning($"{nameof(BeatEmUpEncounterTrigger)} on {name} could not find a stage director.");
            return;
        }

        hasTriggered = true;
        stageDirector.StartEncounter(existingEnemies, enemyPrefabs, spawnCount, finalEncounter);
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            return true;
        }

        return other.name == "Player" || other.GetComponent<PlayerMovement>() != null;
    }
}
