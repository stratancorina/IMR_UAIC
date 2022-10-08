using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fight : MonoBehaviour
{
    Animator anim;
    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
        {
            anim.SetBool("Attack", true);
            anim.SetBool("Idle", false);
        }
        if(Input.GetKeyDown(KeyCode.W))
        {
            anim.SetBool("Attack", false);
            anim.SetBool("Idle", true);
        }
        
    }
}
