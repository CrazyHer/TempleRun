using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRotate : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.DownArrow))
        {
            Vector3 v3 = new Vector3(0, 90, 0);
            this.transform.Rotate(v3);
            Debug.Log("rotate 90 degree");
        }
    }
}
