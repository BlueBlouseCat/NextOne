using System.Collections;
using System.Collections.Generic;
using Spine.Unity;
using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [SerializeField] private Vector2 _launchVelocity = new Vector2(-6f, 10f);
    [SerializeField] private float _controlLockTime = 0.2f;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!other.CompareTag("Player")) return;

        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if(rb == null) return;

        if(rb.velocity.y > 0f) return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if(player == null) return;

        player.Launch(_launchVelocity, _controlLockTime);
    }
}
