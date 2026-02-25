using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ScissorsBehavior : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Header("Prefab")]
    [SerializeField] private GameObject paper;

    private Rigidbody2D _rb;

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
        float randomAngle = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0f, 0f, randomAngle);
    }

    private void FixedUpdate()
    {
        Vector2 moveDir = -transform.up;
        _rb.linearVelocity = moveDir * speed;
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

        transform.Rotate(0f, 0f, 180f);
    }
}