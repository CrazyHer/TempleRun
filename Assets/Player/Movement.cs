using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SocketUtil;

public class Movement : MonoBehaviour
{
    [Header("Movement")]
    public float Speed;
    private float _speed;

    [Tooltip("have a smooth movement vs avatar jumps from one position to the other")]
    public bool useSmoothRide;


    [Header("Score")]
    public int pickupScore;
    private int _totalScore;


    [Header("End Game")]
    public int Strikes;
    private int _currentStrikes;

    private Vector3 _startPosition;


    [Header("Score")]
    private int _points;



    public GameObject  MyCanvas;
    public GameObject IngameScreen;

    public GameObject ScoreText;


    //Level Design Elements
    public GameObject LevelDesignPrefab;
    public Transform LevelDesignParent;

    private SocketServer server;
    // Start is called only once in the beginning 
    void Start()
    {
        server = new SocketServer("127.0.0.1",2333);
        server.StartListen();

        _currentStrikes = Strikes;
        
        _startPosition = transform.position;

        _speed = Speed;

        MyCanvas.SetActive(false);
        IngameScreen.SetActive(true);

        //level design
        _nextLevelPosition = new Vector3(0, 0, 0);
        lastLevels = new List<GameObject>(10);
    }

    // Update is called once per frame
    void Update()
    {

        // from: https://docs.unity3d.com/ScriptReference/Transform.Translate.html


        //always move forward
        transform.Translate(Vector3.forward * Time.deltaTime * _speed);

        if (server != null && server.hasMessage())
        {
            Debug.Log("接收到socket消息：" + server.GetMessageBuffer()+"Count:"+server.count);
        }
        if(this.GetComponent<Transform>().position.y<=0){
            CallGameOver();
        }


        //controlling the player left and right
        if (useSmoothRide)
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                transform.Translate(Vector3.left * Time.deltaTime * _speed);
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                transform.Translate(Vector3.left);
            }
        }

        if (useSmoothRide)
        {
            if (Input.GetKey(KeyCode.RightArrow))
            {
                transform.Translate(Vector3.right * Time.deltaTime * _speed);
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                transform.Translate(Vector3.right);
            }
        }

        



       
       //controlling the player up and down
       if (Input.GetKey(KeyCode.UpArrow)&&this.GetComponent<Rigidbody>().velocity.y==0)
       {
        //    transform.Translate(Vector3.up* Time.deltaTime * _speed * 5);
        this.GetComponent<Rigidbody>().velocity = Vector3.up * 15;
       }
       /*
       if (Input.GetKey(KeyCode.DownArrow))
       {
           transform.Translate(Vector3.back * Time.deltaTime * _speed);
       }
       */
    }



    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("I collide with something ");
        if (other.tag == "OBSTACLE")
        {
            Debug.Log(" I HIT OBSTACLE :) ");
            //take care of player health
            _currentStrikes = _currentStrikes - 1;

            if (_currentStrikes <= 0)
            {
                Debug.Log(" GAME OVER ");
                
                Debug.Log("RESTART");


                CallGameOver();

                //RestartGame();
            }
        }
        
        //collecting (hitting) a pickup
        if (other.tag == "PICKUP")
        {
            Debug.Log(" I HIT PICKUP :) ");

            Destroy(other.gameObject);

            _points = _points + 350;


            ScoreText.GetComponent<Text>().text = _points.ToString();
            
        }


        if (other.tag == "LOAD_NEW_LEVEL")
        {
            if (lastLevels.Count>2)
            {
                Destroy(lastLevels[lastLevels.Count-1]);
                lastLevels.RemoveAt(lastLevels.Count - 1);
                Debug.Log("I destroyed a level design!!!");
            }
            GameObject go = Instantiate(LevelDesignPrefab, LevelDesignParent);
            go.transform.position = _nextLevelPosition;
            _nextLevelPosition.Set(0, 0, _nextLevelPosition.z + go.GetComponent<LevelDesignProperties>().Length);
            lastLevels.Insert(0,go);
        }
    }
    private List<GameObject> lastLevels;
    private Vector3 _nextLevelPosition;
    
    public void RestartGame()
    {
        _currentStrikes = Strikes;
        transform.position = _startPosition;
        _speed = Speed;

        MyCanvas.SetActive(false);
        IngameScreen.SetActive(true);
    }







    
    public void RestartGame1()
    {
        Debug.Log(" RESTARTING ");

        //put player in start position
        transform.position = _startPosition;

        //zero score
        _totalScore = 0;

        //set speed again
       Speed = 3;
    }
    



    public void CallGameOver()
    {
        if (MyCanvas != null)
        {
            _speed = 0.01f;

            MyCanvas.SetActive(true);
            IngameScreen.SetActive(false);
        }
    }
}