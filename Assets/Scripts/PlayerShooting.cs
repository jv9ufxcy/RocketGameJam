using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cinemachine;

public class PlayerShooting : MonoBehaviour
{
    public static PlayerShooting instance;

    [Header("Rotation Fields")]
    [SerializeField] private float rotSpeed;
    public Transform playerAnchor, bulletMuzzle;
    [SerializeField] private GameObject playerCharacter;

    [Space]
    [Header("Charging Shot")]
    [SerializeField] private Slider chargeSlider;
    [SerializeField] private float minLaunchForce, maxLaunchForce, maxChargeTime, currentLaunchForce;

    public Vector2 rightStick;

    private AudioManager audioManager;

    private void OnEnable()
    {
        //currentLaunchForce = minLaunchForce;
        //chargeSlider.value = minLaunchForce;
    }
    private void Awake()
    {
        instance = this;
        //chargeSlider = GetComponentInChildren<Slider>();  
    }
    private void Start()
    {
        audioManager = AudioManager.instance;
        if (audioManager == null)
        {
            Debug.LogError("No Audio Manager in Scene");
        }
        bulletMuzzle = GetComponentInChildren<Transform>();
    }
    void Update()
    {
        rightStick = new Vector2(Input.GetAxis(GameEngine.coreData.rawInputs[15].name), Input.GetAxis(GameEngine.coreData.rawInputs[16].name));
    }
    private void FixedUpdate()
    {
        RotateWithAnalogDirection();
    }
    
    private void RotateWithAnalogDirection()
    {
        if (playerAnchor==null)
            playerAnchor = GameEngine.gameEngine.mainCharacter.transform;
        else
            this.transform.position = playerAnchor.position;

        if (rightStick != Vector2.zero)
        {
            Quaternion newRot = Quaternion.Euler(new Vector3(0, 0, Mathf.Atan2(-rightStick.x, -rightStick.y) * Mathf.Rad2Deg));
            transform.rotation = Quaternion.Slerp(transform.rotation, newRot, rotSpeed);
        }
    }
}
