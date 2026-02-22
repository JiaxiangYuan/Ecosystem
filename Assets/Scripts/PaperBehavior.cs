using System.Collections;
using UnityEngine;

/// <summary>
/// PaperBehavior
/// - Every ScaleInterval seconds, scales up to MaxScale over ScalingTime,
///   then scales back to the original scale over another ScalingTime.
/// - On collision with an object tagged "Scissors", spawns a Rock prefab at this position,
///   then destroys itself.
/// </summary>
public class PaperBehavior : MonoBehaviour
{
    [Header("Scaling")]
    [SerializeField] private float maxScale = 1.5f;
    [SerializeField] private float scaleInterval = 2f;
    [SerializeField] private float scalingTime = 0.25f;

    [Header("Prefab")]
    [SerializeField] private GameObject rock;

    private Vector3 _baseScale;
    private bool _isScaling;
    private Coroutine _loopCo;

    private void OnEnable()
    {
        InstanceNumberManager.RegisterInstance();
        _loopCo = StartCoroutine(ScaleLoop());
    }

    private void OnDestroy()
    {
        InstanceNumberManager.UnregisterInstance();
        if (_loopCo != null)
        {
            StopCoroutine(_loopCo);
            _loopCo = null;
        }

        _isScaling = false;
        transform.localScale = _baseScale;
    }
    
    private void Awake()
    {
        _baseScale = transform.localScale;
    }
    
    private void OnDisable()
    {
        if (_loopCo != null)
        {
            StopCoroutine(_loopCo);
            _loopCo = null;
        }

        _isScaling = false;
        transform.localScale = _baseScale;
    }
    
    private IEnumerator ScaleLoop()
    {
        float safeInterval = Mathf.Max(0f, scaleInterval);

        while (true)
        {
            if (safeInterval > 0f)
                yield return new WaitForSeconds(safeInterval);

            if (!_isScaling)
                yield return ScalePulse();
            else
                yield return null;
        }
    }
    
    private IEnumerator ScalePulse()
    {
        _isScaling = true;

        float tUp = Mathf.Max(0.0001f, scalingTime);
        float tDown = tUp;

        Vector3 targetScale = _baseScale * Mathf.Max(0f, maxScale);
        
        float t = 0f;
        while (t < tUp)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / tUp);
            transform.localScale = Vector3.Lerp(_baseScale, targetScale, a);
            yield return null;
        }
        transform.localScale = targetScale;
        
        t = 0f;
        while (t < tDown)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / tDown);
            transform.localScale = Vector3.Lerp(targetScale, _baseScale, a);
            yield return null;
        }
        transform.localScale = _baseScale;

        _isScaling = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("Scissors"))
            return;
        
        if (rock != null && !InstanceNumberManager.IsAtLimit())
            Instantiate(rock, transform.position, Quaternion.identity);

        Destroy(transform.parent.gameObject);
    }
}