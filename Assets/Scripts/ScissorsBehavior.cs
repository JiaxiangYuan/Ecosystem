using UnityEngine;

/// <summary>
/// ScissorsBehavior
/// - On spawn, picks a random direction and moves at a constant speed.
/// - On collision with an object tagged "Rock", spawns a Rock prefab at this position,
///   then destroys itself.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class ScissorsBehavior : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Header("Prefab")]
    [SerializeField] private GameObject paper;

    private Rigidbody2D _rb;
    private Vector2 _moveDir;

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
        _moveDir = Random.insideUnitCircle;
        if (_moveDir.sqrMagnitude < 0.0001f)
            _moveDir = Vector2.right;

        _moveDir = _moveDir.normalized;
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = _moveDir * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Rock"))
        {
            if (paper != null && !InstanceNumberManager.IsAtLimit())
                Instantiate(paper, transform.position, Quaternion.identity);

            Destroy(transform.parent.gameObject);
            return;
        }
        _moveDir = -_moveDir;
    }
}