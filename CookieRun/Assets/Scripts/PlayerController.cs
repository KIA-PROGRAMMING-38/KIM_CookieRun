using Mono.Cecil.Cil;
using PlayerAnimationID;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Animator animator;
    ParticleSystem DashPartice;

    // 플레이어 체력 1초마다 1씩 감소
    public int HP { get; private set; }
    IEnumerator playerHealthDecrease;
    WaitForSeconds waitForSeconds = new WaitForSeconds(1);

    // 플레이어 무적 상태
    IEnumerator InvincibleState;
    WaitForSeconds InvicibleWaitForSeconds = new WaitForSeconds(3);
    protected bool hurt;

    // 플레이어의 무적상태를 Alpha를 통해 알려준다.
    IEnumerator Alpha;
    WaitForSeconds AlphaSeconds = new WaitForSeconds(0.1f);
    protected SpriteRenderer _spriteRenderer;
    protected Color _halfAlpha = new Color(1, 1, 1, 0.5f);
    protected Color _fullAlpha = new Color(1, 1, 1, 1);

    // 플레이어 체력 변수
    private int _maxHP = 100;
    private int _minHP = 0;

    // 자석 센서
    [SerializeField] private MagnetSensor magnetSensor;

    // BraveCookie에서 코루틴을 Start에서 실행하기 때문에, 같이 Start 함수에 작성하면 로직이 꼬이게 된다.
    // 그래서 그런 버그를 방지하기 위해 Awake 함수에 작성한다.
    // 상속 받은 BraveCookie에서 Awake에 컴포넌트를 참조 받는다면 routine is null 이라는 오류가 발생한다.
    // BraveCookie는 Awake에서 참조를 받으면 playerHealthDecrease와 로직이 겹쳐서 에러가 발생한다.
    private void Awake()
    {
        playerHealthDecrease = PlayerHealthDecrease();
        InvincibleState = Invincible();
        Alpha = AlphaBlink();

        _spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        DashPartice = GetComponentInChildren<ParticleSystem>();

    }
  
    public virtual void PlayerHP(int Health)
    {
        HP = Health;
        StartCoroutine(playerHealthDecrease);
    }
    public virtual void PlayerTakeDamageState(int damage)
    {
        if (!hurt)
        {
            animator.SetTrigger(PlayerAniID.IS_TakeDamage);

            // 플레이어가 충돌로 데미지를 받으면 어떤 상태이든, 달리기 상태로 돌아와야한다.
            animator.SetBool(PlayerAniID.IS_Jumping, false);
            animator.SetBool(PlayerAniID.IS_DobuleJumping, false);
            animator.SetBool(PlayerAniID.IS_Slide, false);
            HP -= damage;
            if (HP <= 0)
            {
                GameManager.Instance.GameOver = true;
                PlayerDeathAnimation(2);
            }
            hurt = true;
            StartCoroutine(InvincibleState);
            StartCoroutine(Alpha);
        }
    }

    // 플레이어 무적 상태
    public virtual void PlayerInvinvibleState()
    {
        hurt = true;
        animator.SetBool(PlayerAniID.IS_PlayDashRun, true);
        if (hurt)
        {
            StartCoroutine(DashRunCooltime());
        }

    }
    // 플레이어가 장애물과 충돌하지 않은 상태에서 사망함.
    public void PlayerDeathAnimation(int number)
    {
        if (HP <= 0)
        {
            // 플레이어가 죽는 상황이 다르기 때문에 주어지 상황을 다르게 만든다.
            if (number == 1)
            {
                animator.SetTrigger(PlayerAniID.IS_PlayerDeath);
            }
            else if (number == 2)
            {
                animator.SetTrigger(PlayerAniID.IS_PlayCrashDeath);
            }

        }
    }

    // 체력 증진
    public void PlayerIncreaseHP(int healthUP)
    {
        HP += healthUP;
        HP = Mathf.Clamp(HP, _minHP, _maxHP);
    }

    // 자석 아이템 
    public void ActiveMagnet()
    {
        magnetSensor.gameObject.SetActive(true);
    }

    // 코루틴
    // 1초마다 플레이어 체력을 1씩 감소시킨다.
    IEnumerator PlayerHealthDecrease()
    {
        while (HP > 0)
        {
            yield return waitForSeconds;
            HP -= 1;
            if (HP <= 0)
            {
                PlayerDeathAnimation(1);
                GameManager.Instance.GameOver = true;
            }
        }
    }

    // 무적상태 코루틴
    // 코루틴 로직을 while(true)문 안에 가둔다.
    // 그렇게 되면 StopCoroutine함수가 yield return null에서 종료되어도
    // 다음 코루틴 호출 될 시 while문 덕분에 다시 처음 맨 위로 돌아갈 수 있다.
    IEnumerator Invincible()
    {
        while (true)
        {
            // 로직
            yield return InvicibleWaitForSeconds;
            hurt = false;

            // StopCoroutine은 다음 프레임에서 종료된다. 그러므로 밑에 yield return null문에서 종료된다.
            // 만약 yield return null문이 없다면 다음 프레임인 yield return InvicibleWaitForSeconds문에서 종료된다.
            // 그렇게 되면 다음 코루틴 시작할 때 _hurt = false문이 제일 먼저 실행되는 상황이 발생한다.
            // 그러므로 yield return null문을 사용해서 StopCoroutine문이 종료 될 지점을 만들어주는 것이다.
            StopCoroutine(InvincibleState);

            yield return null;
        }
    }
    // 무적상태를 Sprite Render로 알려주는 코루틴
    IEnumerator AlphaBlink()
    {
        while (true)
        {
            // 로직
            while (hurt)
            {
                yield return AlphaSeconds;
                _spriteRenderer.color = _halfAlpha;

                yield return AlphaSeconds;
                _spriteRenderer.color = _fullAlpha;
            }

            // 코루틴을 종료한다.
            StopCoroutine(Alpha);

            // StopCoroutine은 다음 프레임에서 종료한다.
            // 종료 지점은 yield return null이다. 이렇게 만들어줌으로써
            // 다음 코루틴 시작할 때, while문 로직을 다시 실행 시킨다.
            yield return null;
        }
    }

    IEnumerator DashRunCooltime()
    {
        yield return InvicibleWaitForSeconds;
        DashPartice.Stop();
        animator.SetBool(PlayerAniID.IS_PlayDashRun, false);
        hurt = false;

        StopCoroutine(DashRunCooltime());

        yield return null;
    }
}
