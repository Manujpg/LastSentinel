using System.Collections;
using UnityEngine;

public class CelesteController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 14f;

    [Header("Coyote / Jump Buffer")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Gravity")]
    [SerializeField] private float baseGravityScale = 3f;
    [SerializeField] private float fallGravityMultiplier = 2.5f;
    [SerializeField] private float jumpCutGravityMultiplier = 3f;
    [SerializeField] private float maxFallSpeed = -25f;

    [Header("Dash")]
    [SerializeField] private float dashingPower = 24f;
    [SerializeField] private float dashingTime = 0.3f;
    [SerializeField] private TrailRenderer tr;

    [Header("Ground / Wall Checks")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private Transform wallCheckLeftHigh;
    [SerializeField] private Transform wallCheckLeftLow;
    [SerializeField] private Transform wallCheckRightHigh;
    [SerializeField] private Transform wallCheckRightLow;

    [SerializeField] private float checkRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Wall Behaviour")]
    [SerializeField] private float wallSlideSpeed = -3f;   // NEGATIV
    [SerializeField] private float wallJumpForceX = 12f;
    [SerializeField] private float wallJumpForceY = 14f;
    [SerializeField] private float wallJumpLockTime = 0.2f; // Zeit, in der X nicht überschrieben wird

    [Header("Jetpack (nach Dash)")]
    [SerializeField] private float maxJetpackFuel = 2f;          // Sekunden Hover
    [SerializeField] private float jetpackHoverFallSpeed = -2f;  // max. Fallgeschw. beim Hover
    [SerializeField] private float jetpackUpDrift = 2f;          // leichter Updrift
    [SerializeField] private float jetpackRefillRate = 2f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform spriteRoot; // NUR fürs Flippen

    // --- State ---
    float horizontal;
    float vertical;
    bool isFacingRight = true;

    bool isGrounded;
    bool isOnWall;
    bool isWallSliding;
    bool isWallGrabbing;
    int wallDirectionX; // -1 = links, 1 = rechts

    bool canDash = true;
    bool isDashing;

    bool jetpackAvailable = false; // wird NACH dem Dash freigeschaltet
    bool isHovering;

    float coyoteCounter;
    float jumpBufferCounter;
    float wallJumpLockTimer;

    float currentJetpackFuel;

    Vector2 lastMoveDir = Vector2.right;

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = baseGravityScale;

        currentJetpackFuel = maxJetpackFuel;

        if (tr != null)
            tr.emitting = false;
    }

    void Update()
    {
        if (isDashing) return;

        // --- Input ---
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical   = Input.GetAxisRaw("Vertical");

        Vector2 currentInput = new Vector2(horizontal, vertical);
        if (currentInput.sqrMagnitude > 0.01f)
            lastMoveDir = currentInput.normalized;

        bool shiftDown = Input.GetKeyDown(KeyCode.RightShift);
        bool shiftHeld = Input.GetKey(KeyCode.RightShift);
        bool grabHeld  = Input.GetKey(KeyCode.LeftShift);
        bool jumpDown  = Input.GetButtonDown("Jump");
        bool jumpUp    = Input.GetButtonUp("Jump");

        // --- Ground / Wall ---
        UpdateGroundAndWallState();

        // --- Coyote & Buffer ---
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        if (jumpDown)
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (wallJumpLockTimer > 0f)
            wallJumpLockTimer -= Time.deltaTime;

        // Auf dem Boden: neuen Cycle starten
        if (isGrounded && !isDashing)
        {
            canDash = true;
            jetpackAvailable = false;
            isHovering = false;
        }

        // Wallgrab
        isWallGrabbing = isOnWall && !isGrounded && grabHeld && wallJumpLockTimer <= 0f;

        // Walljump (direkt von der Wand)
        if (isOnWall && !isGrounded && jumpDown && wallJumpLockTimer <= 0f)
        {
            HandleWallJump();
        }

        // Normaler Jump mit Coyote + Buffer
        if (!isWallGrabbing && jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            HandleJump();
        }

        // Jump-Cut
        if (jumpUp && rb.linearVelocity.y > 0f)
        {
            HandleJumpCut();
        }

        // RightShift: Dash ODER (nach Dash) Jetpack mit zweitem Druck
        if (shiftDown)
        {
            if (canDash)
            {
                StartCoroutine(Dash());
            }
            else if (jetpackAvailable && !isHovering && !isGrounded && !isOnWall && currentJetpackFuel > 0f)
            {
                // zweiter Druck nach Dash: Jetpack starten
                isHovering = true;
            }
        }

        // Jetpack Fuel / Stop
        UpdateJetpack(shiftHeld);

        FlipSprite();
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        if (isWallGrabbing)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        ApplyGravity();

        float currentX = rb.linearVelocity.x;
        float targetX  = horizontal * moveSpeed;

        bool pressingTowardsWall = horizontal != 0f && Mathf.Sign(horizontal) == wallDirectionX;
        bool movingDown = rb.linearVelocity.y <= 0f;   // NEU

        // Wallslide nur, wenn man wirklich fällt
        isWallSliding = isOnWall 
                        && !isGrounded 
                        && pressingTowardsWall 
                        && !isWallGrabbing 
                        && wallJumpLockTimer <= 0f
                        && movingDown;                // HIER ergänzt

        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(0f, wallSlideSpeed);
            return;
        }

        if (wallJumpLockTimer > 0f)
        {
            targetX = currentX;
        }

        rb.linearVelocity = new Vector2(targetX, rb.linearVelocity.y);
    }


    // ---------- Jump / Walljump ----------

    void HandleJump()
    {
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    void HandleJumpCut()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
    }

    void HandleWallJump()
    {
        rb.gravityScale = baseGravityScale;
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;

        // weg von der Wand
        Vector2 jumpDir = new Vector2(-wallDirectionX * wallJumpForceX, wallJumpForceY);
        rb.linearVelocity = jumpDir;

        isWallGrabbing = false;
        isWallSliding  = false;
        isOnWall       = false;

        wallJumpLockTimer = wallJumpLockTime;
    }

    // ---------- Gravity / Jetpack ----------

    void ApplyGravity()
    {
        if (isHovering)
        {
            // BO3-mäßiger Hover: leicht nach oben oder sehr langsames Fallen
            float targetY = vertical > 0f ? jetpackUpDrift : jetpackHoverFallSpeed;
            float newY = Mathf.Lerp(rb.linearVelocity.y, targetY, 10f * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
            rb.gravityScale = 0f;
            return;
        }

        rb.gravityScale = baseGravityScale;

        if (rb.linearVelocity.y < 0f)
        {
            rb.gravityScale = baseGravityScale * fallGravityMultiplier;
        }
        else if (rb.linearVelocity.y > 0f && !Input.GetButton("Jump"))
        {
            rb.gravityScale = baseGravityScale * jumpCutGravityMultiplier;
        }

        if (rb.linearVelocity.y < maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
    }

    void UpdateJetpack(bool shiftHeld)
    {
        // Fuel aufladen am Boden
        if (isGrounded)
        {
            currentJetpackFuel = Mathf.MoveTowards(
                currentJetpackFuel,
                maxJetpackFuel,
                jetpackRefillRate * Time.deltaTime
            );
            return;
        }

        if (!isHovering) return;

        // Jetpack läuft:
        // Abbrechen wenn Shift losgelassen oder Wand berührt
        if (!shiftHeld || isOnWall)
        {
            isHovering = false;
            jetpackAvailable = false;
            return;
        }

        currentJetpackFuel -= Time.deltaTime;

        if (currentJetpackFuel <= 0f)
        {
            currentJetpackFuel = 0f;
            isHovering = false;
            jetpackAvailable = false;
        }
    }

    // ---------- Dash ----------

    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        isWallGrabbing = false;
        isWallSliding = false;
        isHovering = false;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(0.10f, 0.25f);
        }

        // Richtung bestimmen
        float dashX = Input.GetAxisRaw("Horizontal");
        float dashY = Input.GetAxisRaw("Vertical");
        Vector2 dashDir = new Vector2(dashX, dashY);

        bool dashingFromWall = isOnWall && !isGrounded && wallDirectionX != 0;

        if (dashingFromWall)
        {
            // weg von der Wand, leicht nach oben
            dashDir = new Vector2(-wallDirectionX, 0.3f);
        }
        else if (dashDir.sqrMagnitude < 0.01f)
        {
            dashDir = lastMoveDir.sqrMagnitude > 0.01f
                ? lastMoveDir
                : (isFacingRight ? Vector2.right : Vector2.left);
        }

        dashDir = dashDir.normalized;

        if (tr != null) tr.emitting = true;

        float elapsed = 0f;
        while (elapsed < dashingTime)
        {
            float t = elapsed / dashingTime;
            float speedMultiplier = Mathf.SmoothStep(1.2f, 0.4f, t);
            rb.linearVelocity = dashDir * dashingPower * speedMultiplier;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (tr != null) tr.emitting = false;

        rb.gravityScale = originalGravity;
        isDashing = false;

        // Nach dem Dash: Jetpack für zweiten Shift-Druck freischalten
        jetpackAvailable = true;
    }

    // ---------- Ground / Wall ----------

    void UpdateGroundAndWallState()
    {
        
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        
        
        bool leftHigh  = Physics2D.OverlapCircle(wallCheckLeftHigh.position,  checkRadius, wallLayer);
        bool leftLow   = Physics2D.OverlapCircle(wallCheckLeftLow.position,   checkRadius, wallLayer);
        bool rightHigh = Physics2D.OverlapCircle(wallCheckRightHigh.position, checkRadius, wallLayer);
        bool rightLow  = Physics2D.OverlapCircle(wallCheckRightLow.position,  checkRadius, wallLayer);

        bool leftHit  = leftHigh  || leftLow;
        bool rightHit = rightHigh || rightLow;

        bool anyWall = leftHit || rightHit;

        isOnWall = anyWall && !isGrounded;

// Wand-Richtung bestimmen
        if (!anyWall)
            wallDirectionX = 0;
        else if (leftHit && !rightHit)
            wallDirectionX = -1;
        else if (rightHit && !leftHit)
            wallDirectionX = 1;
        else
        {
            // Ecke → fallback
            if (horizontal != 0)
                wallDirectionX = horizontal > 0 ? 1 : -1;
            else
                wallDirectionX = isFacingRight ? 1 : -1;
        }

    }

    // ---------- Utils ----------

    void FlipSprite()
    {
        if (horizontal == 0f) return;
        if (spriteRoot == null) return;

        bool movingRight = horizontal > 0f;

        if (movingRight != isFacingRight)
        {
            isFacingRight = movingRight;

            Vector3 ls = spriteRoot.localScale;
            ls.x = Mathf.Abs(ls.x) * (isFacingRight ? 1f : -1f);
            spriteRoot.localScale = ls;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }

        if (wallCheckLeft != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wallCheckLeft.position, checkRadius);
        }

        if (wallCheckRight != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wallCheckRight.position, checkRadius);
        }
    }
}
