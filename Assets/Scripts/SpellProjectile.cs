using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 180f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private int damage = 20;
    [SerializeField] private LayerMask hitLayers;

    private Vector2 direction = Vector2.right;
    private float destroyTime;

    private void Awake()
    {
        if (hitLayers == 0)
        {
            hitLayers = LayerMask.GetMask("Enemy");
        }
    }

    private void OnEnable()
    {
        destroyTime = Time.time + lifetime;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        if (Time.time >= destroyTime)
        {
            Destroy(gameObject);
        }
    }

    public void Launch(Vector2 launchDirection, float projectileSpeed, float projectileLifetime, int projectileDamage)
    {
        direction = launchDirection.sqrMagnitude > 0f ? launchDirection.normalized : Vector2.right;
        speed = projectileSpeed;
        lifetime = projectileLifetime;
        damage = projectileDamage;
        destroyTime = Time.time + lifetime;
    }

    public void SetHitLayers(LayerMask layers)
    {
        hitLayers = layers;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayers) == 0)
        {
            return;
        }

        EnemyCombatStateController enemy = other.GetComponentInParent<EnemyCombatStateController>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
