using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Default Shake")]
    [SerializeField] private float defaultDuration = 0.12f;
    [SerializeField] private float defaultStrength = 0.25f;

    Transform _cam;
    Vector3 _initialLocalPos;
    Coroutine _currentShake;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _cam = transform;
        _initialLocalPos = _cam.localPosition;
    }

    public void Shake(float duration, float strength)
    {
        if (_currentShake != null)
            StopCoroutine(_currentShake);

        _currentShake = StartCoroutine(ShakeRoutine(duration, strength));
    }

    public void ShakeDefault()
    {
        Shake(defaultDuration, defaultStrength);
    }

    IEnumerator ShakeRoutine(float duration, float strength)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // kleine zufällige Offsets
            Vector2 offset = Random.insideUnitCircle * strength;
            _cam.localPosition = _initialLocalPos + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        _cam.localPosition = _initialLocalPos;
        _currentShake = null;
    }
}