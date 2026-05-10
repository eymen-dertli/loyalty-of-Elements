using UnityEngine;
using UnityEngine.Events;

public class CharacterHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private UnityEvent<int, int> healthChanged;
    [SerializeField] private UnityEvent died;

    private int currentHealth;

    public event UnityAction<int, int> HealthChanged;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
        healthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, damage));
        HealthChanged?.Invoke(currentHealth, maxHealth);
        healthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0)
        {
            died?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(1, amount));
        HealthChanged?.Invoke(currentHealth, maxHealth);
        healthChanged?.Invoke(currentHealth, maxHealth);
    }
}
