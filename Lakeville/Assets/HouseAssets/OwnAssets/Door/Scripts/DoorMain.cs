using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorMain : MonoBehaviour
{
    public single_door_open single_door_open;

    void Start()
    {

    }
    void Update() {

    }
    private void OnTriggerEnter(Collider other)
    {
        single_door_open.openDoor = true;
    }
    private void OnTriggerExit(Collider other)
    {
        single_door_open.openDoor = false;
    }
}
