using PlayerAnimationID;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BraveCookie : PlayerController
{
    Animator animator;
    [SerializeField] private ParticleSystem DashParticle;
    public void Start()
    {
        animator = GetComponent<Animator>();
        DashParticle = GetComponentInChildren<ParticleSystem>();    
        PlayerHP(100);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void PlayerHP(int Health)
    {
        base.PlayerHP(Health);
    }

    public override void PlayerTakeDamageState(int damage)
    {
        base.PlayerTakeDamageState(damage);
    }

    public override void PlayerInvinvibleState()
    {
        //Instantiate(DashParticle, transform.position, Quaternion.identity);
        DashParticle.Play();
        base.PlayerInvinvibleState();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Floor"))
        {
            animator.SetBool(PlayerAniID.IS_Jumping, false);
            animator.SetBool(PlayerAniID.IS_DobuleJumping, false);
        }
    }
 
}
