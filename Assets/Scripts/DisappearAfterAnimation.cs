using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class DisappearAfterAnimation : MonoBehaviour {
    private Animator animator;
    public string animationName;
    
    // Start is called before the first frame update
    void Start() {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update() {
        if (animator.GetCurrentAnimatorStateInfo(0).IsName(animationName)) {
            Debug.Log("destroyed game object");
            Destroy(gameObject);
        }
    }
}
