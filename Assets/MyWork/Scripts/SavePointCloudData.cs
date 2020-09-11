using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GoogleARCore;
using UnityEngine.UI;

public class SavePointCloudData : MonoBehaviour {

	public Text dirText;

  private const int k_MaxPointCount = 61440;

        private Mesh m_Mesh;

        private Vector3[] m_Points = new Vector3[k_MaxPointCount];

		private int[] indices = new int[k_MaxPointCount];

        /// <summary>
        /// Unity start.
        /// </summary>
        public void Start()
        {
            m_Mesh = GetComponent<MeshFilter>().mesh;
            m_Mesh.Clear();
        }

        /// <summary>
        /// Unity update.
        /// </summary>
        public void Update()
        {
            // Fill in the data to draw the point cloud.
            if (Frame.PointCloud.IsUpdatedThisFrame)
            {
                // Copy the point cloud points for mesh verticies.
                for (int i = 0; i < Frame.PointCloud.PointCount; i++)
                {
                    m_Points[i] = Frame.PointCloud.GetPoint(i);
                }

                // Update the mesh indicies array.
              //  int[] indices = new int[Frame.PointCloud.PointCount];
                for (int i = 0; i < Frame.PointCloud.PointCount; i++)
                {
                    indices[i] = i;
                }

                m_Mesh.Clear();
                m_Mesh.vertices = m_Points;
                m_Mesh.SetIndices(indices, MeshTopology.Points, 0);
            }
        }

	public void savePointCloudInfo(){


		string path = "";
		string pointcloudDataPath;
using (AndroidJavaClass jcEnvironment = new AndroidJavaClass ("android.os.Environment"))
using (AndroidJavaObject joExDir = jcEnvironment.CallStatic<AndroidJavaObject> ("getExternalStorageDirectory")) {
    path = joExDir.Call<string>("toString");
	
//	Debug.Log(path);	
	pointcloudDataPath = path + "/pointcloud";
//フォルダがなければ作成
if (!Directory.Exists(pointcloudDataPath)) Directory.CreateDirectory(pointcloudDataPath);

		string filepath = pointcloudDataPath + "/"+DateTime.Now.ToString("yyyyMMddHHmmss")+".csv";
		 StreamWriter sw= new StreamWriter(filepath, true);
		dirText.text =filepath;
		int maxPointIndex = indices.Length;
		int endLoopCount =0;
		for(int i=0; i<maxPointIndex;i++){
			if( (indices[i] ==0) && (m_Points[i].x == 0) && (m_Points[i].y == 0) && (m_Points[i].z == 0) ){
				endLoopCount++;
				if(endLoopCount == 3){
					break;
				}
			}
			string tmp = indices[i].ToString()+","+m_Points[i].x.ToString()+","+m_Points[i].y.ToString()+","+m_Points[i].z.ToString();
			sw.WriteLine(tmp);
		}
        
        sw.Flush();
        sw.Close();



}

//フォルダがなければ作成
//if (!Directory.Exists(path)) Directory.CreateDirectory(path);

	}
}
