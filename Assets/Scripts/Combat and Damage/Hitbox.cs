﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    private List<Collider2D> enemies = new List<Collider2D>();
    public int projectileIndex = 0;
    public CharacterObject character;
    [IndexedItem(IndexedItemAttribute.IndexedItemType.STATES)]
    public int stateIndex;

    // Start is called before the first frame update
    void Start()
    {
        if (character==null)
        {
            character = GetComponentInParent<CharacterObject>();
            //character = transform.root.GetComponent<CharacterObject>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (projectileIndex == 0)
        {
            if (character.hitActive > 0 && !enemies.Contains(collision))
            {
                CharacterObject victim = collision.transform.root.GetComponent<CharacterObject>();
                if (victim != null)
                {
                    enemies.Add(collision);
                    victim.GetHit(character, projectileIndex,0);
                }
                if (collision.gameObject.CompareTag("Shootable"))
                {
                    HealthManager destructible = collision.transform.root.GetComponent<HealthManager>();
                    enemies.Add(collision);
                    destructible.RemoveHealth(1);
                }
            }
        }
        else
        {
            if (!enemies.Contains(collision))
            {
                CharacterObject victim = collision.transform.root.GetComponent<CharacterObject>();
                if (victim != null)
                {
                    enemies.Add(collision);
                    victim.GetHit(character, projectileIndex,0);
                }
                if (collision.gameObject.CompareTag("Shootable"))
                {
                    HealthManager destructible = collision.transform.root.GetComponent<HealthManager>();
                    enemies.Add(collision);
                    destructible.RemoveHealth(1);
                }
            }
        }
    }
    public void RestoreGetHitBools()
    {
        enemies.Clear();
    }
}
