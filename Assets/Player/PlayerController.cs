using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {


    private Animator _animator;

    public string ActivateLandingTriggerName;

    // Use this for initialization
    void Start () {
        _animator = GetComponent<Animator>();
    }
    
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _animator.SetTrigger(ActivateLandingTriggerName);
        }
        
	}
}
