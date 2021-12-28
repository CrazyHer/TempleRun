//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------——————————

namespace Microsoft.Samples.Kinect.BodyBasics
{
    using Microsoft.Kinect;
    using SocketUtil;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading;
    using System.Windows;
    using System.Windows.Media;


    //搭建主窗口

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        int i = 0;
        point[] HandTipLeft = new point[200000];
        point[] HandTipRight = new point[200000];
        point[] Spine = new point[200000];
        point[] ShoulderCenter = new point[200000];
        point[] ShoulderLeft = new point[200000];
        point[] ShoulderRight = new point[200000];
        point[] HipCenter = new point[200000];
        point[] Head = new point[200000];

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for body frames
        /// </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary>
        /// Array for the bodies
        /// </summary>
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        public struct point
        {
            public double x, y, z;
        }
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
           // SocketUtil.SocketServer socket = new SocketServer("127.0.0.1", 8888);
            //socket.StartListen();

            Console.WriteLine("生成窗口");
            Thread t = new Thread(new ThreadStart(DoChoose));
            Thread t1 = new Thread(new ThreadStart(DoChoose_Xposition));
            t.Start();
            t1.Start();

            // one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();

            // get the coordinate mapper
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            // get the depth (display) extents
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            // get size of joint space
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            // populate body colors, one for each BodyIndex
            this.bodyColors = new List<Pen>();

            this.bodyColors.Add(new Pen(Brushes.Red, 6));
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // use the window object as the view model in this simple example
            this.DataContext = this;

            // initialize the components (controls) of the window
            this.InitializeComponent();
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        private void TestFrame()
        {

        }




        Boolean first_see = true;
        Double init_vertical = 0;
        Double init_horizontal = 0;
        Double init_distance = 0;
        Double init_head_vertical = 0;

        //采集捕获数据
        /// <summary>
        /// Handles the body frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }
            if (dataReceived)
            {

                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;
                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            // convert the joint points to depth (display) space
                            Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                            // 需要使用到的节点要在这里添加，不然采集不到
                            // 手指和手的关节点
                            HandTipLeft[i].x = body.Joints[JointType.HandTipLeft].Position.X;
                            HandTipLeft[i].y = body.Joints[JointType.HandTipLeft].Position.Y;
                            HandTipRight[i].x = body.Joints[JointType.HandTipRight].Position.X;
                            HandTipRight[i].y = body.Joints[JointType.HandTipRight].Position.Y;

                            // 脊柱点
                            Spine[i].x = body.Joints[JointType.SpineMid].Position.X;
                            Spine[i].y = body.Joints[JointType.SpineMid].Position.Y;
                            Spine[i].z = body.Joints[JointType.SpineMid].Position.Z;

                            ShoulderCenter[i].x = body.Joints[JointType.SpineShoulder].Position.X;
                            ShoulderCenter[i].y = body.Joints[JointType.SpineShoulder].Position.Y;
                            ShoulderLeft[i].x = body.Joints[JointType.ShoulderLeft].Position.X;
                            ShoulderLeft[i].y = body.Joints[JointType.ShoulderLeft].Position.Y;
                            ShoulderRight[i].x = body.Joints[JointType.ShoulderRight].Position.X;
                            ShoulderRight[i].y = body.Joints[JointType.ShoulderRight].Position.Y;
                            HipCenter[i].x = body.Joints[JointType.SpineBase].Position.X;
                            HipCenter[i].y = body.Joints[JointType.SpineBase].Position.Y;

                            // 头部的前后距离
                            Head[i].y = body.Joints[JointType.Head].Position.Y;

                            // 采集用户初始时站立的点位
                            if (first_see)
                            {
                                Console.WriteLine("脊柱中心y坐标（垂直方向）");
                                Console.WriteLine(Spine[0].y);
                                init_vertical = Spine[0].y;

                                Console.WriteLine("脊柱中心z坐标（远近方向）");
                                Console.WriteLine(Spine[0].z);
                                init_distance = Spine[0].z;

                                Console.WriteLine("脊柱中心x坐标（水平方向）");
                                Console.WriteLine(Spine[0].x);
                                init_horizontal = Spine[0].x;

                                init_head_vertical = Head[0].y;
                                first_see = !first_see;
                            }

                            i++;

                            foreach (JointType jointType in joints.Keys)
                            {
                                // sometimes the depth(Z) of an inferred joint may show as negative
                                // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            this.DrawBody(joints, jointPoints, dc, drawPen);

                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                        }
                    }

                    // prevent drawing outside of our render area
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));
                }
            }
        }


        // 对从用户里获得的数据的识别和分析
        public SocketUtil.Action PointWhere(point[] HandTipLeft, point[] HandTipRight, point[] Spine, point[] ShoulderCenter, point[] ShoulderLeft, point[] ShoulderRight, point[] HipCenter, point[] Head, int i, int k)
        {
            int A = 0;
            int B = 0;
            int C = 0;
            int D = 0;

            for (; k > 0 && i - k > 0; k--)
            {
                double midY = (ShoulderCenter[i - k].y + HipCenter[i - k].y) / 2.0;
                //if (HandTipRight[i - k].y <= midY && HandTipLeft[i - k].y <= midY) continue;
                double X = 0;

                // 表示左手抬起
                if (HandTipLeft[i - k].x < ShoulderLeft[i - k].x - 0.4 && HandTipLeft[i - k - 1].x > ShoulderLeft[i - k].x - 0.4)
                {
                    A++;
                }

                //检测右手抬起
                if (HandTipRight[i - k].x > ShoulderRight[i - k].x + 0.4 && HandTipRight[i - k - 1].x < ShoulderRight[i - k].x + 0.4)
                {
                    B++;
                }

                //检测跳起
                if (Spine[i - k].y > init_vertical + 0.12 * (init_distance / Spine[i - k].z) && Spine[i - k - 1].y < init_vertical + 0.12 * (init_distance / Spine[i - k].z))
                //if (Spine[i - k].y > init_vertical + 0.12 && Spine[i - k - 1].y < init_vertical + 0.12 )
                {
                    Console.WriteLine("跳跃x记录");
                    Console.WriteLine(Spine[i - 1].x);
                    Console.WriteLine("跳跃y记录");
                    Console.WriteLine(Spine[i - 1].y);
                    //Console.WriteLine("跳跃z记录");
                    //Console.WriteLine(Spine[i - 1].z);
                    C++;
                }

                //检测下蹲 和跳跃一样都要进行补偿
                if (Head[i - k].y < init_head_vertical - 0.2 * (init_distance / Spine[i - k].z) && Head[i - k - 1].y > init_head_vertical - 0.2 * (init_distance / Spine[i - k].z))
                {
                    D++;
                }
            }

            if (A != 0)
            {
                SocketUtil.Action action = new SocketUtil.Action(Spine[i - 1].x, 1);
                return action;
            }
            else if (B != 0)
            {
                SocketUtil.Action action = new SocketUtil.Action(Spine[i - 1].x, 2);

                return action;
                //return new Action(20, Spine[i - 1].x); ;
            }
            else if (C != 0)
            {
                SocketUtil.Action action = new SocketUtil.Action(Spine[i - 1].x, 3);
                return action;
                //return new Action(30, Spine[i - 1].x);
            }
            //else if (D != 0)
            //{
            //    SocketUtil.Action action = new SocketUtil.Action(Spine[i - 1].x, 4);
            //    return action;
            //    //return new Action(40, Spine[i - 1].x);
            //}
            else
            {
                if (i < 1)
                {
                    SocketUtil.Action action = new SocketUtil.Action(0, 0);
                    return action;
                }
                else
                {
                    SocketUtil.Action action = new SocketUtil.Action(Spine[i - 1].x, 0);
                    return action;
                }
            }
        }



        //// DoChooseXposition()线程用来对PointWhere进行解析
        SocketUtil.Action recognize_position;
        
        public  void DoChoose_Xposition()
        {
             SocketClient client = new SocketClient("1.116.132.94", 6001);
           // SocketClient client = new SocketClient("127.0.0.1",8888);
            //SocketClient client = new SocketClient("10.27.130.123", 2333);

            Console.WriteLine("位置发送开始");
            while (true)
            {
                recognize_position = PointWhere(HandTipLeft, HandTipRight, Spine, ShoulderCenter, ShoulderLeft, ShoulderRight, HipCenter, Head, i, 1);

                //Console.WriteLine(recognize_position.getAction_mark() + "   " + recognize_position.getX_position());//输出选择结果
                double a = recognize_position.getX_position();
                if (a > 0.5)
                {
                    a = 1;
                }
                else if (a < -0.5)
                {
                    a = -1;
                }            
                else
                {
                    if (a == 0)
                    {
                        Console.WriteLine("---------------------------------------------------------------------------------------------");
                        a = 0.0001;
                    }
                    else
                    {
                        a = a * 2;
                    }
                }
                //Console.WriteLine(recognize_position.getAction_mark() + "   " + a);//输出选择结果
               // string position_message = a.ToString() + ",0,";

                //client.send(position_message);
                
                client.send(a.ToString()+",0,");
                //Console.WriteLine("识别位置");//测试
                Thread.Sleep(50);
            }
        }


        // 之后额外写一个DoChoose() 专门用来不断发送 X 位置信息 

        SocketUtil.Action recognize_action;

        public void DoChoose()
        {

           // SocketClient client1 = new SocketClient("1.116.132.94", 6001);
            SocketClient client1 = new SocketClient("10.27.137.33",2333);
           //SocketClient client1 = new SocketClient("127.0.0.1", 8888);

            Console.WriteLine("动作发送开始");

            while (true)
            {
                recognize_action = PointWhere(HandTipLeft, HandTipRight, Spine, ShoulderCenter, ShoulderLeft, ShoulderRight, HipCenter, Head, i, 1);

                if (recognize_action.getAction_mark() == 1)   // 对左手抬手的识别
                {
                    Console.WriteLine(recognize_action.getAction_mark() + "   " + recognize_action.getX_position());//输出选择结果
                    string message = recognize_action.getX_position().ToString() + "," + recognize_action.getAction_mark().ToString();
                    //string message = 0.123123123 + "," + 3;
                    client1.send(message);
                    //client.send(s);
                    Console.WriteLine("抬起了   左手");//测试
                    Thread.Sleep(300);
                }
                else if (recognize_action.getAction_mark() == 2)  // 对右手抬手的识别
                {
                    Console.WriteLine(recognize_action.getAction_mark() + "   " + recognize_action.getX_position());//输出选择结果
                    string message = recognize_action.getX_position().ToString() + "," + recognize_action.getAction_mark().ToString();
                    client1.send(message);
                    Console.WriteLine("抬起了   右手");//测试
                    Thread.Sleep(300);
                }
                if (recognize_action.getAction_mark() == 3)   // 对跳跃的识别
                {
                   // Console.WriteLine(recognize_action.getAction_mark() + "   " + recognize_action.getX_position());//输出选择结果
                    Console.WriteLine("你跳起来了");//测试

                    Console.WriteLine(recognize_position.getAction_mark() + "   " + recognize_position.getX_position());//输出选择结果
                    double a = recognize_position.getX_position();
                    if (a > 0.5)
                    {
                        a = 1;
                    }
                    else if (a < -0.5)
                    {
                        a = -1;
                    }
                    else {
                        if (a == 0)
                        {
                            // Console.WriteLine("---------------------------------------------------------------------------------------------");
                            a = 0.0001;
                        }
                        a = a * 2;
                    }
                   string message =a.ToString() + ",3,";

                   // string message = recognize_action.getX_position().ToString() + "," + recognize_action.getAction_mark().ToString();
                    client1.send(message);
                    Console.WriteLine("跳跃识别位置：" + recognize_action.getX_position());
                    Thread.Sleep(500);
                    // 发送跳跃信息后 半秒内不进行识别 防止误识别
                }
               

                recognize_action = new SocketUtil.Action(0, 0);
            }
        }
        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="drawingPen">specifies color to draw a specific body</param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext, Pen drawingPen)
        {
            // Draw the bones
            foreach (var bone in this.bones)
            {
                this.DrawBone(joints, jointPoints, bone.Item1, bone.Item2, drawingContext, drawingPen);
            }

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }




        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// /// <param name="drawingPen">specifies color to draw a specific bone</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext, Pen drawingPen)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = drawingPen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping body data
        /// </summary>
        /// <param name="body">body to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdges = body.ClippedEdges;

            if (clippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, this.displayHeight - ClipBoundsThickness, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }
    }
}
