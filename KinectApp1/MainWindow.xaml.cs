using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;
using Microsoft.Kinect;
using System.Windows.Media.Imaging;


namespace KinectApp1
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinect;

        private WriteableBitmap _ScreenImage;
        private Int32Rect _ScreenImageRect;
        private int _ScreenImageStride;
        private short[] _DepthPixelData;
        private byte[] _ColorPixelData;
        private int bytesPerPixel = 4;


        public MainWindow()
        {
            InitializeComponent();

            try
            {
                if (KinectSensor.KinectSensors.Count == 0)
                {
                    throw new Exception("Kinectが接続されていません");
                }

                // Kinectインスタンスを取得する
                kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);

                // すべてのフレーム更新通知をもらう
                kinect.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(kinect_AllFramesReady);

                // Color,Depth,Skeletonを有効にする
                kinect.ColorStream.Enable();
                kinect.DepthStream.Enable();
                kinect.SkeletonStream.Enable();


                DepthImageStream depthStream = kinect.DepthStream;
                _ScreenImage = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Bgra32, null);
                _ScreenImageRect = new Int32Rect(0, 0, (int)Math.Ceiling(_ScreenImage.Width), (int)Math.Ceiling(_ScreenImage.Height));
                this._ScreenImageStride = depthStream.FrameWidth * 4;

                this._DepthPixelData = new short[kinect.DepthStream.FramePixelDataLength];
                this._ColorPixelData = new byte[kinect.ColorStream.FramePixelDataLength];


                imageDepthCamera.Source = _ScreenImage;

                // Kinectの動作を開始する
                //kinect.ElevationAngle = 5;
                kinect.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }



        // すべてのデータの更新通知を受け取る
        void kinect_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
                {
                    RenderScreen(colorFrame, depthFrame);
                }
            }


        }


        private void RenderScreen(ColorImageFrame colorFrame, DepthImageFrame depthFrame)
        {
            if (depthFrame == null) return;
            if (colorFrame == null) return;

            int depthPixelIndex;
            int playerIndex;
            int colorPixelIndex;
            ColorImagePoint colorPoint;
            int colorStride = colorFrame.BytesPerPixel * colorFrame.Width;

            byte[] playerImage = new byte[depthFrame.Height * _ScreenImageStride];
            int playerImageIndex = 0;


            depthFrame.CopyPixelDataTo(_DepthPixelData);
            colorFrame.CopyPixelDataTo(_ColorPixelData);


            for (int depthY = 0; depthY < depthFrame.Height; depthY++)
            {
                for (int depthX = 0; depthX < depthFrame.Width; depthX++, playerImageIndex += bytesPerPixel)
                {
                    depthPixelIndex = depthX + (depthY * depthFrame.Width);
                    playerIndex = _DepthPixelData[depthPixelIndex] & DepthImageFrame.PlayerIndexBitmask;

                    if (playerIndex != 0)
                    {
                        colorPoint = kinect.MapDepthToColorImagePoint(depthFrame.Format, depthX, depthY, _DepthPixelData[depthPixelIndex], colorFrame.Format);
                        colorPixelIndex = (colorPoint.X * colorFrame.BytesPerPixel) + (colorPoint.Y * colorStride);

                        playerImage[playerImageIndex] = _ColorPixelData[colorPixelIndex];         //Blue    
                        playerImage[playerImageIndex + 1] = _ColorPixelData[colorPixelIndex + 1];     //Green
                        playerImage[playerImageIndex + 2] = _ColorPixelData[colorPixelIndex + 2];     //Red
                        playerImage[playerImageIndex + 3] = 0xFF;                                          //Alpha
                    }
                }
            }

            _ScreenImage.WritePixels(_ScreenImageRect, playerImage, _ScreenImageStride, 0);
        }


    }
}
