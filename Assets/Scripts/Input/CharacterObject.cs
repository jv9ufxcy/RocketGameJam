using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Experimental.XR;
using System;

public class CharacterObject : MonoBehaviour
{
    [Header("Movement")]
    public Vector2 velocity;

    public float gravity = -0.01f, gravityMin = -17f;
    public float aniMoveSpeed;

    public Vector3 friction = new Vector3(0.95f, 0.99f, 0.95f);
    [SerializeField] private float direction = 1;

    public Rigidbody2D myRB;
    [HideInInspector] public BoxCollider2D boxCollider2D;
    [HideInInspector] public Controller2D controller;
    [HideInInspector] public HealthManager healthManager;
    [HideInInspector] public AudioManager audioManager;

    [Header("CurrentState")]
    public int currentState;
    public float currentStateTime;
    public float prevStateTime;

    [Header("CharacterModel")]
    public CharacterObject characterObject;
    public GameObject character;
    public GameObject draw;
    public Animator characterAnim;
    public RuntimeAnimatorController[] formAnims;
    public SpriteRenderer spriteRend;
    public Material defaultMat, whiteMat;
    private Color flashColor = new Color ( 0,0.5f,0.75f,1f);
    public enum ControlType { AI, PLAYER, BOSS, DEAD, OBJECT };
    public ControlType controlType;

    [Header("HitCancel")]
    public Hitbox hitbox;
    public bool canCancel;
    public bool isHit;
    public int hitConfirm;

    public InputBuffer inputBuffer = new InputBuffer();

    // Use this for initialization
    void Awake()
    {
        myRB = GetComponent<Rigidbody2D>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        controller = GetComponent<Controller2D>();
        spriteRend = characterAnim.gameObject.GetComponent<SpriteRenderer>();
        healthManager = GetComponent<HealthManager>();
    }
    void Start()
    {
        defaultMat = spriteRend.material;

        audioManager = AudioManager.instance;
        if (audioManager == null)
        {
            Debug.LogError("No Audio Manager in Scene");
        }
    }

    // Update is called once per frame
    private void Update()
    {
        switch (controlType)
        {
            case ControlType.AI:
                isNearPlayer = Vector3.Distance(transform.position, GameEngine.gameEngine.mainCharacter.transform.position) <= aggroRange;
                isLongRange = (Mathf.Abs(transform.position.x - GameEngine.gameEngine.mainCharacter.transform.position.x) <= longAttackRange &&
                    (Mathf.Abs(transform.position.x - GameEngine.gameEngine.mainCharacter.transform.position.x)) > shortAttackRange);
                isShortRange = (Mathf.Abs(transform.position.x - GameEngine.gameEngine.mainCharacter.transform.position.x) <= shortAttackRange);
                break;
            case ControlType.BOSS:
                isNearPlayer = Vector3.Distance(transform.position, GameEngine.gameEngine.mainCharacter.transform.position) <= aggroRange;
                isLongRange = (Mathf.Abs(transform.position.x - GameEngine.gameEngine.mainCharacter.transform.position.x) <= longAttackRange &&
                    (Mathf.Abs(transform.position.x - GameEngine.gameEngine.mainCharacter.transform.position.x)) > shortAttackRange);
                isShortRange = (Mathf.Abs(transform.position.x - GameEngine.gameEngine.mainCharacter.transform.position.x) <= shortAttackRange);
                break;
            case ControlType.PLAYER:
                PauseMenu();
                if (!PauseManager.IsGamePaused && !DialogueManager.instance.isDialogueActive)
                {
                    JumpCut();
                    //DashCut();
                    ChargeAttack();
                    leftStick = new Vector2(Input.GetAxis(GameEngine.coreData.rawInputs[13].name), Input.GetAxis(GameEngine.coreData.rawInputs[14].name));
                }
                break;
            default:
                break;
        }
    }
    void FixedUpdate()
    {
        if (!PauseManager.IsGamePaused && !DialogueManager.instance.isDialogueActive)
        {
            if (GameEngine.hitStop <= 0)
            {
                //UpdateInputBuffer

                //Update Input
                //HitCancel();
                switch (controlType)
                {
                    case ControlType.AI:
                        UpdateAI();
                        break;
                    case ControlType.BOSS:
                        UpdateAI();
                        break;
                    case ControlType.PLAYER:
                        UpdateInput();
                        break;
                    default:
                        break;
                }
                UpdateState();
                UpdatePhysics();
            }
            UpdateTimers();
        }
            UpdateAnimator();
    }
    public void UpdateCharacter()
    {
        UpdateState();
        UpdatePhysics();
        UpdateTimers();
        UpdateAnimator();
    }
    void UpdateTimers()
    {
        if (dashCooldown > 0) { dashCooldown -= dashCooldownRate; }
        if (invulCooldown > 0) { invulCooldown --; }
        else { isInvulnerable = false; curComboValue = -1; }
    }

    [HideInInspector] public float animSpeed;
    void UpdateAnimator()
    {
        animSpeed = 1;
        if (GameEngine.hitStop > 0)
        {
            animSpeed = 0;
        }

        Vector2 latSpeed = new Vector2(velocity.x, 0);
        aniMoveSpeed = Vector3.SqrMagnitude(latSpeed);
        animFallSpeed = velocity.y /** 30f*/;
        characterAnim.SetFloat("moveSpeed", aniMoveSpeed);
        characterAnim.SetFloat("aerialState", animAerialState);
        characterAnim.SetFloat("fallSpeed", animFallSpeed);
        characterAnim.SetFloat("hitAnimX", curHitAnim.x);
        characterAnim.SetFloat("hitAnimY", curHitAnim.y);
        characterAnim.SetFloat("animSpeed", animSpeed);

    }
    void FaceStick()
    {
        if (CheckVelocityDeadZone())
        {
            if (leftStick.x > 0) { direction = 1; transform.localScale = new Vector3(1f, 1f, 1f); }
            else if (leftStick.x < 0) { direction = -1; transform.localScale = new Vector3(-1f, 1f, 1f); }
        }
    }
    void UpdateState()
    {
        CharacterState myCurrentState = GameEngine.coreData.characterStates[currentState];

        if (hitStun > 0 && controlType!=ControlType.DEAD)
        {
            GettingHit();
        }
        else
        {

            UpdateStateEvents();
            UpdateStateAttacks();

            prevStateTime = currentStateTime;
            currentStateTime++;


            if (currentStateTime >= myCurrentState.length)
            {
                if (myCurrentState.loop) { LoopState(); }
                else { EndState(); }
            }
        }

    }
    void LoopState()
    {
        currentStateTime = 0;

        prevStateTime = -1;
    }

    void EndState()
    {
        currentStateTime = 0;
        currentState = 0;
        prevStateTime = -1;
        StartState(currentState);
    }

    void UpdateStateEvents()
    {
        int _curEv = 0;
        foreach (StateEvent _ev in GameEngine.coreData.characterStates[currentState].events)
        {
            if (_ev.active)
            {
                if (currentStateTime >= _ev.start && currentStateTime <= _ev.end)
                {
                    DoEventScript(_ev.script, currentState, _curEv, _ev.parameters);
                }
            }
            _curEv++;
        }
    }
    [Header("CurrentAttack")]
    public float hitActive;
    public int currentAttackIndex;
    void UpdateStateAttacks()
    {
        int _cur = 0;
        foreach (AttackEvent _atk in GameEngine.coreData.characterStates[currentState].attacks)
        {
            if (currentStateTime == _atk.start)
            {
                hitbox.RestoreGetHitBools();
                hitActive = _atk.length;
                hitbox.transform.localScale = _atk.hitBoxScale;
                hitbox.transform.localPosition = _atk.hitBoxPos;
                currentAttackIndex = _cur;
            }
            if (currentStateTime == _atk.start + _atk.length)
            {
                hitActive = 0;
            }
            //HitCancel
            float cWindow = _atk.start + _atk.cancelWindow;
            if (currentStateTime >= cWindow)
                if (hitConfirm > 0)
                    canCancel = true;

            //Whiff Cancel
            //if (currentStateTime >= cWindow + whiffWindow)
            //    canCancel = true;

            _cur++;
        }
    }
    public static float whiffWindow = 8f;
    void HitCancel()
    {
        float cWindow = GameEngine.coreData.characterStates[currentState].attacks[currentAttackIndex].start +
            GameEngine.coreData.characterStates[currentState].attacks[currentAttackIndex].cancelWindow;

        if (currentStateTime == cWindow)
            if (hitConfirm > 0)
                canCancel = true;

        if (currentStateTime == cWindow + whiffWindow)
            canCancel = true;
    }
    void DoEventScript(int _index, int _actIndex, int _evIndex, List<ScriptParameters> _params)
    {
        if (_params == null) { return; }
        if (_params.Count <= 0) { return; }
        switch (_index)//index = element in characterscripts
        {
            case 0://Jump
                VelocityY(_params[0].val);
                break;
            case 1:
                FrontVelocity(_params[0].val);
                break;
            case 3:
                StickMove(_params[0].val);
                break;
            case 4:
                GettingHit();
                break;
            case 5:
                GlobalPrefab(_params[10].val, _actIndex, _evIndex);
                break;
            case 6:
                CanCancel(_params[0].val);
                break;
            case 7:
                Jump(_params[0].val);
                break;
            case 8:
                FaceStick();
                break;
            case 9:
                AirMove(_params[0].val);
                break;
            case 10:
                FireBullet(_params[0].val, _params[1].val, _params[2].val, _params[3].val, _params[4].val, _params[5].val);
                break;
            case 11:
                //Dash(_params[0].val);
                break;
            case 12:
                //KinzectorActions(_params[0].val, _params[1].val, _params[2].val);
                break;
            case 13:
                AirStill(_params[0].val);
                break;
            case 14:
                PlayAudio(_params[0].name);
                break;
            case 15:
                //SpawnTurret(_params[0].val);
                break;
            case 16:
                FireBullet(_params[0].val, _params[1].val, _params[2].val, _params[3].val, _params[4].val, _params[5].val);
                break;
            case 17:
                QuickChangeForm((int)_params[0].val);
                break;

        }
    }
    public void SetState(int stateIndex)
    {
        StartState(stateIndex);
    }
    public void DOChangeMovelist(int index)
    {
        PlayFlashParticle(henshinColors[index]);
        GameEngine.SetHitPause(5f);
        QuickChangeForm(index);
    }

    public void QuickChangeForm(int index)
    {
        GameEngine.gameEngine.ChangeMovelist(index);
        characterAnim.runtimeAnimatorController = formAnims[GameEngine.gameEngine.globalMovelistIndex];
    }

    public void ToggleMovelist()
    {
        GameEngine.SetHitPause(5f);
        GameEngine.gameEngine.ToggleMovelist();
        PlayFlashParticle(henshinColors[GameEngine.gameEngine.globalMovelistIndex]);
        characterAnim.runtimeAnimatorController = formAnims[GameEngine.gameEngine.globalMovelistIndex];

        if (GameEngine.gameEngine.globalMovelistIndex == 1)//bomb
            SetCrouchFlag(true);
        else
            SetCrouchFlag(false);
    }
    private void PlayAudio(string audioName)
    {
        audioManager.PlaySound(audioName);
    }
    
    private void AirStill(float _pow)
    {
        if (IsGrounded())
        {
            if (_pow > 0)
            {
                VelocityY(_pow);
            }
        }
        else
            VelocityY(2f);
    }

    public void Jump(float _pow)
    {
        velocity.y = _pow*jumpPow;
        jumps--;
        StopSpinning();
        landingParticle.Play();
    }
    void CanCancel(float _val)
    {
        if (_val > 0)
        {
            canCancel = true;
        }
        else
            canCancel = false;
    }
    void GlobalPrefab(float _index, int _act, int _ev)
    {
        GameEngine.GlobalPrefab((int)_index, character, _act, _ev);
    }
    public void GlobalPrefab(float _prefab)
    {
        GlobalPrefab(_prefab, -1, -1);
    }
    public void FrontVelocity(float _pow)
    {
        velocity.x = _pow * direction;
    }
    [Header("MovementVectors")]
    public Vector2 leftStick;
    void StickMove(float _pow)
    {
        if ((leftStick.x > deadzone || leftStick.x < -deadzone || leftStick.y > deadzone || leftStick.y < -deadzone))
        {
            velocity.x += (leftStick.x * moveSpeed) * _pow;
        }
        if (hitStun <= 0)
        {
            FaceStick();
        }
    }
    void AirMove(float _pow)
    {
        if (!IsGrounded())
        {
            if ((leftStick.x > deadzone || leftStick.x < -deadzone || leftStick.y > deadzone || leftStick.y < -deadzone))
            {
                float _mov = 0;
                if (leftStick.x > deadzone)
                {
                    _mov = 1;
                }
                if (leftStick.x < -deadzone)
                {
                    _mov = -1;
                }
                velocity.x = (airMod * _mov) * moveSpeed * _pow;
            }
        }
        if (hitStun <= 0)
        {
            FaceStick();
        }
    }
    void VelocityY(float _pow)
    {
        velocity.y = _pow;
    }

    public float deadzone = 0.2f;

    public float moveSpeed = 10f;
    public float airMod = 1f;
    public float jumpPow = 12;
    public void StartStateFromScript(int _newState)
    {
        StartState(_newState);
    }
    void StartState(int _newState)
    {
        currentState = _newState;
        prevStateTime = -1;
        currentStateTime = 0;
        canCancel = false;

        if (_newState == 0) { currentCommandStep = 0; }

        //Attacks
        hitActive = 0;
        hitConfirm = 0;

        UseMeter(nextSpecialMeterUse);
        nextSpecialMeterUse = 0;

        SetAnimation(GameEngine.coreData.characterStates[currentState].stateName);
        //Debug.Log("State Started: " + GameEngine.coreData.characterStates[currentState].stateName);
    }
    void SetAnimation(string animName)
    {
        characterAnim.CrossFadeInFixedTime(animName, GameEngine.coreData.characterStates[currentState].blendRate);
        //characterAnim.Play(animName);
    }

    public int currentCommandState;
    public int currentCommandStep;

    public void GetCommandState()
    {
        currentCommandState = 0;
        for (int c = 0; c < GameEngine.gameEngine.CurrentMoveList().commandStates.Count; c++)
        {
            CommandState s = GameEngine.gameEngine.CurrentMoveList().commandStates[c];
            if (s.aerial == aerialFlag)
            {
                currentCommandState = c;
                return;
            }
            //if (s.wall == wallFlag)
            //{
            //    currentCommandState = c;
            //    return;
            //}
        }
    }

    int[] cancelStepList = new int[2];

    void UpdateInput()
    {


        inputBuffer.Update();

        bool startState = false;

        GetCommandState();
        CommandState comState = GameEngine.gameEngine.CurrentMoveList().commandStates[currentCommandState];


        if (currentCommandStep >= comState.commandSteps.Count) { currentCommandStep = 0; }


        cancelStepList[0] = currentCommandStep;//base sub-state
        cancelStepList[1] = 0;
        int finalS = -1;
        int finalF = -1;
        int currentPriority = -1;
        for (int s = 0; s < cancelStepList.Length; s++)
        {
            if (comState.commandSteps[currentCommandStep].strict && s > 0) { break; }
            if (!comState.commandSteps[currentCommandStep].activated) { break; }

            for (int f = 0; f < comState.commandSteps[cancelStepList[s]].followUps.Count; f++)// (CommandStep cStep in comState.commandSteps[currentCommandStep])
            {
                CommandStep nextStep = comState.commandSteps[comState.commandSteps[cancelStepList[s]].followUps[f]];
                InputCommand nextCommand = nextStep.command;

                //if(inputBuffer.)
                if (CheckInputCommand(nextCommand))
                {
                    if (canCancel)
                    {
                        if (GameEngine.coreData.characterStates[nextCommand.state].ConditionsMet(this))
                        {
                            if (nextStep.priority > currentPriority)
                            {
                                currentPriority = nextStep.priority;
                                startState = true;
                                finalS = s;
                                finalF = f;

                            }
                        }
                    }
                }
            }
        }
        if (startState)
        {
            CommandStep nextStep = comState.commandSteps[comState.commandSteps[cancelStepList[finalS]].followUps[finalF]];
            InputCommand nextCommand = nextStep.command;
            inputBuffer.UseInput(nextCommand.input);
            if (nextStep.followUps.Count > 0) { currentCommandStep = nextStep.idIndex; }
            else { currentCommandStep = 0; }
            StartState(nextCommand.state);
        }
    }



    public bool CheckInputCommand(InputCommand _in)
    {
        if (inputBuffer.buttonCommandCheck[_in.input] < 0) { return false; }
        if (inputBuffer.motionCommandCheck[_in.motionCommand] < 0) { return false; }
        return true;
    }
    public bool CheckVelocityDeadZone()
    {
        if (velocity.x > 0.001f) { return true; }
        if (velocity.x < -0.001f) { return true; }
        if (velocity.y > 0.001f) { return true; }
        if (velocity.y < -0.001f) { return true; }
        return false;
    }
    [Space]
    [Header("Charged Shot")]

    private float shotPressure;
    [SerializeField] private float minShotPressure = 30f, strongShotPressure = 60f, criticalFrameWindow = 8f, pressureLimit = 100f;
    public int weakShotIndex = 5,chargeShotIndex = 6, critBusterIndex = 7, chargeInputIndex = 5;
    [SerializeField] private bool firstCharge, secondCharge, thirdCharge;
    private Color c;

    public float chargeIncrement = 1f;
    void ChargeAttack()
    {
        if (Input.GetButton(GameEngine.coreData.rawInputs[chargeInputIndex].name) && hasReleasedShot)//name of charge Attack
        {
            ColorCharge();
            ChargeUp(chargeIncrement);
        }
        if (Input.GetButtonUp(GameEngine.coreData.rawInputs[chargeInputIndex].name))
        {
            hasReleasedShot = true;
            FireCharge();
        }
        if (shotPressure >= pressureLimit)
        {
            hasReleasedShot = false;
            FireCharge();
        }
    }

    private void FireCharge()
    {
        float frameWindow = pressureLimit - criticalFrameWindow;

        if (shotPressure >= minShotPressure && shotPressure < strongShotPressure)
        {
            WeakRocketFire();
        }
        else if (shotPressure >= strongShotPressure && shotPressure < frameWindow || shotPressure >= pressureLimit)
        {
            StrongRocketFire();
        }
        else if (shotPressure >= frameWindow&&shotPressure<pressureLimit)
        {
            CriticalRocketFire();
        }

        shotPressure = 0f;
        firstCharge = false; secondCharge = false; thirdCharge = false;

        foreach (ParticleSystem p in primaryGunParticles)
        {
            var pmain = p.main;
            pmain.startColor = Color.clear;
            p.Stop();
        }
        //shotPressure = 0;
    }

    [Space]
    [Header("Charged Buster")]

    public bool hasReleasedShot = true;

    [Header("ParticleGroups")]
    public Transform gunChargeParticles;
    public Transform flashParticles;
    public Color[] turboColors;
    public Color[] henshinColors;
    public List<ParticleSystem> primaryGunParticles = new List<ParticleSystem>();
    public List<ParticleSystem> secondaryParticles = new List<ParticleSystem>();
    public ParticleSystem bombDashParticle, landingParticle;
    [Space]
    [Header("Shooting Stats")]
    public float shootAnim, shootAnimMax;
    public float fireRate = 10f;
    private float timeToNextFire = 0f;
    public GameObject[] bullets;
    [SerializeField] private Vector2 bulletSpawnPos = new Vector2(0.5f, 1f);


    public void FireBullet(float bulletType, float bulletSpeed, float offsetX, float offsetY, float attackIndex, float bulletRot)
    {
        var offset = new Vector3(offsetX * direction, offsetY, 0);
        GameObject newbullet = Instantiate(bullets[(int)bulletType], transform.position + offset, Quaternion.identity);
        BulletHit bullet = newbullet.GetComponent<BulletHit>();
        shootAnim = shootAnimMax;
        bullet.character = characterObject;
        bullet.attackIndex = (int)attackIndex;
        bullet.direction.x = direction;
        bullet.speed = bulletSpeed;

        switch (controlType)
        {
            case ControlType.AI:
                bullet.velocity.x = direction;
                bullet.attackIndex = (int)attackIndex;
                bullet.rotation = bulletRot * direction;
                newbullet.transform.localScale = new Vector3(direction, 1, 1);
                break;
            case ControlType.PLAYER:
                bullet.velocity.y = 1;
                bullet.rotation = PlayerShooting.instance.transform.eulerAngles.z;
                newbullet.transform.position = PlayerShooting.instance.bulletMuzzle.transform.position;
                break;
            default:
                break;
        }
    }
    public void WeakRocketFire()
    {
        StartState(weakShotIndex);
    }
    public void StrongRocketFire()
    {
        StartState(chargeShotIndex);
    }
    public void CriticalRocketFire()
    {
        StartState(critBusterIndex);
    }
    public void ColorCharge()
    {
        if (!firstCharge)
            c = Color.clear;

        if ((shotPressure >= minShotPressure && shotPressure < strongShotPressure) && !firstCharge)
        {
            foreach (ParticleSystem p in primaryGunParticles)
            {
                p.startColor = Color.clear;
                p.Play();
            }
            firstCharge = true;
            c = turboColors[0];

            PlayFlashParticle(c);
        }

        if (shotPressure >= strongShotPressure && !secondCharge)
        {
            secondCharge = true;
            c = turboColors[1];

            PlayFlashParticle(c);
        }

        float frameWindow = pressureLimit - criticalFrameWindow;
        if (shotPressure>=frameWindow && !thirdCharge)
        {
            thirdCharge = true;
            c = turboColors[2];
            PlayFlashParticle(c);
        }

        foreach (ParticleSystem p in primaryGunParticles)
        {
            var pmain = p.main;
            pmain.startColor = c;
        }
        foreach (ParticleSystem p in secondaryParticles)
        {
            var pmain = p.main;
            pmain.startColor = c;
        }
    }
    void PlayFlashParticle(Color c)
    {
        foreach (ParticleSystem p in secondaryParticles)
        {
            var pmain = p.main;
            pmain.startColor = c;
            p.Play();
        }
    }
    public float minSpinSpeed = 20f;
    public int spinStateIndex = 8;
    public bool isSpinning = false;
    public float newSpeed;

    public void HitByRocket(Vector2 expVel, float explosionForce)
    {
        AttackEvent curAtk = GameEngine.coreData.characterStates[weakShotIndex].attacks[0];
        if (CanBeHit(curAtk))
        {
            healthManager.RemoveHealth(2);
            PlayAudio("Hurt");
            StartInvul(explosionForce / 10);
        }

        StartState(spinStateIndex);
        isSpinning = true;
        newSpeed = explosionForce / 2;
        velocity += expVel;
        bombDashParticle.Play();
        GameEngine.SetHitPause(explosionForce / 10);
    }
    private void OnCollisionStay2D(Collision2D coll)
    {
        var speed = velocity.magnitude;
        var dir = Vector3.Reflect(velocity.normalized, coll.contacts[0].normal);

        if (speed > minSpinSpeed && isSpinning)
        {
            velocity = dir * Mathf.Max(Mathf.Round(newSpeed), 0f);
            velocity.y += newSpeed / 2;
            Debug.Log("Speed = " + Mathf.Round(speed));
        }
        
        //if (/*currentState==spinStateIndex&&*/coll.gameObject.layer==whatCountsAsGround)
        //{
        //    //if (speed > minSpinSpeed)
        //    //{
        //        //velocity = dir * Mathf.Max(speed, 0f);
        //    //}
        //    //else
        //    //    StartState(0);
        //    //if (IsGrounded())
        //    //{
        //    //    StartState(1);
        //    //}
        //}
    }
    private void PauseMenu()
    {
        if (Input.GetButtonDown(GameEngine.coreData.rawInputs[8].name))
            PauseManager.pauseManager.PauseButtonPressed();
    }
    private static void Screenshake()
    {
        Camera.main.transform.GetComponent<CinemachineImpulseSource>().GenerateImpulse();
    }
    void JumpCut()
    {
        //if (currentState==1)//jump state
        if (aerialFlag)//jump state
        {
            if (velocity.y > 0 && Input.GetButtonUp(GameEngine.coreData.rawInputs[0].name))
            {
                VelocityY(-2);
                if (IsGrounded())
                    StartState(0);
            }
        }
    }
    [SerializeField] private int dashInput = 4;
    public Collider2D headColl;
    public bool crouchFlag;

    private void SetCrouchFlag(bool crouch)
    {
        crouchFlag = crouch;
        headColl.enabled = !crouchFlag;
    }
    public bool CanUnCrouch()
    {
        return !Physics2D.OverlapCircle(transform.position + Vector3.up, 0.5f, whatCountsAsGround);
    }
    [SerializeField]
    private float fadeTime = 0.5f, shortDashInterval = 0.05f, dashLength = 0.45f;
    [SerializeField]
    private Transform afterImageParent;
    public void ShowAfterImage()
    {
        Sequence s = DOTween.Sequence();

        for (int i = 0; i < afterImageParent.childCount; i++)
        {
            Transform currentGhost = afterImageParent.GetChild(i);
            s.AppendCallback(() => currentGhost.position = draw.transform.position);
            s.AppendCallback(() => currentGhost.GetComponent<SpriteRenderer>().flipX = direction != 1);
            s.AppendCallback(() => currentGhost.GetComponent<SpriteRenderer>().sprite = spriteRend.sprite);
            s.Append(currentGhost.GetComponent<SpriteRenderer>().material.DOColor(henshinColors[GameEngine.gameEngine.globalMovelistIndex], 0));
            s.AppendCallback(() => FadeSprite(currentGhost, henshinColors[6]));
            s.AppendInterval(shortDashInterval);
        }
    }
    public void FadeSprite(Transform current, Color fadeColor)
    {
        current.GetComponent<SpriteRenderer>().material.DOKill();
        current.GetComponent<SpriteRenderer>().material.DOColor(fadeColor, fadeTime);
    }
    void ChargeUp(float _val)
    {
        shotPressure += _val;
    }
    [Header("Grounded Check")]
    [SerializeField]
    private LayerMask whatCountsAsGround;
    public bool aerialFlag, wallFlag, isOnGround, isOnWall;
    [SerializeField] private float aerialTimer, groundDetectHeight, wallDetectWidth, animAerialState, animFallSpeed;

    public int jumps, jumpMax = 1;
    public int dashes, dashMax = 1;
    public void SpendDash(int dash)
    {
        dashes -= dash;
        Mathf.Clamp(dashes, 0, dashMax);
    }

    [Header("Timers")]
    public float coyoteTimer = 3f;
    public float dashCooldown, dashCooldownRate = 1f, invulCooldown, invulFlickerRate = 4f;
    public float specialMeter, specialMeterMax = 100f, nextSpecialMeterUse;

    public void UseMeter(float _val)
    {
        ChangeMeter(-_val);
        //Debug.Log("Meter Spent");
    }
    public void BuildMeter(float _val)
    {
        ChangeMeter(_val);
    }
    public void ChangeMeter(float _val)
    {
        specialMeter += _val;
        specialMeter = Mathf.Clamp(specialMeter, 0f, specialMeterMax);
        healthManager.ChangeMeter((int)_val);
    }
    public float wallSlideSpeed = -1.7f;
    void UpdatePhysics()
    {
        if (IsGrounded())
        {
            aerialFlag = false;
            //wallFlag = false;
            aerialTimer = 0;
            animAerialState = 0f;
            jumps = jumpMax;
            dashes = dashMax;
            //velocity.y = 0;
            GroundTouch();

            if (isSpinning)
            {
                StartState(1);
                StopSpinning();
            }
        }
        else
        {
            if (!aerialFlag)
            {
                aerialTimer++;
            }
            if (aerialTimer >= coyoteTimer)//coyote time
            {
                aerialFlag = true;
                if (animAerialState <= 1f)
                {
                    animAerialState += 0.1f;
                }
                if (jumps == jumpMax)
                {
                    jumps--;
                }
                //if (IsOnWall())//wall
                //{
                //    wallFlag = true;
                //    if (leftStick.x == direction && velocity.y < 0)
                //    {
                //        if (currentState != spinStateIndex)
                //        {
                //            //velocity.y = wallSlideSpeed;
                //            animAerialState = -.01f;
                //        }
                //    }
                //}
                //else
                //{
                //if (controller.collisions.above || controller.collisions.below)
                //    velocity.y = 0;
                //else
                //{
                    velocity.y += gravity;
                    Mathf.Clamp(velocity.y, gravityMin, 0);
                    hasLanded = false;
                //}
                wallFlag = IsOnWall();

                //}
            }

        }
        Move(velocity);
        velocity.Scale(friction);
    }

    private void StopSpinning()
    {
        bombDashParticle.Stop();
        isSpinning = false;
    }

    public void Move(Vector2 velocity)
    {
        //myRB.velocity = velocity;
        controller.Move(velocity * Time.fixedDeltaTime, leftStick);
    }
    public bool HitCeiling()
    {
        return controller.collisions.above;
    }

    public bool IsGrounded()
    {
        RaycastHit2D rayCastHit = Physics2D.BoxCast(boxCollider2D.bounds.center, boxCollider2D.bounds.size, 0f, Vector2.down, groundDetectHeight, whatCountsAsGround);
        Color rayColor;
        if (rayCastHit.collider != null)
            rayColor = Color.green;
        else
            rayColor = Color.red;
        Debug.DrawRay(boxCollider2D.bounds.center + new Vector3(boxCollider2D.bounds.extents.x, 0), Vector2.down * (boxCollider2D.bounds.extents.y + groundDetectHeight), rayColor);
        Debug.DrawRay(boxCollider2D.bounds.center - new Vector3(boxCollider2D.bounds.extents.x, 0), Vector2.down * (boxCollider2D.bounds.extents.y + groundDetectHeight), rayColor);
        Debug.DrawRay(boxCollider2D.bounds.center - new Vector3(boxCollider2D.bounds.extents.x, boxCollider2D.bounds.extents.y + groundDetectHeight), Vector2.right * (boxCollider2D.bounds.extents.x), rayColor);

        return rayCastHit.collider != null;

        //return controller.collisions.below;
    }
    public bool IsOnWall()
    {
        return controller.collisions.left || controller.collisions.right;
    }
    private bool hasLanded;
    private void GroundTouch()
    {
        if (!hasLanded)
        {
            airMod = 1f;
            animFallSpeed = 0f;
            hasLanded = true;
            landingParticle.Play();
        }
    }
    public void SetVelocity(Vector3 v)
    {
        velocity = v;
    }
    [Header("Hit Stun")]
    public Vector2 curHitAnim;
    public Vector2 targetHitAnim;
    private int curComboValue;

    public bool CanBeHit(AttackEvent curAtk)
    {
        if (controlType == ControlType.DEAD)
            return false;
        if (invulCooldown > 0)
        {
            if (curComboValue < curAtk.comboValue)
                return true;
            else
                return false;
        }
        else
        {
            isInvulnerable = false;
            spriteRend.color = Color.white;
            return true;
        }
    }
    
    public void GetHit(CharacterObject attacker, int projectileIndex, int atkIndex)
    {
        AttackEvent curAtk;
        if (projectileIndex == 0)//not a projectile
        {
            curAtk = GameEngine.coreData.characterStates[attacker.currentState].attacks[attacker.currentAttackIndex];
        }
        else//projectiles
        {
            curAtk = GameEngine.coreData.characterStates[projectileIndex].attacks[atkIndex];
        }

        if (canDefend && IsDefendingInState() && curAtk.poiseDamage < 20f)
        {
            //parry sound
            StartState(defStateIndex);
            dashCooldown = 0;
            FaceTarget(target.transform.position);
            if (projectileIndex == 0) attacker.FrontVelocity(-10f);
        }
        else
        {
            if (healthManager.HasShield())
            {
                healthManager.ShieldDamage(curAtk.poiseDamage);
                GameEngine.SetHitPause(curAtk.hitStop);
                attacker.hitConfirm += 1;
            }
            else
            {
                if (CanBeHit(curAtk))
                {
                    Vector3 nextKnockback = curAtk.knockback;
                    Vector3 knockOrientation = transform.position - attacker.transform.position;
                    knockOrientation.Normalize();
                    nextKnockback.x *= knockOrientation.x;
                    curComboValue = curAtk.comboValue;
                    StartInvul(curAtk.hitStop);

                    healthManager.PoiseDamage(curAtk.poiseDamage);
                    if (healthManager.currentPoise <= 0)
                    {
                        SetVelocity(nextKnockback * 0.7f);//dampen a bit
                        targetHitAnim.x = curAtk.hitAnim.x;
                        targetHitAnim.y = curAtk.hitAnim.y;

                        //curHitAnim.x = UnityEngine.Random.Range(-1f, 1f);//randomized for fun
                        //curHitAnim.y = UnityEngine.Random.Range(-1f, 1f);
                        curHitAnim = targetHitAnim * .25f;

                        hitStun = curAtk.hitStun;
                        StartState(hitStunStateIndex);
                    }

                    GameEngine.SetHitPause(curAtk.hitStop);

                    attacker.hitConfirm += 1;
                    attacker.BuildMeter(curAtk.meterGain);
                    switch (controlType)//damage calc
                    {
                        case ControlType.AI:
                            healthManager.RemoveHealth(curAtk.damage);
                            PlayAudio(attackStrings[curAtk.attackType]);
                            GlobalPrefab(curAtk.attackType);
                            break;
                        case ControlType.OBJECT:
                            healthManager.RemoveHealth(curAtk.damage);
                            PlayAudio(attackStrings[curAtk.attackType]);
                            GlobalPrefab(curAtk.attackType);
                            break;
                        case ControlType.BOSS:
                            healthManager.RemoveHealth(curAtk.damage);
                            PlayAudio(attackStrings[curAtk.attackType]);
                            GlobalPrefab(curAtk.attackType);
                            break;
                        case ControlType.PLAYER:
                            healthManager.RemoveHealth(curAtk.damage);
                            PlayAudio("Hurt");
                            GlobalPrefab(2);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
    private bool isInvulnerable;
    private void StartInvul(float hitFlash)
    {
        if (invulCooldown <= 0 && (controlType != ControlType.AI && controlType != ControlType.OBJECT))
        {
            invulCooldown = 90f;
            isInvulnerable = true;
        }
        StartCoroutine(FlashWhiteDamage(hitFlash));
    }

    private IEnumerator FlashWhiteDamage(float hitFlash)
    {
        spriteRend.material = defaultMat;
        spriteRend.material = whiteMat;
        for (int i = 0; i < hitFlash; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        spriteRend.material = defaultMat;
        StartCoroutine(BlinkWhileInvulnerableCoroutine());
    }
    private IEnumerator BlinkWhileInvulnerableCoroutine()
    {
        while (isInvulnerable)
        {
            //yield return new WaitForSeconds(blinkInterval);
            spriteRend.color = flashColor;
            for (int i = 0; i < invulFlickerRate; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            spriteRend.color = Color.white;
            for (int i = 0; i < invulFlickerRate; i++)
            {
                yield return new WaitForFixedUpdate();
            }

        }
    }

    [SerializeField] string[] attackStrings;

    [Tooltip("hitstun index in coreData")]
    public int hitStunStateIndex = 7, deathStateIndex = 36;//hitstun state in coreData
    public float hitStun;
    public void GettingHit()
    {
        hitStun--;
        if (hitStun <= 0) { EndState();healthManager.PoiseReset(); }
        curHitAnim += (targetHitAnim - curHitAnim) * .1f;//blends for 3D games
    }
    [Header("EnemyLogic")]
    public CharacterObject target;
    public float aggroRange = 30f, longAttackRange = 10f, shortAttackRange = 5f, attackCooldown = 180f;
    [SerializeField] private bool isNearPlayer, isLongRange, isShortRange;
    public int[] closeAttackState, rangedAttackState, desperationCAStates, desperationRAStates;

    [Tooltip("0 = MoveForward, 1 = MoveTowards, 2 = JumpAction")]
    public int enemyType;
    public int desperationTransitionState;

    [Space]
    [Header("Blocking States")]
    public bool canDefend = false;
    public bool IsDefendingInState()
    {
        for (int i = 0; i < defStates.Length; i++)
        {
            if (currentState==defStates[i])
            {
                return true;
            }
        }
        return false;
    }
    public int defStateIndex;
    public int[] defStates;
    private void UpdateAI()
    {
        if (target == null)
        {
            FindTarget();
        }
        if (currentState == 0)//Neutral
        {
            if (isNearPlayer && (Mathf.Abs(transform.position.x - GameEngine.gameEngine.mainCharacter.transform.position.x) > longAttackRange))
            {
                FaceTarget(target.transform.position);
                switch (enemyType)
                {
                    case 0:
                        FrontVelocity(moveSpeed);
                        break;
                    case 1:
                        transform.position = Vector2.MoveTowards(transform.position, target.transform.position, moveSpeed);
                        break;
                    case 2:
                        StartState(rangedAttackState[0]);
                        break;
                }
            }

            if (dashCooldown <= 0 && (Mathf.Abs(transform.position.x - GameEngine.gameEngine.mainCharacter.transform.position.x) <= longAttackRange))
            {
                FaceTarget(target.transform.position);
                velocity = Vector2.zero;
                if (isLongRange && rangedAttackState.Length > 0)
                {
                    int randNum = UnityEngine.Random.Range(0, rangedAttackState.Length);
                    StartState(rangedAttackState[randNum]);
                }
                if (isShortRange && closeAttackState.Length > 0)
                {
                    int randNum = UnityEngine.Random.Range(0, closeAttackState.Length);
                    StartState(closeAttackState[randNum]);
                }
            }
        }
        if (currentState != 0 && currentState != defStateIndex)//Attack
        {
            dashCooldown = attackCooldown;
        }
    }
    public void OnDesperation()
    {
        closeAttackState = desperationCAStates;
        rangedAttackState = desperationRAStates;
        StartState(desperationTransitionState);
        dashCooldown += 100;
        attackCooldown *= 0.5f;
    }
    public void OnDeath()
    {
        StartState(deathStateIndex);
        controlType = ControlType.DEAD;
        invulCooldown = 0f;
        spriteRend.color = Color.white;
        spriteRend.material = defaultMat;
        SetVelocity(Vector2.zero);
    }
    public void OnEnemySpawn()
    {
        controlType = ControlType.AI;
        StartState(0);
    }
    public void OnObjectSpawn()
    {
        controlType = ControlType.OBJECT;
        StartState(0);
    }
    public void OnBossSpawn()
    {
        controlType = ControlType.BOSS;
        StartState(0);
    }
    void FindTarget()
    {
        target = GameEngine.gameEngine.mainCharacter;
    }
    void FaceTarget(Vector3 tarPos)
    {
        Vector3 tarOffset = (tarPos - transform.position);
        direction = Mathf.Sign(tarOffset.x);
        transform.localScale = new Vector3(direction, 1f, 1f);
    }
    public void FaceDir(float dir)
    {
        direction = Mathf.Sign(dir);
        transform.localScale = new Vector3(direction, 1f, 1f);
    }
    public void FacePlayer()
    {
        FaceTarget(GameEngine.gameEngine.mainCharacter.transform.position);
    }
}
