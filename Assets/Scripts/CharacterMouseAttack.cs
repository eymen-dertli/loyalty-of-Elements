using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterMouseAttack : MonoBehaviour
{
    [SerializeField] private float fallbackAttackDuration = 0.3f;
    [SerializeField] private int punchDamage = 10;
    [SerializeField] private int kickDamage = 15;
    [SerializeField] private float attackRange = 32f;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(34f, 38f);
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private string idleStateName = "PlayerIdle";
    [SerializeField] private string punchStateName = "PlayerAttackPunch";
    [SerializeField] private string kickStateName = "PlayerAttackKick";

    private Animator animator;
    private CharacterSpellCaster spellCaster;
    private readonly Collider2D[] hitResults = new Collider2D[8];
    private float attackEndTime;

    public bool IsAttacking => attackEndTime > 0f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spellCaster = GetComponent<CharacterSpellCaster>();

        if (enemyLayers == 0)
        {
            enemyLayers = LayerMask.GetMask("Enemy");
        }
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        if (attackEndTime > 0f && Time.time >= attackEndTime)
        {
            attackEndTime = 0f;
            animator.Play(idleStateName, 0, 0f);
        }

        Mouse mouse = Mouse.current;

        if (mouse == null || (spellCaster != null && spellCaster.IsCasting))
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            PlayAttack(punchStateName, punchDamage);
        }
        else if (mouse.rightButton.wasPressedThisFrame)
        {
            PlayAttack(kickStateName, kickDamage);
        }
    }

    private void PlayAttack(string stateName, int damage)
    {
        animator.Play(stateName, 0, 0f);
        attackEndTime = Time.time + GetAnimationLength(stateName);
        HitEnemies(damage);
    }

    private void HitEnemies(int damage)
    {
        float facingDirection = Mathf.Sign(transform.localScale.x);
        if (Mathf.Approximately(facingDirection, 0f))
        {
            facingDirection = 1f;
        }

        Vector2 attackCenter = (Vector2)transform.position + Vector2.right * facingDirection * attackRange;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(enemyLayers);
        filter.useTriggers = false;

        int hitCount = Physics2D.OverlapBox(attackCenter, attackBoxSize, 0f, filter, hitResults);
        for (int i = 0; i < hitCount; i++)
        {
            if (hitResults[i] == null || hitResults[i].gameObject == gameObject)
            {
                continue;
            }

            EnemyCombatStateController enemy = hitResults[i].GetComponentInParent<EnemyCombatStateController>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    private float GetAnimationLength(string stateName)
    {
        if (animator.runtimeAnimatorController == null)
        {
            return fallbackAttackDuration;
        }

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == stateName)
            {
                return clip.length;
            }
        }

        return fallbackAttackDuration;
    }
}
