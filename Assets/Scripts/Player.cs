using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    public static Player Instance;
    
    // public constants
    public float pushForce;
    public float minVelocity;
    public float maxVelocity;

    public float minJumpForce;
    public float maxJumpForce;
    
    public float midairTimeScale;
    public float dropForce;
    
    public float railTimeScale;
    public float railMinSpeed;
    
    public float unsafeRotationZ;

    // private state
    public enum State {
        Midair,
        OnGround,
        OnRail,
    }
    public State state;
    private float railSpeed;
    [NonSerialized] public int grindCount;
    private bool rolling;
    public bool safe;

    public delegate void OnJump();
    public OnJump onJump;

    public delegate void OnLand();
    public OnLand onLand;

    public delegate void OnUnsafeLanding();
    public OnUnsafeLanding onUnsafeLanding;

    public delegate void OnSafeLanding();
    public OnSafeLanding onSafeLanding;

    // component stuff
    private Rigidbody2D rb;
    private Animator animator;

    private Skateboard skateboard;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        skateboard = FindObjectOfType<Skateboard>();
        onJump += () => transform.eulerAngles = new Vector3(0, 0, unsafeRotationZ);
    }

    private void Update() {
        // Debug.Log("Player.Update() " + Time.deltaTime + " " + Time.unscaledDeltaTime + " ");
        safe = Input.GetKey(KeyCode.Return) || state != State.Midair;
        animator.SetBool("safe", state == State.Midair ? safe : true);

        if (state == State.OnRail) {
            rb.velocity = new Vector2(railSpeed, rb.velocity.y);
        }

        if (rolling && rb.velocity.x < minVelocity) {
            SlowToMinSpeed();
        }
    }

    public void Push(float multiplier = 1.0f)
    {
        if (rb.velocity.x < maxVelocity)
        {
            rb.AddForce(new Vector2(pushForce * multiplier, 0));
        }
        rolling = true;
    }

    public void Jump(float multiplier = 1.0f)
    {
        // if player jumped from the rail, jump proportional to grind count
        if (state == State.OnRail) {
            multiplier = Mathf.Lerp(0.1f, 1.5f, grindCount / 4.0f);
        }
        
        bool wasOnGround = state == State.OnGround;
        rb.AddForce(new Vector2(0, multiplier * Mathf.Lerp(minJumpForce, maxJumpForce, rb.velocity.x / maxVelocity)));
        state = State.Midair;
        Time.timeScale = midairTimeScale;
        // Time.fixedDeltaTime = 0.02f * Time.timeScale;
        skateboard.SetAnimation(Skateboard.Animation.Ollie);
        
        if(wasOnGround) onJump?.Invoke();
    }

    public void Drop() {
        if(rb.velocity.y > 0) rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(new Vector2(0, -1 * dropForce));
    }

    public void WipeOut() {
        // slow
        SlowToMinSpeed();
        rolling = false;
        
        // if player landed with unsecured score, screen shake magnitude is proportional to the score
        float magnitude = (Score.Instance.GetUnsecuredScore() > 0) ? (0.2f + Score.Instance.GetUnsecuredScore() * 0.1f) : 1f;
        StartCoroutine(CameraShake.Instance.Shake(magnitude));
        
        // particles?
    }

    private void SlowToMinSpeed() {
        rb.velocity = new Vector2(minVelocity, rb.velocity.y);
    }
    
    private void OnCollisionEnter2D(Collision2D other)
    {
        // land on ground
        if (other.gameObject.CompareTag("Ground") && state != State.OnGround) {
            state = State.OnGround;
            Time.timeScale = 1.0f;
            // Time.fixedDeltaTime = 0.02f * Time.timeScale;
            onLand?.Invoke();
            if (safe) {
                float multiplier = Mathf.Lerp(0.7f, 2.0f, Score.Instance.GetUnsecuredScore() / 10.0f);
                Push(multiplier);
                // callback
                onSafeLanding?.Invoke();
            }
            else {
                WipeOut();
                // callback
                onUnsafeLanding?.Invoke();
            }
        }
        
        // land on rail
        if (other.gameObject.CompareTag("Rail") && state != State.OnRail) {
            if (safe) {
                state = State.OnRail;
                Time.timeScale = railTimeScale;
                // Time.fixedDeltaTime = 0.02f * Time.timeScale;
                railSpeed = rb.velocity.x > railMinSpeed ? rb.velocity.x : railMinSpeed;
                grindCount = 0;
            }
            else {
                other.gameObject.GetComponent<BoxCollider2D>().enabled = false;
                WipeOut();
                state = State.Midair;
                Time.timeScale = midairTimeScale;
                // Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        // end of rail
        if (other.CompareTag("RailEnd")) {
            // set to midair
            state = State.Midair;
            Jump(0.1f);
        }
    }
}
