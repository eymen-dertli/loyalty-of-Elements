using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BeatEmUpEncounterTrigger : MonoBehaviour
{
    [SerializeField] private BeatEmUpStageDirector stageDirector;
    [SerializeField] private bool finalEncounter = false;
    [SerializeField] private int spawnCount = 4;
    [SerializeField] private List<GameObject> existingEnemies = new List<GameObject>();
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();

    private bool hasTriggered;

    private void Awake()
    {
        BoxCollider2D triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;

        if (stageDirector == null)
        {
            stageDirector = BeatEmUpStageDirector.Instance;
        }
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
