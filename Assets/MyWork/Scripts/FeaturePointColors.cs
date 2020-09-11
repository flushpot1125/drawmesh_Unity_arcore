namespace GoogleARCore.Examples.ComputerVision
{

using System;
using GoogleARCore;
//using GoogleARCore.TextureReader;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
[RequireComponent(typeof(TextureReader))]
public class FeaturePointColors : MonoBehaviour
{
    // Scale output image dimensions for performance
    const int k_DimensionsInverseScale = 2;
    public GameObject cubePrefab;
    public int poolSize;
    byte[] m_PixelByteBuffer = new byte[0];
    int m_PixelBufferSize;
    Material[] m_PixelMaterials;
    GameObject[] m_PixelObjects;
    Color[] m_PixelColors;

	//added by Limes
	private const int k_MaxPointCount = 61440;
	//private int[] indices = new int[k_MaxPointCount];
    float intervalTime=1.0f;
    float elapsedTime;
    int pointsInViewCount_ref = 0;
    void Awake(){
        if (cubePrefab.GetComponent<Renderer>() == null) {
            Debug.LogError("No renderer on pixel prefab!");
            enabled = false;
            return;
        }
 
        var textureReader = GetComponent<TextureReader>();
        textureReader.ImageFormat = TextureReaderApi.ImageFormatType.ImageFormatColor;
        textureReader.OnImageAvailableCallback += OnImageAvailable;
 
        var landscape = ScreenIsLandscape();
        var scaledScreenWidth = Screen.width / k_DimensionsInverseScale;
        var scaledScreenHeight = Screen.height / k_DimensionsInverseScale;
 
        // It's arbitrary whether the output image should be portrait or landscape, as long as
        // you know how to interpret it for each potential screen orientation.
        textureReader.ImageWidth = landscape ? scaledScreenWidth : scaledScreenHeight;
        textureReader.ImageHeight = landscape ? scaledScreenHeight : scaledScreenWidth;
 
        m_PixelObjects = new GameObject[poolSize];
        m_PixelMaterials = new Material[poolSize];
        for (var i = 0; i < poolSize; ++i) {
            var pixelObj = Instantiate(cubePrefab, transform);
            m_PixelObjects[i] = pixelObj;
            m_PixelMaterials[i] = pixelObj.GetComponent<Renderer>().material;
            pixelObj.SetActive(false);
        }
    }
 
    static bool ScreenIsLandscape() {
        return Screen.orientation == ScreenOrientation.LandscapeLeft || Screen.orientation == ScreenOrientation.LandscapeRight;
    }
 
    void OnImageAvailable(TextureReaderApi.ImageFormatType format, int width, int height, IntPtr pixelBuffer, int bufferSize){
        if (format != TextureReaderApi.ImageFormatType.ImageFormatColor)
            return;
 
        // Adjust buffer size if necessary.
        if (bufferSize != m_PixelBufferSize || m_PixelByteBuffer.Length == 0){
            m_PixelBufferSize = bufferSize;
            m_PixelByteBuffer = new byte[bufferSize];
            m_PixelColors = new Color[width * height];
        }
 
        // Move raw data into managed buffer.
        System.Runtime.InteropServices.Marshal.Copy(pixelBuffer, m_PixelByteBuffer, 0, bufferSize);
 
        // Interpret pixel buffer differently depending on which orientation the device is.
        // We need to get pixel colors into a friendly format - an array
        // laid out row by row from bottom to top, and left to right within each row.
        var bufferIndex = 0;
        for (var y = 0; y < height; ++y) {
            for (var x = 0; x < width; ++x) {
                int r = m_PixelByteBuffer[bufferIndex++];
                int g = m_PixelByteBuffer[bufferIndex++];
                int b = m_PixelByteBuffer[bufferIndex++];
                int a = m_PixelByteBuffer[bufferIndex++];
                var color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                int pixelIndex;
                switch (Screen.orientation) {
                    case ScreenOrientation.LandscapeRight:
                        pixelIndex = y * width + width - 1 - x;
                        break;
                    case ScreenOrientation.Portrait:
                        pixelIndex = (width - 1 - x) * height + height - 1 - y;
                        break;
                    case ScreenOrientation.LandscapeLeft:
                        pixelIndex = (height - 1 - y) * width + x;
                        break;
                    default:
                        pixelIndex = x * height + y;
                        break;
                }
                m_PixelColors[pixelIndex] = color;
            }
        }
 
        FeaturePointCubes();
    }
 
    void FeaturePointCubes() {
        foreach (var pixelObj in m_PixelObjects) {
            pixelObj.SetActive(false);
        }
 
        var index = 0;
        var pointsInViewCount = 0;
        var camera = Camera.main;
        var scaledScreenWidth = Screen.width / k_DimensionsInverseScale;
        while (index < Frame.PointCloud.PointCount && pointsInViewCount < poolSize) {
            // If a feature point is visible, use its screen space position to get the correct color for its cube
            // from our friendly-formatted array of pixel colors.
            var point = Frame.PointCloud.GetPoint(index);
            var screenPoint = camera.WorldToScreenPoint(point);
            if (screenPoint.x >= 0 && screenPoint.x < camera.pixelWidth &&
                screenPoint.y >= 0 && screenPoint.y < camera.pixelHeight) {
                var pixelObj = m_PixelObjects[pointsInViewCount];
                pixelObj.SetActive(true);
                pixelObj.transform.position = point;
                var scaledX = (int)screenPoint.x / k_DimensionsInverseScale;
                var scaledY = (int)screenPoint.y / k_DimensionsInverseScale;
                m_PixelMaterials[pointsInViewCount].color = m_PixelColors[scaledY * scaledScreenWidth + scaledX];

				//added by Limes
			//	savePointCloudInfo(index,point,m_PixelMaterials[pointsInViewCount].color);
            //    pointsInViewCount_ref = pointsInViewCount;
                pointsInViewCount++;
            }
            pointsInViewCount_ref = pointsInViewCount;
            index++;
        }
    }
/* 
	bool savePointCloudInfo(int index,Vector3 pos, Vector4 color){
		string path = "";
		string pointcloudDataPath;
		using (AndroidJavaClass jcEnvironment = new AndroidJavaClass ("android.os.Environment"))
		using (AndroidJavaObject joExDir = jcEnvironment.CallStatic<AndroidJavaObject> ("getExternalStorageDirectory")) {
    	path = joExDir.Call<string>("toString");
	
		pointcloudDataPath = path + "/pointcloud";
		//フォルダがなければ作成
		if (!Directory.Exists(pointcloudDataPath)) Directory.CreateDirectory(pointcloudDataPath);

		string filepath = pointcloudDataPath + "/"+DateTime.Now.ToString("yyyyMMddHHmm")+".csv";
		StreamWriter sw= new StreamWriter(filepath, false);//trueにすると追記
		int maxPointIndex = indices.Length;
		int endLoopCount =0;
		
		if( (indices[index] ==0) && (pos.x == 0) && (pos.y == 0) && (pos.z == 0) ){
			endLoopCount++;
			if(endLoopCount == 3){
				sw.Flush();
				sw.Close();
				return true;
			}
		}
		string tmp = indices[index].ToString()+","+pos.x.ToString()+","+pos.y.ToString()+","+pos.z.ToString()+","+color.x.ToString()+","+color.y.ToString()+","+color.z.ToString()+","+color.w.ToString();
		sw.WriteLine(tmp);
		sw.Flush();
		sw.Close();
		return true;
		}
	}
*/
    bool savePointCloudInfo(int index){
		string path = "";
		string pointcloudDataPath;
		using (AndroidJavaClass jcEnvironment = new AndroidJavaClass ("android.os.Environment"))
		using (AndroidJavaObject joExDir = jcEnvironment.CallStatic<AndroidJavaObject> ("getExternalStorageDirectory")) {
    	path = joExDir.Call<string>("toString");
		pointcloudDataPath = path + "/pointcloud";
		if (!Directory.Exists(pointcloudDataPath)) Directory.CreateDirectory(pointcloudDataPath);

		string filepath = pointcloudDataPath + "/"+DateTime.Now.ToString("yyyyMMddHHmm")+".csv";
		StreamWriter sw= new StreamWriter(filepath, true);
	
        for(int i=0;i<index;i++){
            string tmp = i.ToString()+","+m_PixelObjects[i].transform.position.x.ToString()+","+m_PixelObjects[i].transform.position.y.ToString()+","+m_PixelObjects[i].transform.position.z.ToString()+","+m_PixelMaterials[i].color.r.ToString()+","+m_PixelMaterials[i].color.g.ToString()+","+m_PixelMaterials[i].color.b.ToString()+","+m_PixelMaterials[i].color.a.ToString();
		    sw.WriteLine(tmp);
        }

		sw.Flush();
		sw.Close();
		return true;
		}
	}
    void FixedUpdate(){
        elapsedTime += Time.deltaTime;
        if(elapsedTime > intervalTime){
            savePointCloudInfo(pointsInViewCount_ref);
            elapsedTime = 0;
        }
    }
}
}