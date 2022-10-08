using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gethit : MonoBehaviour
{
    Animator anim1;
    // Start is called before the first frame update
    void Start()
    {
        anim1 = GetComponent<Animator>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
        {
            anim1.SetBool("GetHit", true);
            anim1.SetBool("Idle", false);
        }
        if(Input.GetKeyDown(KeyCode.W))
        {
            anim1.SetBool("GetHit", false);
            anim1.SetBool("Idle", true);
        }
        
    }
}
