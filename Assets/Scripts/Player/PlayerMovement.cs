using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform cameraRoot;   // pitch + roll
    public Camera playerCamera;

    private CharacterController cc;

    // -----------------------
    // Look
    // -----------------------
    [Header("Look")]
    public bool lockCursor = true;
    public float mouseSensitivity = 2.2f;
    public bool invertY = false;
    public float maxPitch = 85f;

    private float yaw;
    private float pitch;

    // -----------------------
    // Input
    // -----------------------
    [Header("Input")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public bool holdToCrouch = true;

    // -----------------------
    // Direction smoothing
    // -----------------------
    [Header("Directional Smoothing")]
    public bool smoothDirectionalInput = true;

    [Tooltip("How smooth movement direction changes feel while pressing into a direction.")]
    public float inputSmoothTime = 0.08f;

    [Tooltip("How quickly movement settles when releasing input.")]
    public float inputReleaseSmoothTime = 0.06f;

    private Vector2 rawMoveInput;
    private Vector2 smoothMoveInput;
    private Vector2 smoothMoveInputVelocity;

    // -----------------------
    // Speed
    // -----------------------
    [Header("Speed")]
    public float walkSpeed = 8.5f;
    public float sprintSpeed = 12.0f;
    public float airMaxSpeed = 11f;

    [Header("Directional Speed Multipliers")]
    [Tooltip("Multiplier applied when moving fully backwards. Diagonal backwards blends smoothly.")]
    [Range(0.1f, 1f)] public float backwardSpeedMultiplier = 0.72f;

    // -----------------------
    // Acceleration
    // -----------------------
    [Header("Acceleration")]
    public float groundAccel = 110f;
    public float groundDecel = 90f;
    public float airAccel = 30f;
    public float airDecel = 8f;

    // -----------------------
    // Jump + Gravity
    // -----------------------
    [Header("Jump & Gravity")]
    public float gravity = 38f;
    public float jumpVelocity = 8.5f;
    public float terminalFallSpeed = 45f;

    [Tooltip("Jump allowed this long after leaving ground.")]
    public float coyoteTime = 0.10f;

    [Tooltip("If jump pressed shortly before landing, it triggers on land.")]
    public float jumpBuffer = 0.10f;

    private float coyoteTimer;
    private float jumpBufferTimer;

    [Header("Double Jump")]
    public bool enableDoubleJump = true;
    public float doubleJumpVelocity = 8.5f;

    private int remainingAirJumps;

    // -----------------------
    // Crouch (height + camera)
    // -----------------------
    [Header("Crouch")]
    public float standHeight = 2.0f;
    public float crouchHeight = 1.2f;
    public float heightLerpSpeed = 14f;

    public float camStandY = 0.9f;
    public float camCrouchY = 0.55f;

    private bool isCrouched;
    private float currentCameraBaseY;

    // -----------------------
    // Slide
    // -----------------------
    [Header("Slide")]
    public bool enableSlide = true;
    public bool slideRequiresSprint = true;

    public float slideDuration = 0.65f;
    public float slideStartSpeed = 13.5f;
    public float slideFriction = 18.0f;
    public float slideEndSpeed = 5.0f;
    public float slideSteer = 6.0f;

    private bool isSliding;
    private float slideTimer;
    private Vector3 slideDir;

    // -----------------------
    // Dodge
    // -----------------------
    [Header("Dodge")]
    public bool enableDodge = true;
    public float dodgeSpeed = 14f;
    public float dodgeDuration = 0.22f;
    public float dodgeCooldown = 0.20f;
    public float dodgeUpKick = 1.5f;

    private bool isDodging;
    private float dodgeTimer;
    private float dodgeCooldownTimer;
    private Vector3 dodgeDir;

    // -----------------------
    // Parkour surfaces
    // -----------------------
    [Header("Parkour Surfaces")]
    [Tooltip("Create ONE layer named ParkourWall and assign runnable/latchable walls to it.")]
    public LayerMask parkourMask;

    // -----------------------
    // Wall Run
    // -----------------------
    [Header("Wall Run")]
    public bool enableWallRun = true;

    public float wallCheckDistance = 0.85f;
    public float wallCheckHeight = 1.0f;

    public float wallRunMaxTime = 1.25f;
    public float wallRunStickTime = 0.10f;

    public float wallRunSpeed = 13.0f;
    public float wallRunAccel = 70f;

    [Range(0f, 1f)] public float wallRunGravityScale = 0.25f;
    public float wallRunMaxFallSpeed = 10f;

    public float wallAdhesion = 8f;

    public float wallRunMinForwardInput = 0.10f;
    [Range(0f, 1f)] public float wallRunAngleMaxDot = 0.55f;
    public float wallApproachMinDot = 0.15f;

    [Header("Wall Jump (from wallrun)")]
    public bool enableWallJump = true;
    public float wallJumpUpVelocity = 8.5f;
    public float wallJumpAwaySpeed = 7.5f;
    public float wallJumpAlongBoost = 2.0f;

    [Header("Wall Run Camera")]
    public float wallRunRoll = 12f;
    public float wallRunRollLerp = 16f;
    public bool invertWallRunRollDirection = false;

    private bool isWallRunning;
    private float wallRunTimer;
    private float wallLostTimer;

    private Vector3 wallNormal;
    private int wallSide;      // -1 left, +1 right
    private Vector3 wallAlong; // along-wall direction

    // -----------------------
    // Wall Latch (platformer-style cling)
    // -----------------------
    [Header("Wall Latch (Cling)")]
    public bool enableWallLatch = true;

    [Tooltip("How far forward to check for a latchable wall (from chest height).")]
    public float latchCheckDistance = 0.95f;

    [Tooltip("Height above player position to cast checks from.")]
    public float latchCheckHeight = 1.0f;

    [Tooltip("How directly you must face the wall to ENTER latch.")]
    [Range(0f, 1f)] public float latchLookDot = 0.90f;

    [Tooltip("Must be moving into the wall to ENTER latch.")]
    public float latchApproachMinDot = 0.20f;

    [Tooltip("Latch only if you pressed jump recently (jump-into-wall).")]
    public float latchJumpInputWindow = 0.22f;

    [Tooltip("How long you can hang on the wall.")]
    public float latchMaxTime = 2.5f;

    [Tooltip("Grace time so latch doesn't drop on tiny raycast gaps.")]
    public float latchGraceTime = 0.12f;

    [Tooltip("Small push into the wall while latched to keep CC contact stable.")]
    public float latchPushSpeed = 2.0f;

    [Tooltip("Press backwards (S) to drop from the wall.")]
    public bool dropFromLatchOnBackInput = true;

    [Tooltip("Latch can only start while sprinting.")]
    public bool latchRequiresSprint = true;

    [Tooltip("Minimum horizontal speed required to begin a latch.")]
    public float latchMinStartSpeed = 6f;

    [Header("Latch Jump")]
    public bool enableLatchJump = true;
    public float latchJumpUpVelocity = 8.8f;
    public float latchJumpAwaySpeed = 8.5f;

    [Header("Latch Auto Turn")]
    public bool autoTurnOnLatch = true;
    public float latchTurnSpeed = 900f;
    public bool lockMouseYawDuringLatchTurn = true;

    private bool isLatched;
    private float latchTimer;
    private float latchLostTimer;
    private Vector3 latchNormal;

    private float timeSinceJumpPressed = 999f;

    private bool latchTurning;
    private float latchTargetYaw;

    // -----------------------
    // Camera FOV
    // -----------------------
    [Header("Camera FOV")]
    public float baseFOV = 80f;
    public float sprintFOV = 92f;
    public float fovLerp = 14f;

    // -----------------------
    // Head Bob + Strafe Tilt
    // -----------------------
    [Header("Head Bob")]
    public bool enableHeadBob = true;
    public float bobFrequency = 10f;
    public float bobAmplitude = 0.045f;
    public float sprintBobFrequency = 14f;
    public float sprintBobAmplitude = 0.07f;
    public float bobLerpSpeed = 14f;

    [Header("Strafe Tilt")]
    public bool enableStrafeTilt = true;
    public float strafeTiltAngle = 6f;
    public float strafeTiltLerp = 10f;

    private float bobTime;
    private float currentBobOffset;
    private float currentStrafeTilt;

    // -----------------------
    // Debug Gizmos
    // -----------------------
    [Header("Debug Gizmos")]
    public bool drawRaycastGizmos = true;
    public bool drawGizmosWhenNotSelected = false;
    public float gizmoSphereSize = 0.04f;
    public float gizmoHitMarkerSize = 0.08f;

    // -----------------------
    // State
    // -----------------------
    private Vector3 velocity; // world-space
    private Vector3 wishDir;  // camera-relative world-space
    private bool wantsSprint;

    private float roll;

    public float CurrentHorizontalSpeed => new Vector3(velocity.x, 0f, velocity.z).magnitude;

    void Awake()
    {
        cc = GetComponent<CharacterController>();

        if (playerCamera != null)
            playerCamera.fieldOfView = baseFOV;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        yaw = transform.eulerAngles.y;

        cc.height = standHeight;
        cc.center = new Vector3(cc.center.x, standHeight * 0.5f, cc.center.z);

        currentCameraBaseY = camStandY;
        remainingAirJumps = enableDoubleJump ? 1 : 0;

        if (cameraRoot != null)
        {
            Vector3 p = cameraRoot.localPosition;
            p.y = camStandY;
            cameraRoot.localPosition = p;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        LookTick(dt);
        InputTick(dt);
        TimersTick(dt);

        GroundingAndGravityTick(dt);

        // Priority: Latch > WallRun
        WallLatchTick(dt);
        WallRunTick(dt);

        MovementTick(dt);

        CrouchAndSlideTick(dt);

        CameraTick(dt);
        HeadBobTick(dt);
        ApplyCameraRotation(dt);
        PlayerLoopAudioTick();

        cc.Move(velocity * dt);
    }

    // -----------------------
    // Look
    // -----------------------
    private void LookTick(float dt)
    {
        if (cameraRoot == null) return;

        float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        if (!(lockMouseYawDuringLatchTurn && latchTurning))
            yaw += mx;

        if (latchTurning)
        {
            yaw = Mathf.MoveTowardsAngle(yaw, latchTargetYaw, latchTurnSpeed * dt);

            if (Mathf.Abs(Mathf.DeltaAngle(yaw, latchTargetYaw)) < 0.25f)
            {
                yaw = latchTargetYaw;
                latchTurning = false;
            }
        }

        float ySign = invertY ? 1f : -1f;
        pitch += my * ySign;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void ApplyCameraRotation(float dt)
    {
        if (cameraRoot == null) return;

        float wallRoll = 0f;
        if (isWallRunning)
        {
            float raw = (wallSide == +1) ? -wallRunRoll : wallRunRoll;
            wallRoll = invertWallRunRollDirection ? -raw : raw;
        }

        float strafeRoll = 0f;
        if (enableStrafeTilt && !isWallRunning && !isLatched)
        {
            float strafeInput = smoothDirectionalInput ? smoothMoveInput.x : rawMoveInput.x;
            float targetStrafeRoll = -strafeInput * strafeTiltAngle;
            currentStrafeTilt = Mathf.Lerp(currentStrafeTilt, targetStrafeRoll, strafeTiltLerp * dt);
            strafeRoll = currentStrafeTilt;
        }
        else
        {
            currentStrafeTilt = Mathf.Lerp(currentStrafeTilt, 0f, strafeTiltLerp * dt);
            strafeRoll = currentStrafeTilt;
        }

        float targetRoll = wallRoll + strafeRoll;
        roll = Mathf.Lerp(roll, targetRoll, wallRunRollLerp * dt);

        Vector3 localPos = cameraRoot.localPosition;
        localPos.y = currentCameraBaseY + currentBobOffset;
        cameraRoot.localPosition = localPos;

        cameraRoot.localRotation = Quaternion.Euler(pitch, 0f, roll);
    }

    // -----------------------
    // Input
    // -----------------------
    private void InputTick(float dt)
    {
        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");

        rawMoveInput = new Vector2(ix, iz);

        if (smoothDirectionalInput)
        {
            float smoothTimeX = Mathf.Abs(ix) > Mathf.Abs(smoothMoveInput.x) ? inputSmoothTime : inputReleaseSmoothTime;
            float smoothTimeZ = Mathf.Abs(iz) > Mathf.Abs(smoothMoveInput.y) ? inputSmoothTime : inputReleaseSmoothTime;

            smoothMoveInput.x = Mathf.SmoothDamp(smoothMoveInput.x, ix, ref smoothMoveInputVelocity.x, smoothTimeX, Mathf.Infinity, dt);
            smoothMoveInput.y = Mathf.SmoothDamp(smoothMoveInput.y, iz, ref smoothMoveInputVelocity.y, smoothTimeZ, Mathf.Infinity, dt);

            smoothMoveInput.x = Mathf.Clamp(smoothMoveInput.x, -1f, 1f);
            smoothMoveInput.y = Mathf.Clamp(smoothMoveInput.y, -1f, 1f);
        }
        else
        {
            smoothMoveInput = rawMoveInput;
            smoothMoveInputVelocity = Vector2.zero;
        }

        wantsSprint = Input.GetKey(sprintKey) && !isCrouched && !isSliding && !isWallRunning && !isLatched && !isDodging;

        GetPlanarBasis(out Vector3 fwd, out Vector3 right);

        Vector2 movementInput = smoothDirectionalInput ? smoothMoveInput : rawMoveInput;
        wishDir = Vector3.ClampMagnitude(right * movementInput.x + fwd * movementInput.y, 1f);

        if (Input.GetKeyDown(jumpKey))
        {
            bool isDodgeInput = ShouldDodgeFromCurrentInput(ix, iz);

            if (enableDodge && isDodgeInput)
            {
                bool canTryDodge =
                    !isLatched &&
                    !isWallRunning &&
                    !isSliding &&
                    !isDodging &&
                    dodgeCooldownTimer <= 0f &&
                    cc.isGrounded;

                if (canTryDodge)
                    StartDodge();

                return;
            }

            if (!isLatched)
                jumpBufferTimer = jumpBuffer;

            timeSinceJumpPressed = 0f;
        }
    }

    private bool ShouldDodgeFromCurrentInput(float ix, float iz)
    {
        Vector2 move = new Vector2(ix, iz);

        if (move.sqrMagnitude < 0.01f)
            return false;

        if (iz > 0.01f)
            return false;

        return true;
    }

    // -----------------------
    // Timers
    // -----------------------
    private void TimersTick(float dt)
    {
        if (jumpBufferTimer > 0f) jumpBufferTimer -= dt;
        if (coyoteTimer > 0f) coyoteTimer -= dt;

        timeSinceJumpPressed += dt;

        if (dodgeCooldownTimer > 0f)
            dodgeCooldownTimer -= dt;

        if (isDodging)
        {
            dodgeTimer -= dt;
            if (dodgeTimer <= 0f)
                StopDodge();
        }

        if (isSliding)
        {
            slideTimer -= dt;
            if (slideTimer <= 0f) StopSlide();
        }

        if (isWallRunning)
        {
            wallRunTimer -= dt;
            if (wallRunTimer <= 0f) StopWallRun();
        }

        if (isLatched)
        {
            latchTimer -= dt;
            if (latchTimer <= 0f) StopLatch();
        }
    }

    // -----------------------
    // Grounding + gravity
    // -----------------------
    private void GroundingAndGravityTick(float dt)
    {
        if (cc.isGrounded)
        {
            coyoteTimer = coyoteTime;
            remainingAirJumps = enableDoubleJump ? 1 : 0;

            if (isWallRunning) StopWallRun();
            if (isLatched) StopLatch();

            if (velocity.y < 0f) velocity.y = -2f;

            if (jumpBufferTimer > 0f)
            {
                velocity.y = jumpVelocity;
                coyoteTimer = 0f;
                jumpBufferTimer = 0f;
                SoundManager.Instance?.PlayOneShot2D(SoundId.Jump);
            }
        }
        else
        {
            if (!isWallRunning && !isLatched)
            {
                velocity.y -= gravity * dt;
                if (velocity.y < -terminalFallSpeed) velocity.y = -terminalFallSpeed;
            }
        }
    }

    // -----------------------
    // Movement tick (state priority)
    // -----------------------
    private void MovementTick(float dt)
    {
        if (isLatched)
        {
            LatchMoveTick(dt);
            return;
        }

        if (isDodging)
        {
            DodgeMoveTick(dt);
            return;
        }

        if (jumpBufferTimer > 0f && (cc.isGrounded || coyoteTimer > 0f || isWallRunning))
        {
            if (isSliding) StopSlide();

            if (isWallRunning && enableWallJump)
                WallJump();
            else
            {
                velocity.y = jumpVelocity;
                coyoteTimer = 0f;
                SoundManager.Instance?.PlayOneShot2D(SoundId.Jump);
            }

            jumpBufferTimer = 0f;
        }

        if (jumpBufferTimer > 0f &&
            !cc.isGrounded &&
            coyoteTimer <= 0f &&
            !isWallRunning &&
            !isLatched &&
            enableDoubleJump &&
            remainingAirJumps > 0)
        {
            if (isSliding) StopSlide();

            velocity.y = doubleJumpVelocity;
            remainingAirJumps--;
            jumpBufferTimer = 0f;
            SoundManager.Instance?.PlayOneShot2D(SoundId.Jump);
        }

        if (isSliding)
        {
            SlideMoveTick(dt);
            return;
        }

        if (isWallRunning)
        {
            WallRunMoveTick(dt);
            return;
        }

        NormalMoveTick(dt);
    }

    private void NormalMoveTick(float dt)
    {
        float baseTargetSpeed = wantsSprint ? sprintSpeed : walkSpeed;

        Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
        bool hasInput = wishDir.sqrMagnitude > 0.0001f;

        Vector2 movementInput = smoothDirectionalInput ? smoothMoveInput : rawMoveInput;
        Vector2 clampedInput = Vector2.ClampMagnitude(movementInput, 1f);

        float backwardWeight = Mathf.Clamp01(-clampedInput.y);
        float directionalSpeedMultiplier = Mathf.Lerp(1f, backwardSpeedMultiplier, backwardWeight);

        float targetSpeed = baseTargetSpeed * directionalSpeedMultiplier;

        if (cc.isGrounded)
        {
            float accel = hasInput ? groundAccel : groundDecel;
            Vector3 desired = hasInput ? wishDir * targetSpeed : Vector3.zero;
            horiz = Vector3.MoveTowards(horiz, desired, accel * dt);
        }
        else
        {
            float accel = hasInput ? airAccel : airDecel;
            float airTarget = Mathf.Min(targetSpeed, airMaxSpeed);
            Vector3 desired = hasInput ? wishDir * airTarget : horiz;
            horiz = Vector3.MoveTowards(horiz, desired, accel * dt);
        }

        velocity.x = horiz.x;
        velocity.z = horiz.z;
    }

    // -----------------------
    // Dodge
    // -----------------------
    private void StartDodge()
    {
        isDodging = true;
        dodgeTimer = dodgeDuration;
        dodgeCooldownTimer = dodgeCooldown;

        GetPlanarBasis(out Vector3 fwd, out Vector3 right);

        Vector2 dodgeInput = rawMoveInput;
        Vector3 dir = Vector3.ClampMagnitude(right * dodgeInput.x + fwd * dodgeInput.y, 1f);

        if (dir.sqrMagnitude < 0.01f)
            dir = transform.forward;

        dodgeDir = dir.normalized;

        velocity.x = dodgeDir.x * dodgeSpeed;
        velocity.z = dodgeDir.z * dodgeSpeed;

        if (dodgeUpKick > 0f)
            velocity.y = dodgeUpKick;
        else
            velocity.y = 0f;

        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        SoundManager.Instance?.PlayOneShot2D(SoundId.Dash);
    }

    private void StopDodge()
    {
        isDodging = false;
        dodgeTimer = 0f;
    }

    private void DodgeMoveTick(float dt)
    {
        velocity.x = dodgeDir.x * dodgeSpeed;
        velocity.z = dodgeDir.z * dodgeSpeed;
    }

    // -----------------------
    // Crouch + Slide
    // -----------------------
    private void CrouchAndSlideTick(float dt)
    {
        if (isDodging)
            return;

        if (enableSlide && Input.GetKeyDown(crouchKey) && cc.isGrounded && !isSliding && !isWallRunning && !isLatched && !isDodging)
        {
            bool moving = new Vector3(velocity.x, 0f, velocity.z).sqrMagnitude > 0.5f;
            if (moving && (!slideRequiresSprint || wantsSprint))
                StartSlide();
        }

        if (!isSliding)
        {
            if (holdToCrouch)
            {
                if (Input.GetKeyDown(crouchKey)) isCrouched = true;
                if (Input.GetKeyUp(crouchKey)) isCrouched = false;
            }
            else
            {
                if (Input.GetKeyDown(crouchKey)) isCrouched = !isCrouched;
            }
        }
        else
        {
            isCrouched = true;
        }

        float desiredHeight = isCrouched ? crouchHeight : standHeight;
        cc.height = Mathf.Lerp(cc.height, desiredHeight, heightLerpSpeed * dt);
        cc.center = new Vector3(cc.center.x, cc.height * 0.5f, cc.center.z);

        float desiredCamY = isCrouched ? camCrouchY : camStandY;
        currentCameraBaseY = Mathf.Lerp(currentCameraBaseY, desiredCamY, heightLerpSpeed * dt);
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        isCrouched = true;

        Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);

        if (horiz.sqrMagnitude > 0.01f) slideDir = horiz.normalized;
        else if (wishDir.sqrMagnitude > 0.01f) slideDir = wishDir.normalized;
        else slideDir = transform.forward;

        float startSpeed = Mathf.Max(horiz.magnitude, slideStartSpeed);

        Vector3 newHoriz = slideDir * startSpeed;
        velocity.x = newHoriz.x;
        velocity.z = newHoriz.z;
    }

    private void StopSlide()
    {
        isSliding = false;
        slideTimer = 0f;

        if (holdToCrouch)
            isCrouched = Input.GetKey(crouchKey);
    }

    private void SlideMoveTick(float dt)
    {
        if (!cc.isGrounded)
        {
            StopSlide();
            return;
        }

        Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);

        if (wishDir.sqrMagnitude > 0.01f && slideSteer > 0f)
            slideDir = Vector3.Slerp(slideDir, wishDir.normalized, slideSteer * dt).normalized;

        float speed = Mathf.MoveTowards(horiz.magnitude, 0f, slideFriction * dt);

        Vector3 newHoriz = slideDir * speed;
        velocity.x = newHoriz.x;
        velocity.z = newHoriz.z;

        if (speed <= slideEndSpeed)
            StopSlide();
    }

    // -----------------------
    // Wall Latch (cling)
    // -----------------------
    private void WallLatchTick(float dt)
    {
        if (!enableWallLatch) return;
        if (cc.isGrounded) return;
        if (isSliding) return;
        if (isDodging) return;

        if (isLatched)
        {
            if (TryGetLatchedWallContact(out Vector3 n))
            {
                latchNormal = n;
                latchLostTimer = latchGraceTime;
            }
            else
            {
                latchLostTimer -= dt;
                if (latchLostTimer <= 0f)
                    StopLatch();
            }
            return;
        }

        if (latchRequiresSprint && !wantsSprint)
            return;

        if (!TryGetFrontWall(out Vector3 nStart))
            return;

        latchNormal = nStart;

        if (playerCamera != null)
        {
            float lookDot = Vector3.Dot(playerCamera.transform.forward.normalized, (-latchNormal).normalized);
            if (lookDot < latchLookDot)
                return;
        }

        Vector3 horizVel = new Vector3(velocity.x, 0f, velocity.z);
        float horizSpeed = horizVel.magnitude;

        if (horizSpeed < latchMinStartSpeed)
            return;

        float approachDot = Vector3.Dot(horizVel.normalized, (-latchNormal).normalized);
        if (approachDot < latchApproachMinDot)
            return;

        if (timeSinceJumpPressed <= latchJumpInputWindow)
        {
            StartLatch();
            StopWallRun();
            latchLostTimer = latchGraceTime;
        }
    }

    private void StartLatch()
    {
        isLatched = true;
        latchTimer = latchMaxTime;
        latchLostTimer = latchGraceTime;

        velocity = Vector3.zero;
        jumpBufferTimer = 0f;

        if (autoTurnOnLatch)
        {
            Vector3 outDir = new Vector3(latchNormal.x, 0f, latchNormal.z);
            if (outDir.sqrMagnitude > 0.0001f)
            {
                outDir.Normalize();
                latchTargetYaw = Quaternion.LookRotation(outDir, Vector3.up).eulerAngles.y;
                latchTurning = true;
            }
        }
        else
        {
            latchTurning = false;
        }
    }

    private void StopLatch()
    {
        isLatched = false;
        latchTimer = 0f;
        latchLostTimer = 0f;
        latchTurning = false;
    }

    private void LatchMoveTick(float dt)
    {
        if (dropFromLatchOnBackInput)
        {
            float back = Input.GetAxisRaw("Vertical");
            if (back < -0.1f)
            {
                StopLatch();
                return;
            }
        }

        if (enableLatchJump && Input.GetKeyDown(jumpKey))
        {
            Vector3 camForward = playerCamera != null ? playerCamera.transform.forward : transform.forward;

            Vector3 jumpDir = Vector3.ProjectOnPlane(camForward, Vector3.up).normalized;

            if (jumpDir.sqrMagnitude < 0.001f)
                jumpDir = transform.forward;

            StopLatch();

            velocity.x = jumpDir.x * latchJumpAwaySpeed;
            velocity.z = jumpDir.z * latchJumpAwaySpeed;
            velocity.y = latchJumpUpVelocity;
            coyoteTimer = 0f;

            SoundManager.Instance?.PlayOneShot2D(SoundId.WallJump);
            return;
        }

        Vector3 pushIntoWall = (-latchNormal.normalized) * latchPushSpeed;

        velocity.x = pushIntoWall.x;
        velocity.z = pushIntoWall.z;
        velocity.y = 0f;
    }

    private bool TryGetLatchedWallContact(out Vector3 normal)
    {
        normal = Vector3.zero;

        Vector3 towardWall = -latchNormal.normalized;
        Vector3 origin = transform.position + Vector3.up * latchCheckHeight + (latchNormal.normalized * 0.25f);
        float dist = latchCheckDistance + 0.35f;

        if (Physics.Raycast(origin, towardWall, out RaycastHit hit, dist, parkourMask, QueryTriggerInteraction.Ignore))
        {
            normal = hit.normal;
            return true;
        }

        return false;
    }

    // -----------------------
    // Wall Run
    // -----------------------
    private void WallRunTick(float dt)
    {
        if (!enableWallRun) return;
        if (cc.isGrounded) return;
        if (isSliding) return;
        if (isLatched) return;
        if (isDodging) return;

        if (Input.GetAxisRaw("Vertical") <= wallRunMinForwardInput)
        {
            if (isWallRunning)
            {
                wallLostTimer -= dt;
                if (wallLostTimer <= 0f) StopWallRun();
            }
            return;
        }

        bool hasWall = TryGetSideWall(out Vector3 n, out int side);

        if (hasWall)
        {
            wallNormal = n;
            wallSide = side;

            if (playerCamera != null)
            {
                float lookIntoWallDot = Vector3.Dot(playerCamera.transform.forward.normalized, (-wallNormal).normalized);
                if (lookIntoWallDot > wallRunAngleMaxDot)
                {
                    if (isWallRunning)
                    {
                        wallLostTimer -= dt;
                        if (wallLostTimer <= 0f) StopWallRun();
                    }
                    return;
                }
            }

            Vector3 horizVel = new Vector3(velocity.x, 0f, velocity.z);
            if (!isWallRunning)
            {
                if (horizVel.sqrMagnitude > 0.01f)
                {
                    float toward = Vector3.Dot(horizVel.normalized, (-wallNormal).normalized);
                    if (toward < wallApproachMinDot)
                        return;
                }

                StartWallRun();
            }

            wallLostTimer = wallRunStickTime;

            wallAlong = Vector3.Cross(Vector3.up, wallNormal).normalized;
            if (Vector3.Dot(wallAlong, transform.forward) < 0f)
                wallAlong = -wallAlong;

            velocity.y -= gravity * wallRunGravityScale * dt;
            if (velocity.y < -wallRunMaxFallSpeed)
                velocity.y = -wallRunMaxFallSpeed;

            Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
            horiz += (-wallNormal) * (wallAdhesion * dt);
            velocity.x = horiz.x;
            velocity.z = horiz.z;
        }
        else
        {
            if (isWallRunning)
            {
                wallLostTimer -= dt;
                if (wallLostTimer <= 0f) StopWallRun();
            }
        }
    }

    private void StartWallRun()
    {
        isWallRunning = true;
        wallRunTimer = wallRunMaxTime;
        wallLostTimer = wallRunStickTime;

        if (isSliding) StopSlide();
        if (velocity.y < 0f) velocity.y = 0f;
    }

    private void StopWallRun()
    {
        isWallRunning = false;
        wallRunTimer = 0f;
        wallLostTimer = 0f;
    }

    private void WallRunMoveTick(float dt)
    {
        Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
        Vector3 desired = wallAlong * wallRunSpeed;
        horiz = Vector3.MoveTowards(horiz, desired, wallRunAccel * dt);

        velocity.x = horiz.x;
        velocity.z = horiz.z;
    }

    private void WallJump()
    {
        Vector3 away = wallNormal.normalized;
        Vector3 along = wallAlong.normalized;

        StopWallRun();

        Vector3 horiz = new Vector3(velocity.x, 0f, velocity.z);
        horiz += away * wallJumpAwaySpeed;
        horiz += along * wallJumpAlongBoost;

        velocity.x = horiz.x;
        velocity.z = horiz.z;
        velocity.y = wallJumpUpVelocity;

        coyoteTimer = 0f;
        SoundManager.Instance?.PlayOneShot2D(SoundId.WallJump);
    }

    // -----------------------
    // Camera FOV
    // -----------------------
    private void CameraTick(float dt)
    {
        if (playerCamera == null) return;

        bool hasMoveInput = wishDir.sqrMagnitude > 0.01f;

        float target =
            (wantsSprint && hasMoveInput && !isSliding && !isWallRunning && !isLatched && !isDodging) ? sprintFOV : baseFOV;

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, target, fovLerp * dt);
    }

    // -----------------------
    // Head Bob
    // -----------------------
    private void HeadBobTick(float dt)
    {
        if (!enableHeadBob || cameraRoot == null)
        {
            currentBobOffset = Mathf.Lerp(currentBobOffset, 0f, bobLerpSpeed * dt);
            return;
        }

        Vector3 horizVel = new Vector3(velocity.x, 0f, velocity.z);
        float speed = horizVel.magnitude;

        bool groundedMoving = cc.isGrounded && speed > 0.1f && !isSliding && !isLatched && !isDodging;

        if (!groundedMoving)
        {
            bobTime = 0f;
            currentBobOffset = Mathf.Lerp(currentBobOffset, 0f, bobLerpSpeed * dt);
            return;
        }

        bool sprintingNow = wantsSprint && wishDir.sqrMagnitude > 0.01f;

        float targetFrequency = sprintingNow ? sprintBobFrequency : bobFrequency;
        float targetAmplitude = sprintingNow ? sprintBobAmplitude : bobAmplitude;

        bobTime += dt * targetFrequency;

        float bob = Mathf.Sin(bobTime) * targetAmplitude;
        currentBobOffset = Mathf.Lerp(currentBobOffset, bob, bobLerpSpeed * dt);
    }

    private void PlayerLoopAudioTick()
    {
        if (SoundManager.Instance == null)
            return;

        bool groundedMoving =
            cc.isGrounded &&
            CurrentHorizontalSpeed > 0.1f &&
            !isSliding &&
            !isLatched &&
            !isDodging &&
            !isWallRunning;

        if (isWallRunning)
        {
            SoundManager.Instance.StartLoop2D(SoundId.WallRun);
            return;
        }

        if (groundedMoving)
        {
            SoundManager.Instance.StartLoop2D(wantsSprint ? SoundId.Sprint : SoundId.Walk);
        }
        else
        {
            SoundManager.Instance.StopAllPlayerLoops();
        }
    }

    // -----------------------
    // Helpers
    // -----------------------
    private void GetPlanarBasis(out Vector3 fwd, out Vector3 right)
    {
        fwd = playerCamera != null ? playerCamera.transform.forward : transform.forward;
        right = playerCamera != null ? playerCamera.transform.right : transform.right;

        fwd.y = 0f;
        right.y = 0f;

        if (fwd.sqrMagnitude > 0.0001f) fwd.Normalize();
        if (right.sqrMagnitude > 0.0001f) right.Normalize();
    }

    // -----------------------
    // Wall detection
    // -----------------------
    private bool TryGetSideWall(out Vector3 normal, out int side)
    {
        normal = Vector3.zero;
        side = 0;

        Vector3 origin = transform.position + Vector3.up * wallCheckHeight;

        if (Physics.Raycast(origin, transform.right, out RaycastHit hitR, wallCheckDistance, parkourMask, QueryTriggerInteraction.Ignore))
        {
            normal = hitR.normal;
            side = +1;
            return true;
        }

        if (Physics.Raycast(origin, -transform.right, out RaycastHit hitL, wallCheckDistance, parkourMask, QueryTriggerInteraction.Ignore))
        {
            normal = hitL.normal;
            side = -1;
            return true;
        }

        return false;
    }

    private bool TryGetFrontWall(out Vector3 normal)
    {
        normal = Vector3.zero;

        Vector3 origin = transform.position + Vector3.up * latchCheckHeight;
        Vector3 dir = (playerCamera != null) ? playerCamera.transform.forward : transform.forward;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, latchCheckDistance, parkourMask, QueryTriggerInteraction.Ignore))
        {
            normal = hit.normal;
            return true;
        }

        return false;
    }

    // -----------------------
    // Gizmos
    // -----------------------
    private void OnDrawGizmos()
    {
        if (drawGizmosWhenNotSelected)
            DrawMovementRaycastGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmosWhenNotSelected)
            DrawMovementRaycastGizmos();
    }

    private void DrawMovementRaycastGizmos()
    {
        if (!drawRaycastGizmos)
            return;

        Vector3 right = transform.right;

        Vector3 frontDir;
        if (playerCamera != null)
            frontDir = playerCamera.transform.forward;
        else
            frontDir = transform.forward;

        if (frontDir.sqrMagnitude > 0.0001f)
            frontDir.Normalize();

        Vector3 sideOrigin = transform.position + Vector3.up * wallCheckHeight;
        Vector3 frontOrigin = transform.position + Vector3.up * latchCheckHeight;

        DrawRayWithHitMarker(sideOrigin, right, wallCheckDistance, Color.cyan);
        DrawRayWithHitMarker(sideOrigin, -right, wallCheckDistance, new Color(1f, 0f, 1f, 1f));
        DrawRayWithHitMarker(frontOrigin, frontDir, latchCheckDistance, Color.yellow);

        if (Application.isPlaying && isLatched)
        {
            Vector3 towardWall = -latchNormal.normalized;
            Vector3 latchOrigin = transform.position + Vector3.up * latchCheckHeight + (latchNormal.normalized * 0.25f);
            float dist = latchCheckDistance + 0.35f;

            DrawRayWithHitMarker(latchOrigin, towardWall, dist, Color.green);
        }
    }

    private void DrawRayWithHitMarker(Vector3 origin, Vector3 dir, float distance, Color rayColor)
    {
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        dir.Normalize();

        Gizmos.color = rayColor;
        Gizmos.DrawSphere(origin, gizmoSphereSize);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, distance, parkourMask, QueryTriggerInteraction.Ignore))
        {
            Gizmos.DrawLine(origin, hit.point);
            Gizmos.DrawSphere(hit.point, gizmoHitMarkerSize);
            Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.25f);
        }
        else
        {
            Gizmos.DrawRay(origin, dir * distance);
        }
    }
}