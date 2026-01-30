using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DateSiteBehavior : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Debug.Log("Date Site Attacked!");
        }
    }
}