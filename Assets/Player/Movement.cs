using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SocketUtil;
using System;

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
    public GameObject InitLevelDesign;

    private SocketServer server;
    // Start is called only once in the beginning 
    void Start()
    {
        server = new SocketServer("127.0.0.1", 2333);
        server.StartListen();
        Debug.Log("Socket服务器已启动监听");
        // ...进行后续的初始化操作
        _currentStrikes = Strikes;
        
        _startPosition = transform.position;

        _speed = Speed;

        MyCanvas.SetActive(false);
        IngameScreen.SetActive(true);

        //level design

        _nextLevelPosition = new Vector3(0, 0, 0);
        lastLevels = new List<GameObject>(10)
        {
            InitLevelDesign
        };
        CallGameOver();
    }

    // Update is called once per frame
    void Update()
    {

        // from: https://docs.unity3d.com/ScriptReference/Transform.Translate.html


        //always move forward
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);

        if (server != null && server.hasMessage())
        {
            var rawMessage = server.GetMessageBuffer();
            // ...解析消息并进行相应的响应处理操作
          //  Debug.LogError(rawMessage);
            var message = rawMessage.Split(',');

            try
            {
                double position = double.Parse(message[0]);
                int actionType = int.Parse(message[1]);
                Debug.Log(rawMessage);
                //controlling the player up and down
                if (actionType == 3 && this.GetComponent<Rigidbody>().velocity.y == 0)
                {
                    //    transform.Translate(Vector3.up* Time.deltaTime * _speed * 5);
                    this.GetComponent<Rigidbody>().velocity = Vector3.up * 15;
                }
                this.GetComponent<Rigidbody>().position = Vector3.right * 3.5f / 2 * (float)position;
                transform.position = new Vector3(3.5f / 2 * (float)position, transform.position.y, transform.position.z);

                if (MyCanvas.activeInHierarchy) // 如果处于菜单界面
                {
                    if(actionType == 2)
                    {
                        RestartGame();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                Debug.LogError("rawMessage: " + rawMessage);
            }




            //if (Input.GetKey(KeyCode.LeftArrow))
            //{
            //    transform.Translate(Vector3.left * Time.deltaTime * _speed);
            //}
            //if (Input.GetKey(KeyCode.RightArrow))
            //{
            //    transform.Translate(Vector3.right * Time.deltaTime * _speed);
            //}

        }

        if (this.GetComponent<Transform>().position.y<=-0.2){
            //Debug.Log(this.GetComponent<Transform>().position.y);
            CallGameOver();
        }
        
    }



    public void OnTriggerEnter(Collider other)
    {
        //Debug.Log("I collide with something ");
        if (other.tag == "OBSTACLE")
        {
            //Debug.Log(" I HIT OBSTACLE :) ");
            //take care of player health
            _currentStrikes = _currentStrikes - 1;

            if (_currentStrikes <= 0)
            {



                CallGameOver();

                //RestartGame();
            }
        }
        
        //collecting (hitting) a pickup
        if (other.tag == "PICKUP")
        {

            Destroy(other.gameObject);

            _points = _points + 350;


            ScoreText.GetComponent<Text>().text = _points.ToString();
            
        }


        if (other.tag == "LOAD_NEW_LEVEL")
        {
            if (lastLevels.Count>=2) // 销毁上一个已经走过的地块的实例
            {
                Destroy(lastLevels[lastLevels.Count-1]);
                lastLevels.RemoveAt(lastLevels.Count - 1);
            }
            GameObject go = Instantiate(LevelDesignPrefab, LevelDesignParent); // 生成新地块实例
            _nextLevelPosition.Set(0, 0, _nextLevelPosition.z + go.GetComponent<LevelDesignProperties>().Length); // 设置坐标为在当前地块的前方
            go.transform.position = _nextLevelPosition;

            lastLevels.Insert(0,go); // 将新生成的地块加入进来
        }
    }
    private List<GameObject> lastLevels;
    private Vector3 _nextLevelPosition;
    
    public void RestartGame()
    {
        while (lastLevels.Count > 0)
        {
            Destroy(lastLevels[lastLevels.Count - 1]);
            lastLevels.RemoveAt(lastLevels.Count - 1);
        }
        GameObject go = Instantiate(LevelDesignPrefab, LevelDesignParent);
        _nextLevelPosition.Set(0, 0, 0);
        go.transform.position = _nextLevelPosition;

        lastLevels.Insert(0, go);
        //zero score
        _totalScore = 0;

        //set speed again
        _speed = Speed;
        _currentStrikes = Strikes;
        //put player in start position
        transform.position = _startPosition;
        MyCanvas.SetActive(false);
        IngameScreen.SetActive(true);
    }




    public void CallGameOver()
    {
        if (MyCanvas != null)
        {
            _speed = 0;

            MyCanvas.SetActive(true);
            IngameScreen.SetActive(false);
        }
    }
}