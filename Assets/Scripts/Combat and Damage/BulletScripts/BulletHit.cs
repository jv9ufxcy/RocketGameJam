using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class BulletHit : MonoBehaviour
{
    private Rigidbody2D bulletRB;
    private SpriteRenderer[] bulletSprites;
    private BoxCollider2D[] bulletColls;
    private AudioManager audioManager;
    [Tooltip("0 - straight, 1 - homing, 2 - target")]
    public int bulletType;
    public Vector2 direction;
    public CharacterObject target;
    public EnemySpawn closestEnemy;
    public Vector3 targetPos, velocity;
    [SerializeField] private GameObject bulletHitEffect, bulletChild;
    [SerializeField] private string tagToHit = "Enemy", tagToCollide="Ground";
    public float lifeTime = 2f, speed, rotation, gravity=-2f, targetRange=10f;
    [SerializeField] private int attackState = 0;
    [SerializeField] private LayerMask whatLayersToHit;

    [SerializeField] private string bulletCollisionSound;
    [SerializeField] private bool shouldScreenshakeOnHit = false, shouldStopOnHit = true, canExplodeOnHit = true, parabolicArc = false;
    public float explosionForce = 20f, explosionRadius = 5f;
    public CharacterObject character, thisBullet;
    // Use this for initialization
    void Awake()
    {
        bulletSprites = GetComponentsInChildren<SpriteRenderer>();
        bulletColls = GetComponents<BoxCollider2D>();
        bulletRB = GetComponent<Rigidbody2D>();

        if (bulletType==4)
        {
            thisBullet = GetComponent<CharacterObject>();
            thisBullet.FaceDir(direction.x);
        }
    }
    void Start()
    {
        if (bulletType!=0)
        {
            target = GameEngine.gameEngine.mainCharacter;
            targetPos = target.transform.position;
            velocity = (targetPos - transform.position).normalized * speed;
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, rotation);
        }
        if (parabolicArc)
        {
            thisBullet.Jump(1);
        }
    }
    private void FixedUpdate()
    {
        switch (bulletType)
        {
            case 0://fly straight
                transform.Translate(velocity * speed * Time.fixedDeltaTime);
                break;
            case 1://home in
                transform.position = Vector2.MoveTowards(transform.position, target.transform.position, speed);
                break;
            case 2://fly firectly at target
                transform.Translate(velocity * speed * Time.fixedDeltaTime);
                break;
            case 3://home in on closest enemy
                closestEnemy = EnemySpawn.GetClosestEnemy(transform.position, targetRange);
                if (closestEnemy!=null)
                    transform.position = Vector2.MoveTowards(transform.position, closestEnemy.transform.position, speed);
                break;
            case 4: //follow ground

                thisBullet.FrontVelocity( speed * transform.localScale.x);
                break;
        }
        //Countdown to lifetime
        if (lifeTime > 0)
        {
            lifeTime -= Time.fixedDeltaTime;
        }
        else if (lifeTime <= 0)
        {
            OnDestroyGO();
        }
    }
    public LayerMask whatObjectsAffectForce;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(tagToCollide))
        {
            if (shouldStopOnHit)
            {
                OnDestroyGO();
            }
            if (canExplodeOnHit)
            {
                ExplodeOnHit();
            }
        }
    }
    public Vector2 ExpVel;
    private void ExplodeOnHit()
    {
        Collider2D[] charactersToExplode = Physics2D.OverlapCircleAll(transform.position + transform.position.normalized, explosionRadius, whatObjectsAffectForce);
        foreach (Collider2D character in charactersToExplode)
        {
            CharacterObject chara = character.gameObject.GetComponent<CharacterObject>();

            var direction = chara.transform.position - (transform.position + transform.position.normalized);
            Debug.Log($"Position {transform.position} + Forward {transform.position.normalized} = Point {transform.position + transform.position.normalized}");
            Debug.Log("Direction = " + direction);

            Vector3 ExplosionVelocity = direction.normalized * explosionForce;
            ExpVel = ExplosionVelocity;
            Debug.Log("Velocity = " + ExpVel);
            chara.StartStateFromScript(chara.spinStateIndex);
            chara.velocity += ExpVel;
        }
    }

    private void ReflectOnHit(Collision2D collision)
    {
        Vector2 wallNormal = collision.contacts[0].normal;
        velocity = Vector2.Reflect(velocity, wallNormal).normalized;
    }

    public int projectileIndex, attackIndex = 0;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(tagToHit)&&other.gameObject!=character.gameObject)
        {
            CharacterObject victim = other.transform.root.GetComponent<CharacterObject>();
            if (victim != null&&projectileIndex>0)
                victim.GetHit(character, projectileIndex, attackIndex);

            if (shouldScreenshakeOnHit)
                Screenshake();
            if (shouldStopOnHit)
            {
                OnDestroyGO();
            }
            if (canExplodeOnHit)
            {
                ExplodeOnHit();
            }
        }
    }
    private bool isDestroyed = false;
    [SerializeField] private float destroyTimer = .1f;
    private void OnDestroyGO()
    {
        if (!isDestroyed)
        {
            isDestroyed = true;
            foreach (SpriteRenderer s in bulletSprites)
            {
                s.color = Color.clear;
            }
            foreach (BoxCollider2D s in bulletColls)
            {
                s.enabled = false;
            }
            bulletRB.isKinematic = true;
            RemoveForce();
            Instantiate(bulletHitEffect, transform.position, transform.rotation);
            if (thisBullet !=null)
            {
                thisBullet.StartStateFromScript(attackState);
            }
            Destroy(gameObject, destroyTimer);
        }
    }
    public void ReverseForce()
    {
        if (canExplodeOnHit)
        {
            transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
            
            gameObject.layer = LayerMask.NameToLayer("Projectile");
        }
        else
            Destroy(gameObject);
    }
    private void RemoveForce()
    {
        //stops the bullet on collision
        if (bulletRB!=null)
            bulletRB.velocity = Vector2.zero;
    }
    private static void Screenshake()
    {
        Camera.main.transform.GetComponent<CinemachineImpulseSource>().GenerateImpulse();
    }
}
