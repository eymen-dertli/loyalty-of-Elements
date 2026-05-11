using UnityEngine;
using UnityEngine.InputSystem;

#pragma warning disable 0649
public class CharacterSpellCaster : MonoBehaviour
{
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Transform castPoint;
    [SerializeField] private Vector2 castOffset = new Vector2(28f, 4f);
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private string projectileSortingLayer = "Player";
    [SerializeField] private int projectileSortingOrder = 50;
    [SerializeField] private SpellData spell1 = new SpellData("Spell 1", 180f, 2f, 20);
    [SerializeField] private SpellData spell2 = new SpellData("Spell 2", 150f, 2.2f, 30);
    [SerializeField] private SpellData spell3 = new SpellData("Spell 3", 130f, 2.4f, 40);
    [SerializeField] private SpellData ultimate = new SpellData("Ulti", 110f, 3f, 80);

    private float nextCastTime;

    public bool IsCasting => Time.time < nextCastTime;

    private void Awake()
    {
        if (characterAnimator == null)
        {
            characterAnimator = GetComponent<Animator>();
        }

        if (enemyLayers == 0)
        {
            enemyLayers = LayerMask.GetMask("Enemy");
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || IsCasting)
        {
            return;
        }

        if (keyboard.qKey.wasPressedThisFrame)
        {
            Cast(spell1);
        }
        else if (keyboard.wKey.wasPressedThisFrame)
        {
            Cast(spell2);
        }
        else if (keyboard.eKey.wasPressedThisFrame)
        {
            Cast(spell3);
        }
        else if (keyboard.rKey.wasPressedThisFrame)
        {
            Cast(ultimate);
        }
    }

    private void Cast(SpellData spell)
    {
        nextCastTime = Time.time + Mathf.Max(0f, spell.cooldown);
        PlayCharacterCastAnimation(spell);

        Vector2 direction = GetFacingDirection();
        GameObject projectile = spell.projectilePrefab != null
            ? Instantiate(spell.projectilePrefab, GetCastPosition(direction), Quaternion.identity)
            : CreateFallbackProjectile(spell, direction);

        Vector3 baseScale = projectile.transform.localScale;
        projectile.transform.localScale = new Vector3(
            Mathf.Abs(baseScale.x) * spell.scale * Mathf.Sign(direction.x),
            Mathf.Abs(baseScale.y) * spell.scale,
            Mathf.Abs(baseScale.z));
        ApplyProjectileSorting(projectile);
        PlayProjectileAnimation(projectile, spell);

        SpellProjectile spellProjectile = projectile.GetComponent<SpellProjectile>();
        if (spellProjectile == null)
        {
            spellProjectile = projectile.AddComponent<SpellProjectile>();
        }

        spellProjectile.SetHitLayers(enemyLayers);
        spellProjectile.Launch(direction, spell.speed, spell.lifetime, spell.damage);
    }

    private void PlayCharacterCastAnimation(SpellData spell)
    {
        if (characterAnimator == null || string.IsNullOrWhiteSpace(spell.characterCastStateName))
        {
            return;
        }

        characterAnimator.Play(spell.characterCastStateName, 0, 0f);
    }

    private void PlayProjectileAnimation(GameObject projectile, SpellData spell)
    {
        if (spell.projectileAnimation == null)
        {
            return;
        }

        SpellProjectileVisualPlayer visualPlayer = projectile.GetComponent<SpellProjectileVisualPlayer>();
        if (visualPlayer == null)
        {
            visualPlayer = projectile.AddComponent<SpellProjectileVisualPlayer>();
        }

        visualPlayer.Play(spell.projectileAnimation);
    }

    private Vector3 GetCastPosition(Vector2 direction)
    {
        if (castPoint != null)
        {
            return castPoint.position;
        }

        Vector2 offset = new Vector2(castOffset.x * Mathf.Sign(direction.x), castOffset.y);
        return (Vector2)transform.position + offset;
    }

    private Vector2 GetFacingDirection()
    {
        float facingDirection = Mathf.Sign(transform.localScale.x);
        return Mathf.Approximately(facingDirection, 0f) ? Vector2.right : Vector2.right * facingDirection;
    }

    private GameObject CreateFallbackProjectile(SpellData spell, Vector2 direction)
    {
        GameObject projectile = new GameObject(spell.name);
        projectile.transform.position = GetCastPosition(direction);

        SpriteRenderer spriteRenderer = projectile.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = spell.sprite;
        spriteRenderer.sortingLayerName = projectileSortingLayer;
        spriteRenderer.sortingOrder = projectileSortingOrder;

        CircleCollider2D collider = projectile.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = spell.hitRadius;

        Rigidbody2D rb = projectile.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        projectile.layer = gameObject.layer;
        return projectile;
    }

    private void ApplyProjectileSorting(GameObject projectile)
    {
        SpriteRenderer[] renderers = projectile.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = projectileSortingLayer;
            renderers[i].sortingOrder = projectileSortingOrder;
        }
    }

    [System.Serializable]
    private class SpellData
    {
        public string name;
        public GameObject projectilePrefab;
        public Sprite sprite;
        public AnimationClip projectileAnimation;
        public string characterCastStateName = "PlayerAttackWindBall";
        public float speed;
        public float lifetime;
        public float cooldown = 0.35f;
        public int damage;
        public float scale = 12f;
        public float hitRadius = 0.35f;

        public SpellData(string name, float speed, float lifetime, int damage)
        {
            this.name = name;
            this.speed = speed;
            this.lifetime = lifetime;
            this.damage = damage;
        }
    }
}
#pragma warning restore 0649
