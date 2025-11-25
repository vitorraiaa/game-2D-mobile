using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics; // para Stopwatch

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movimento")]
    public float walkSpeed = 3.5f;
    public float runSpeed  = 8f;
    public float aceleracaoNoChao = 60f;
    public float aceleracaoNoAr   = 40f;

    [Header("Pulo")]
    public float forcaPulo = 10.5f;
    public float coyoteTime   = 0.12f;
    public float jumpBuffer   = 0.12f;
    public float gravidadeDescida = 2.5f;
    public float gravidadeSubida  = 2.0f;

    [Header("Limite de Altura do Pulo")]
    public bool  limitarAlturaDoPulo = true;
    public float tempoMaxSubida = 0.16f;

    [Header("High Jump (via pickup)")]
    public bool  hasHighJumpPower = false;
    public float highJumpForce    = 18f;
    public float highJumpCoyote   = 0.10f;
    public float highJumpNoCutTime = 0.15f;
    public float tempoMaxSubidaHigh = 0.20f;

    [Header("Chão (sólido)")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.18f;
    public LayerMask groundMask;

    [Header("Plataformas (OneWay)")]
    public LayerMask oneWayMask;
    public LayerMask bridgeMask;
    public float dropThroughTime = 0.35f;
    public KeyCode dropKey = KeyCode.S; // (fica, mas agora usamos InputRouter)
    public float groundCastDistance = 0.08f;
    public float movingPlatformStick = 0.15f;

    [Header("Estabilização de Grounded")]
    public float groundHysteresis = 0.06f;
    public float ungroundYThreshold = 0.15f;

    [Header("Escada (Climb)")]
    public string ladderLayerName = "Ladder";
    public float climbSpeed = 3.0f;
    public float descendExtra = 1.0f;
    public float climbExitHorizontalBoost = 0f;

    [Header("Animator (auto)")]
    public Animator animator;

    // internos
    Rigidbody2D rb;
    SpriteRenderer sr;
    Collider2D col;

    int ladderLayer;
    bool emLadderZone = false;
    bool isClimbing   = false;

    float inputX;
    float inputY;

    // pulo
    float coyoteTimer;
    float jumpBufferTimer;
    float tempoDesdeInicioDoPulo;
    bool  emSubidaLimitada;
    bool  emHighJumpSubida;
    float noCutTimer;
    bool  queuedHighJump = false;

    // Nova lógica com Stopwatch
    private Stopwatch coyoteTimeStopwatch = new Stopwatch();
    private Stopwatch jumpBufferStopwatch = new Stopwatch();
    private Stopwatch jumpTimeDebuffStopwatch = new Stopwatch();
    private bool isJumping = false;
    private bool jumpPressed = false;

    // drop-through
    float dropThroughTimer;
    Collider2D[] myCols;
    readonly List<(Collider2D a, Collider2D b)> _ignores = new();

    // plataforma móvel
    Rigidbody2D currentPlatformRb;
    Vector2 lastPlatformVelocity;

    // grounded estável
    bool isGrounded;

    // raycast
    private float distanceToGround;

    // Animator hashes
    int hSpeed, hIsGrounded, hYVelocity, hJump, hHighJump, hIsClimbing;

    // cache
    float cachedSpeed, cachedYVel;
    bool  cachedIsGrounded, cachedIsClimbing;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        if (!animator) animator = GetComponent<Animator>();

        if (!groundCheck)
        {
            var found = transform.Find("GroundCheck");
            if (found) groundCheck = found;
            else
            {
                var go = new GameObject("GroundCheck");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0f, -0.7f, 0f);
                groundCheck = go.transform;
            }
        }
        if (groundMask == 0) groundMask = LayerMask.GetMask("Ground");
        myCols = GetComponentsInChildren<Collider2D>();

        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        ladderLayer = LayerMask.NameToLayer(ladderLayerName);

        hSpeed       = Animator.StringToHash("Speed");
        hIsGrounded  = Animator.StringToHash("IsGrounded");
        hYVelocity   = Animator.StringToHash("YVelocity");
        hJump        = Animator.StringToHash("Jump");
        hHighJump    = Animator.StringToHash("HighJump");
        hIsClimbing  = Animator.StringToHash("IsClimbing");

        isGrounded = false;
        distanceToGround = col.bounds.extents.y;

        CacheAnim(-1f, -999f, false, false, force:true);
    }

    void Update()
    {
        // ====== INPUT via InputRouter ======
        inputX = InputRouter.AxisX();
        inputY = InputRouter.AxisY();

        if (inputX != 0) sr.flipX = inputX < 0;

        // Buffer de pulo (equivalente a apertar espaço/Jump)
        if (InputRouter.JumpTap())
            jumpBufferTimer = jumpBuffer;
        jumpBufferTimer -= Time.deltaTime;

        // “corte de subida” quando solta o pulo
        HandleJumpForce();
        if (jumpTimeDebuffStopwatch.ElapsedTimeSec() < coyoteTime + 0.01f)
        {
            return;
        }

        // Press do pulo neste frame (para lógica que usa bool)
        jumpPressed = InputRouter.JumpTap();

        HandleJumpBuffer();
        HandleCoyoteTime();
        HandleJump();
        jumpPressed = false;

        // High Jump via pickup (se quiser usar Special para habilitar o high jump: descomente)
        // if (hasHighJumpPower && InputRouter.SpecialTap())
        //     queuedHighJump = true;

        // Pedido de drop-through (↓/S) — agora pelo eixo Y do roteador
        bool dropPressed = (inputY < -0.7f);
        if (dropPressed)
        {
            dropThroughTimer = dropThroughTime;
            TryStartDropThroughOneWay();
        }
        if (dropThroughTimer > 0f)
            dropThroughTimer -= Time.deltaTime;

        // Ladder: Up/Down controlam entrar/sair
        if (emLadderZone)
        {
            if (!isClimbing && Mathf.Abs(inputY) > 0.05f)
                StartClimb();
            else if (isClimbing && Mathf.Abs(inputY) <= 0.05f)
                StopClimb(resetGravity: false);
        }
        else if (isClimbing)
        {
            StopClimb(resetGravity: true);
        }
    }

    void FixedUpdate()
    {
        if (jumpTimeDebuffStopwatch.ElapsedTimeSec() < coyoteTime + 0.01f)
        {
            return;
        }

        // Coyote time
        if (isGrounded) coyoteTimer = coyoteTime;
        else            coyoteTimer -= Time.fixedDeltaTime;

        // “colar” na plataforma móvel
        if (isGrounded && currentPlatformRb)
        {
            Vector2 pv = currentPlatformRb.linearVelocity;
            float blend = movingPlatformStick;
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x + pv.x * blend,
                Mathf.Max(rb.linearVelocity.y, pv.y)
            );
            lastPlatformVelocity = pv;
        }
        else lastPlatformVelocity = Vector2.zero;

        // Escada
        if (isClimbing)
        {
            rb.gravityScale = 0f;
            float y = inputY * climbSpeed;
            if (y < 0f) y *= (1f + descendExtra);
            float x = inputX * walkSpeed * 0.2f;
            rb.linearVelocity = new Vector2(x, y);
            return;
        }
        else rb.gravityScale = 1f;

        // Movimento horizontal: usa AxisX do roteador
        float moveInput = inputX;

        // (se ainda quiser Shift correr no desktop, pode manter — no mobile ignora)
        bool runHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float speed = runHeld ? runSpeed : walkSpeed;

        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

        // High jump simplificado
        if (queuedHighJump && isGrounded)
        {
            queuedHighJump = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, highJumpForce);
            if (animator) animator.SetTrigger(hHighJump);
        }

        // Descer por OneWay: empurrãozinho se no chão
        if (dropThroughTimer > 0f && isGrounded)
        {
            rb.position += Vector2.down * (groundCastDistance * 1.5f);
        }
    }

    void LateUpdate()
    {
        CacheAnim(Mathf.Abs(rb.linearVelocity.x), rb.linearVelocity.y, isGrounded, isClimbing, force:false);
        if (!animator) return;

        if (!Mathf.Approximately(cachedSpeed, Mathf.Abs(rb.linearVelocity.x)))
        {
            cachedSpeed = Mathf.Abs(rb.linearVelocity.x);
            animator.SetFloat(hSpeed, cachedSpeed);
        }
        if (!Mathf.Approximately(cachedYVel, rb.linearVelocity.y))
        {
            cachedYVel = rb.linearVelocity.y;
            animator.SetFloat(hYVelocity, cachedYVel);
        }
        if (cachedIsGrounded != isGrounded)
        {
            cachedIsGrounded = isGrounded;
            animator.SetBool(hIsGrounded, cachedIsGrounded);
        }
        if (cachedIsClimbing != isClimbing)
        {
            cachedIsClimbing = isClimbing;
            animator.SetBool(hIsClimbing, cachedIsClimbing);
        }
    }

    // —— Climb helpers ——
    void StartClimb()
    {
        isClimbing = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        if (animator) animator.SetBool(hIsClimbing, true);
    }

    void StopClimb(bool resetGravity = true)
    {
        isClimbing = false;
        if (resetGravity) rb.gravityScale = 1f;
        if (animator) animator.SetBool(hIsClimbing, false);
    }

    // — Ground via collision —
    void OnCollisionEnter2D(Collision2D collision)
    {
        int layer = collision.gameObject.layer;
        LayerMask allGroundLayers = groundMask | oneWayMask | bridgeMask;
        if ((allGroundLayers & (1 << layer)) != 0)
        {
            isGrounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        int layer = collision.gameObject.layer;
        LayerMask allGroundLayers = groundMask | oneWayMask | bridgeMask;
        if ((allGroundLayers & (1 << layer)) != 0)
        {
            isGrounded = false;
        }
    }

    // — Escada (Trigger) —
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == ladderLayer)
            emLadderZone = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == ladderLayer)
        {
            emLadderZone = false;
            if (isClimbing)
            {
                StopClimb(resetGravity: true);
                if (climbExitHorizontalBoost != 0f)
                    rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * climbExitHorizontalBoost, rb.linearVelocity.y);
            }
        }
    }

    // — Pulo: corta subida ao soltar (usa InputRouter indiretamente via jumpPressed) —
    private void HandleJumpForce()
    {
        // Se NÃO está pressionando jump (no desktop: Space/Jump), corta subida
        // No mobile o "segurar" não existe por padrão — sem problemas.
        if (!Input.GetButton("Jump") && !Input.GetKey(KeyCode.Space) && isJumping)
        {
            if (rb.linearVelocity.y > 0)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            isJumping = false;
        }
    }

    private void HandleJump()
    {
        if (isGrounded) isJumping = false;

        // Raycast para validar pulo
        float rayDistance = distanceToGround + 0.1f;
        bool canJump = Physics2D.Raycast(transform.position, Vector2.down, rayDistance, groundMask);

        if (jumpPressed)
        {
            UnityEngine.Debug.Log($"[Player] Tentando pular - canJump: {canJump}, isGrounded: {isGrounded}, rayDistance: {rayDistance}");
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayDistance, groundMask);
            if (hit.collider)
                UnityEngine.Debug.Log($"[Player] Raycast HIT: {hit.collider.name}, distance: {hit.distance}");
            else
                UnityEngine.Debug.Log($"[Player] Raycast MISS - groundMask: {groundMask.value}");
        }

        if ((jumpPressed || jumpBufferStopwatch.ElapsedTimeSec() < jumpBuffer) && canJump)
        {
            isJumping = true;
            jumpTimeDebuffStopwatch.Restart();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, forcaPulo);
            if (animator) animator.SetTrigger(hJump);
        }
    }

    private void HandleJumpBuffer()
    {
        if (jumpPressed)
            jumpBufferStopwatch.Restart();
    }

    private void HandleCoyoteTime()
    {
        if (isGrounded)
        {
            coyoteTimeStopwatch.Restart();
            return;
        }
        if (coyoteTimeStopwatch.ElapsedTimeSec() < coyoteTime)
            isGrounded = true;
    }

    // Drop-through
    void TryStartDropThroughOneWay()
    {
        Bounds b = col.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - 0.02f);
        Vector2 size   = new Vector2(b.size.x * 0.9f, 0.08f);

        var oneWayHits = Physics2D.OverlapBoxAll(center, size, 0f, oneWayMask);
        var bridgeHits = Physics2D.OverlapBoxAll(center, size, 0f, bridgeMask);

        var allHits = new List<Collider2D>();
        if (oneWayHits != null) allHits.AddRange(oneWayHits);
        if (bridgeHits != null) allHits.AddRange(bridgeHits);
        if (allHits.Count == 0) return;

        foreach (var platformCol in allHits)
        {
            foreach (var my in myCols)
            {
                if (!my || !platformCol) continue;
                Physics2D.IgnoreCollision(my, platformCol, true);
                _ignores.Add((my, platformCol));
            }
        }
        StartCoroutine(ReenableCollisionsAfter(dropThroughTime));
    }

    IEnumerator ReenableCollisionsAfter(float t)
    {
        yield return new WaitForSeconds(t);
        foreach (var pair in _ignores)
        {
            if (pair.a && pair.b) Physics2D.IgnoreCollision(pair.a, pair.b, false);
        }
        _ignores.Clear();
    }

    void CacheAnim(float spd, float yv, bool g, bool climb, bool force)
    {
        if (force)
        {
            cachedSpeed = spd;
            cachedYVel  = yv;
            cachedIsGrounded = g;
            cachedIsClimbing = climb;
            if (animator)
            {
                animator.SetFloat(hSpeed, spd);
                animator.SetFloat(hYVelocity, yv);
                animator.SetBool (hIsGrounded, g);
                animator.SetBool (hIsClimbing, climb);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
