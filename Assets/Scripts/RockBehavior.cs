using UnityEngine;

/// <summary>
/// RockBehavior
/// - Every JumpInterval seconds, applies an impulse in a random direction within ±45° from up.
/// - On collision with an object tagged "Paper", spawns a Scissors prefab at this position,
///   then destroys itself.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class RockBehavior : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float jumpInterval = 1.0f;

    [Header("Prefab")]
    [SerializeField] private GameObject scissors;

    private Rigidbody2D _rb;
    private float _timer;

    private void OnEnable()
    {
        InstanceNumberManager.RegisterInstance();
    }

    private void OnDestroy()
    {
        InstanceNumberManager.UnregisterInstance();
    }
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _timer = 0f;
    }

    private void Update()
    {
        if (jumpInterval <= 0f)
            return;

        _timer += Time.deltaTime;
        if (_timer >= jumpInterval)
        {
            _timer -= jumpInterval;
            Jump();
        }
    }
    
    private void Jump()
    {
        float angleDeg = Random.Range(-45f, 45f);
        Vector2 dir = Quaternion.Euler(0f, 0f, angleDeg) * Vector2.up;
        
        _rb.AddForce(dir.normalized * jumpForce, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Paper"))
            return;
        
        if (scissors != null && !InstanceNumberManager.IsAtLimit())
            Instantiate(scissors, transform.position, Quaternion.identity);

        Destroy(transform.parent.gameObject);
    }
}