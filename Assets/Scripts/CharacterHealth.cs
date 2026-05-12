using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private UnityEvent<int, int> healthChanged;
    [SerializeField] private UnityEvent died;
    [SerializeField] private string deathAnimationName = "Death";
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button tryAgainButton;

    private int currentHealth;
    private bool isDead;
    private Animator animator;

    public event UnityAction<int, int> HealthChanged;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {   
        currentHealth = maxHealth;
        isDead = false;
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (tryAgainButton != null)
        {
            tryAgainButton.onClick.AddListener(OnTryAgainClicked);
        }

        HealthChanged?.Invoke(currentHealth, maxHealth);
        healthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (isDead || currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, damage));
        Debug.Log($"Hasar alındı! Can: {currentHealth}/{maxHealth}");
        
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        HealthChanged?.Invoke(currentHealth, maxHealth);
        healthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead || currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(1, amount));
        
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        HealthChanged?.Invoke(currentHealth, maxHealth);
        healthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        Debug.Log($"{gameObject.name} ÖLDÜ!");
        
        died?.Invoke();
        
        // Ölüm animasyonunu çal
        if (animator != null)
        {
            animator.Play(deathAnimationName);
            Debug.Log($"Ölüm animasyonu çalıştırılıyor: {deathAnimationName}");
        }

        // Hareketi durdur
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Collider'ı devre dışı bırak
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // Karakteri kontrol edilemez yap
        CharacterKeyboardMovement movement = GetComponent<CharacterKeyboardMovement>();
        if (movement != null)
        {
            movement.enabled = false;
        }

        CharacterMouseAttack attack = GetComponent<CharacterMouseAttack>();
        if (attack != null)
        {
            attack.enabled = false;
        }

        // Game Over panelini göster
        ShowGameOver();
    }

    private void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("Game Over paneli gösterildi!");
        }
    }

    private void OnTryAgainClicked()
    {
        Debug.Log("Try Again butonuna tıklandı!");
        
        // Sahneyi tekrar yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
