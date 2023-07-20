using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour {

    void Start() {



    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            print(A());
        }
    }
    float A() {
        return Time.time;
    }
}

