using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class single_door_open : MonoBehaviour
{
    public bool openDoor = false;

    private Animator _anim;

    void Start()
    {
        _anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.E))
        {
            _anim.SetBool("DoorVicinity", openDoor);
        }

        if (openDoor == false)
        {
            _anim.SetBool("DoorVicinity", openDoor);
        }
    }
}
