using System.Collections;
using UnityEngine;
using DG.Tweening;

public class Movement : MonoBehaviour
{
    private const float DefaultGravityScale = 3f;

    private Collision coll;
    [HideInInspector] public Rigidbody2D rb;
    private AnimationScript anim;
    private BetterJumping betterJumping;
    private GhostTrail ghostTrail;
    private PlayerAudio playerAudio;
    private PlayerAbilities playerAbilities;
    private RippleEffect rippleEffect;
    private GameInput gameInput;

    [Space]
    [Header("Stats")]
    public float speed = 10f;
    public float jumpForce = 50f;
    public float slideSpeed = 5f;
    public float wallClimbSpeed = 3.5f;
    public float maxWallClimbTime = 0.75f;
    public float wallClimbRecoverRate = 1.25f;
    public float wallJumpLerp = 10f;
    public float dashSpeed = 20f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Space]
    [Header("Booleans")]
    public bool canMove;
    public bool wallGrab;
    public bool wallJumped;
    public bool wallSlide;
    public bool isDashing;

    [Space]
    public int side = 1;

    [Space]
    [Header("Polish")]
    public ParticleSystem dashParticle;
    public ParticleSystem jumpParticle;
    public ParticleSystem wallJumpParticle;
    public ParticleSystem slideParticle;

    private bool groundTouch;
    private bool hasDashed;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float wallClimbTimeRemaining;
    private bool hasBeenAirborne;

    void Start()
    {
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<AnimationScript>();
        betterJumping = GetComponent<BetterJumping>();
        ghostTrail = FindObjectOfType<GhostTrail>();
        playerAudio = GetComponent<PlayerAudio>();
        playerAbilities = GetComponent<PlayerAbilities>();
        rippleEffect = FindObjectOfType<RippleEffect>();
        gameInput = GameInput.Instance;
        wallClimbTimeRemaining = maxWallClimbTime;
    }

    void Update()
    {
        if (gameInput == null)
        {
            gameInput = GameInput.Instance;
        }

        if (playerAbilities == null)
        {
            playerAbilities = GetComponent<PlayerAbilities>();
        }

        bool gameplayBlocked = GameSession.GameplayInputBlocked;
        float x = gameplayBlocked ? 0f : GetHorizontal();
        float y = gameplayBlocked ? 0f : GetVertical();
        float xRaw = gameplayBlocked ? 0f : GetHorizontalRaw();
        float yRaw = gameplayBlocked ? 0f : GetVerticalRaw();
        Vector2 dir = new Vector2(x, y);

        bool jumpPressed = !gameplayBlocked && GetJumpPressed();
        bool wallGrabHeld = !gameplayBlocked && GetGrabHeld();
        bool dashPressed = !gameplayBlocked && GetDashPressed();

        UpdateJumpTimers(jumpPressed);
        UpdateGroundState();

        Walk(dir);
        anim.SetHorizontalMovement(x, y, rb.velocity.y);

        if (coll.onGround && !isDashing)
        {
            wallJumped = false;
            wallClimbTimeRemaining = maxWallClimbTime;
            if (betterJumping != null)
            {
                betterJumping.enabled = true;
            }
        }

        UpdateWallState(y, wallGrabHeld);
        HandleBufferedJump();
        HandleDashInput(dashPressed, xRaw, yRaw);
        WallParticle(y);

        if (wallGrab || wallSlide || !canMove)
        {
            return;
        }

        if (x > 0f)
        {
            side = 1;
            anim.Flip(side);
        }

        if (x < 0f)
        {
            side = -1;
            anim.Flip(side);
        }
    }

    private void UpdateJumpTimers(bool jumpPressed)
    {
        if (coll.onGround)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        if (jumpPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }

    private void UpdateGroundState()
    {
        if (coll.onGround && !groundTouch)
        {
            GroundTouch();
            groundTouch = true;
        }

        if (!coll.onGround && groundTouch)
        {
            groundTouch = false;
            hasBeenAirborne = true;
        }
    }

    private void UpdateWallState(float verticalInput, bool wallGrabHeld)
    {
        wallGrab = false;
        wallSlide = false;

        if (!canMove || isDashing)
        {
            rb.gravityScale = DefaultGravityScale;
            return;
        }

        bool touchingWallInAir = coll.onWall && !coll.onGround;

        if (touchingWallInAir && wallGrabHeld)
        {
            wallGrab = true;
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(0f, GetWallGrabVerticalSpeed(verticalInput));
            FaceAwayFromWall();
            return;
        }

        rb.gravityScale = DefaultGravityScale;
        RecoverWallClimb(verticalInput);

        if (touchingWallInAir && rb.velocity.y < 0f)
        {
            wallSlide = true;
            WallSlide();
        }
    }

    private void HandleBufferedJump()
    {
        if (jumpBufferCounter <= 0f || isDashing)
        {
            return;
        }

        if (coll.onWall && !coll.onGround)
        {
            anim.SetTrigger("jump");
            WallJump();
            jumpBufferCounter = 0f;
            return;
        }

        if (coyoteCounter > 0f)
        {
            anim.SetTrigger("jump");
            Jump(Vector2.up, false);
            coyoteCounter = 0f;
            jumpBufferCounter = 0f;
        }
    }

    private void HandleDashInput(bool dashPressed, float xRaw, float yRaw)
    {
        if (!dashPressed || hasDashed)
        {
            return;
        }

        if (playerAbilities != null && !playerAbilities.DashUnlocked)
        {
            return;
        }

        if (xRaw == 0f && yRaw == 0f)
        {
            return;
        }

        Dash(xRaw, yRaw);
    }

    void GroundTouch()
    {
        hasDashed = false;
        isDashing = false;
        side = anim.sr.flipX ? -1 : 1;
        wallClimbTimeRemaining = maxWallClimbTime;

        if (hasBeenAirborne)
        {
            playerAudio?.PlayLanding();
            hasBeenAirborne = false;
        }

        PlayParticle(jumpParticle);
    }

    private void Dash(float x, float y)
    {
        if (Camera.main != null)
        {
            Camera.main.transform.DOComplete();
            Camera.main.transform.DOShakePosition(.2f, .5f, 14, 90, false, true);
        }

        if (rippleEffect != null && Camera.main != null)
        {
            rippleEffect.Emit(Camera.main.WorldToViewportPoint(transform.position));
        }

        hasDashed = true;
        anim.SetTrigger("dash");
        playerAudio?.PlayDash();

        rb.velocity = Vector2.zero;
        Vector2 dir = new Vector2(x, y);
        rb.velocity += dir.normalized * dashSpeed;
        StartCoroutine(DashWait());
    }

    IEnumerator DashWait()
    {
        if (ghostTrail != null)
        {
            ghostTrail.ShowGhost();
        }

        StartCoroutine(GroundDash());
        DOVirtual.Float(14, 0, .8f, RigidbodyDrag);

        PlayParticle(dashParticle);
        rb.gravityScale = 0f;
        if (betterJumping != null)
        {
            betterJumping.enabled = false;
        }

        wallJumped = true;
        isDashing = true;

        yield return new WaitForSeconds(.3f);

        StopParticle(dashParticle);
        rb.gravityScale = DefaultGravityScale;
        if (betterJumping != null)
        {
            betterJumping.enabled = true;
        }

        wallJumped = false;
        isDashing = false;
    }

    IEnumerator GroundDash()
    {
        yield return new WaitForSeconds(.15f);
        if (coll.onGround)
        {
            hasDashed = false;
        }
    }

    private void WallJump()
    {
        if ((side == 1 && coll.onRightWall) || (side == -1 && !coll.onRightWall))
        {
            side *= -1;
            anim.Flip(side);
        }

        StopCoroutine(DisableMovement(0f));
        StartCoroutine(DisableMovement(.1f));

        Vector2 wallDir = coll.onRightWall ? Vector2.left : Vector2.right;
        Jump((Vector2.up / 1.5f + wallDir / 1.5f), true);

        wallJumped = true;
        coyoteCounter = 0f;
    }

    private void WallSlide()
    {
        FaceAwayFromWall();

        if (!canMove)
        {
            return;
        }

        float horizontalVelocity = rb.velocity.x;
        if ((horizontalVelocity > 0f && coll.onRightWall) || (horizontalVelocity < 0f && coll.onLeftWall))
        {
            horizontalVelocity = 0f;
        }

        rb.velocity = new Vector2(horizontalVelocity, Mathf.Max(rb.velocity.y, -slideSpeed));
    }

    private void Walk(Vector2 dir)
    {
        if (!canMove || wallGrab)
        {
            return;
        }

        if (!wallJumped)
        {
            rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
        }
        else
        {
            Vector2 targetVelocity = new Vector2(dir.x * speed, rb.velocity.y);
            rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, wallJumpLerp * Time.deltaTime);
        }
    }

    private void Jump(Vector2 dir, bool wall)
    {
        SetSlideParticleDirection();

        ParticleSystem particle = wall ? wallJumpParticle : jumpParticle;
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.velocity += dir * jumpForce;

        playerAudio?.PlayJump();
        PlayParticle(particle);
    }

    IEnumerator DisableMovement(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    void RigidbodyDrag(float x)
    {
        rb.drag = x;
    }

    void WallParticle(float vertical)
    {
        if (slideParticle == null)
        {
            return;
        }

        var main = slideParticle.main;
        if (wallSlide || (wallGrab && vertical < 0f))
        {
            SetSlideParticleDirection();
            main.startColor = Color.white;
        }
        else
        {
            main.startColor = Color.clear;
        }
    }

    int ParticleSide()
    {
        return coll.onRightWall ? 1 : -1;
    }

    void FaceAwayFromWall()
    {
        if (coll.wallSide != 0 && coll.wallSide != side)
        {
            anim.Flip(side * -1);
        }
    }

    void SetSlideParticleDirection()
    {
        if (slideParticle == null || slideParticle.transform.parent == null)
        {
            return;
        }

        slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1f, 1f);
    }

    void PlayParticle(ParticleSystem particleSystem)
    {
        if (particleSystem != null)
        {
            particleSystem.Play();
        }
    }

    void StopParticle(ParticleSystem particleSystem)
    {
        if (particleSystem != null)
        {
            particleSystem.Stop();
        }
    }

    public void SuspendControl()
    {
        ResetMotionState();
        canMove = false;
        enabled = false;
    }

    public void ResumeControl()
    {
        ResetMotionState();
        canMove = true;
        enabled = true;
    }

    private void ResetMotionState()
    {
        StopAllCoroutines();

        wallGrab = false;
        wallJumped = false;
        wallSlide = false;
        isDashing = false;
        groundTouch = false;
        hasDashed = false;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        hasBeenAirborne = false;
        wallClimbTimeRemaining = maxWallClimbTime;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.drag = 0f;
            rb.gravityScale = DefaultGravityScale;
        }
    }

    private float GetWallGrabVerticalSpeed(float verticalInput)
    {
        if (verticalInput > 0.1f)
        {
            if (wallClimbTimeRemaining > 0f)
            {
                wallClimbTimeRemaining = Mathf.Max(0f, wallClimbTimeRemaining - Time.deltaTime);
                return wallClimbSpeed;
            }

            return 0f;
        }

        if (verticalInput < -0.1f)
        {
            return -slideSpeed;
        }

        return 0f;
    }

    private void RecoverWallClimb(float verticalInput)
    {
        if (coll.onGround)
        {
            wallClimbTimeRemaining = maxWallClimbTime;
            return;
        }

        if (coll.onWall || verticalInput <= 0.1f)
        {
            wallClimbTimeRemaining = Mathf.Min(
                maxWallClimbTime,
                wallClimbTimeRemaining + wallClimbRecoverRate * Time.deltaTime);
        }
    }

    private float GetHorizontal()
    {
        return gameInput != null ? gameInput.GetHorizontal() : Input.GetAxis("Horizontal");
    }

    private float GetVertical()
    {
        return gameInput != null ? gameInput.GetVertical() : Input.GetAxis("Vertical");
    }

    private float GetHorizontalRaw()
    {
        return gameInput != null ? gameInput.GetHorizontalRaw() : Input.GetAxisRaw("Horizontal");
    }

    private float GetVerticalRaw()
    {
        return gameInput != null ? gameInput.GetVerticalRaw() : Input.GetAxisRaw("Vertical");
    }

    private bool GetJumpPressed()
    {
        return gameInput != null ? gameInput.GetJumpPressed() : Input.GetButtonDown("Jump");
    }

    private bool GetDashPressed()
    {
        return gameInput != null ? gameInput.GetDashPressed() : (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter));
    }

    private bool GetGrabHeld()
    {
        return gameInput != null ? gameInput.GetGrabHeld() : (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
    }
}
