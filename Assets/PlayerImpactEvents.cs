using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerImpactEvents : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private LayerMask groundLayer;

    [Header("Landing Shake")]
    [SerializeField] private float minLandingSpeed = 8f;
    [SerializeField] private float landingShakeDuration = 0.10f;
    [SerializeField] private float landingShakeStrength = 0.2f;

    [Header("Dash Shake (Velocity Spike)")]
    [SerializeField] private float dashSpeedThreshold = 12f;
    [SerializeField] private float dashShakeDuration = 0.08f;
    [SerializeField] private float dashShakeStrength = 0.18f;

    Vector2 _prevVelocity;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Vector2 v = rb.linearVelocity;   // wenn du rb.velocity benutzt, hier anpassen

        // einfacher Dash-Start-Detektor:
        // vorher langsam, jetzt plötzlich sehr schnell -> kleiner Screenshake
        if (_prevVelocity.magnitude < dashSpeedThreshold &&
            v.magnitude >= dashSpeedThreshold)
        {
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.Shake(dashShakeDuration, dashShakeStrength);
            }
        }

        _prevVelocity = v;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // checkt, ob getroffener Collider im Ground-Layer ist
        int otherLayerMask = 1 << collision.gameObject.layer;
        bool isGroundHit = (groundLayer.value & otherLayerMask) != 0;

        if (!isGroundHit) return;

        // relativeVelocity.y sagt dir, wie hart du "reingekracht" bist
        float impactSpeed = Mathf.Abs(collision.relativeVelocity.y);

        if (impactSpeed >= minLandingSpeed && CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(landingShakeDuration, landingShakeStrength);
        }
    }
}
