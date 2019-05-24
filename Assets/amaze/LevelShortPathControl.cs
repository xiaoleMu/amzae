using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

public class LevelShortPathControl : MonoBehaviour
{
	private int id = 0;
	private int width = 0;
	private int heigh = 0;
	private int[,] m_Squares;
	private PointData[,] m_PointDatas;

	private int m_CurPointCount = 0;

	// Use this for initialization
	void Start ()
	{
		
	}

	public void StartProcessData()
	{
//		ProcessXmlData("Assets/amaze/amaze_levels/018.xml");
//		
//		Stopwatch st=new Stopwatch ();
//		st. Start();
//		List<Direction> result = GenerateShortPath(m_StartPoint);
//		string resultStr = "Result is : ";
//		foreach (var direction in result)
//		{
//			resultStr = resultStr + (int)direction + ", ";
//		}
//		
//		UnityEngine.Debug.LogError(resultStr);
//		st.Stop();
		StartCoroutine (StartProcess ());
	}

	private IEnumerator StartProcess (){
//		int i=51;
		for(int i=1; i<171; i++){
			ProcessXmlData(string.Format("Assets/amaze/amaze_levels/{0:D3}.xml", i));

			Stopwatch st=new Stopwatch ();
			st. Start();
			List<Direction> result = GenerateShortPath(m_StartPoint);
			st.Stop();
			string resultStr = "Result is : ";
			foreach (var direction in result)
			{
				resultStr = resultStr + (int)direction + ", ";
			}

			resultStr = string.Format("{0:D3}.xml, {1}, ", i, st.Elapsed.ToString()) + resultStr;
			UnityEngine.Debug.LogError(resultStr);

			yield return new WaitForSeconds(0.1f);
		}
	}

	private PointData m_StartPoint = null;
	private void ProcessXmlData(string path)
	{
		XmlDocument xmlDoc = new XmlDocument();
		xmlDoc.Load(path);
		XmlNode map = xmlDoc.SelectSingleNode("map");
		XmlNode layer = map.SelectSingleNode("layer");
		width = int.Parse(layer.Attributes["width"].Value);
		heigh = int.Parse(layer.Attributes["height"].Value);
		id = int.Parse(layer.Attributes["id"].Value);

//		UnityEngine.Debug.LogError(string.Format("width is : {0}, height is : {1}, id is : {2}", width, heigh, id));

		XmlNode data = layer.SelectSingleNode("data");
		string dataStr = data.InnerText;
//		UnityEngine.Debug.LogError(dataStr);
		dataStr = Regex.Replace(dataStr, @"\s", "");
		string[] dataArray = dataStr.Split(',');
//		UnityEngine.Debug.LogError(dataArray.Length);
		m_CurPointCount = 0;
//		m_Squares = new int[heigh, width];
		m_PointDatas = new PointData[heigh, width];
		int index = 0;
		m_StartPoint = null;
		foreach (var t_data in dataArray)
		{
			int value = int.Parse(t_data);
			int x = index / width;
			int y = index % width;
//			m_Squares[x, y] = value;
			m_PointDatas[x, y] = new PointData(x, y, value);
			if (value > 0)
			{
				if (m_StartPoint == null){
					m_StartPoint = m_PointDatas[x, y];
				}else {
					if (x>=m_StartPoint.x && y<=m_StartPoint.y){
						m_StartPoint = m_PointDatas[x, y];
					}
				}

				m_CurPointCount++;
			}

			index++;
		}
		
//		m_StartPoint = new PointData(max_x, min_y);
//		m_StartPoint = m_PointDatas[max_x, min_y];
//		UnityEngine.Debug.LogError(string.Format ("{0}, {1}", m_StartPoint.x, m_StartPoint.y));
	}

	private  List<SolutionData> solutionDatas = new  List<SolutionData>();
	//int curMaxCount = 0;
	private List<Direction> GenerateShortPath(PointData startPoint)
	{
		solutionDatas.Clear();
		List<Direction> result = new List<Direction>();
		
//		var queue = new  Queue();
//		queue.Enqueue(new DequeueData(startPoint, null));

		List<DequeueData> queueList = new List <DequeueData> ();
		queueList.Add (new DequeueData(startPoint, null));

		while (queueList.Count != 0)// || solutionDatas.Count != 0)
		{
//			UnityEngine.Debug.LogError (string.Format("{0}_{1}_{2}", queueList[0].point.x, queueList[0].point.y, queueList[0].point.value));
			DequeueData t_CurDequeueData = queueList[0];
			queueList.Remove (queueList[0]);
			
			List<PathData> t_NearPaths = GetNearPoints(t_CurDequeueData.point);
			if (t_NearPaths.Count == 0)
			{
				UnityEngine.Debug.LogError(string.Format("{0},{1} Point not have Near Paths !!! {2}", t_CurDequeueData.point.x, t_CurDequeueData.point.y, t_CurDequeueData.point.value));
				result.Add(Direction.None);
				return result;
			}
			else
			{
				foreach (var nearPath in t_NearPaths)
				{
					if (t_CurDequeueData.solution == null)
					{
						SolutionData t_Solution = new SolutionData();
						t_Solution.pathDatas = new  List<PathData>();
						t_Solution.pointDatas = new List<string>();
						t_Solution.pathDatas.Add(nearPath);
						foreach (var pointData in nearPath.pointDatas)
						{
							string pointKey = pointData.x.ToString() + pointData.y.ToString();
							if (!t_Solution.pointDatas.Contains(pointKey))
							{
								t_Solution.pointDatas.Add(pointKey);
							}
						}
						
						if (IsWin(t_Solution))
						{
							foreach (var pathData in t_Solution.pathDatas)
							{
								result.Add(pathData.direction);
							}

							return result;
						}

						if (t_Solution.pathDatas.Count < 100)
						{
							solutionDatas.Add(t_Solution);

							queueList.Add (new DequeueData(nearPath.endPoint, t_Solution));
//							queue.Enqueue(new DequeueData(nearPath.endPoint, t_Solution));
						}
					}
					else
					{
						PathData t_LastSolutionPathData = t_CurDequeueData.solution.pathDatas[t_CurDequeueData.solution.pathDatas.Count-1];
						if (!IsLoopPath(t_LastSolutionPathData, nearPath))
						{
							SolutionData t_Solution = new SolutionData();
							t_Solution.pathDatas = new List<PathData>(t_CurDequeueData.solution.pathDatas);
							t_Solution.pointDatas = new List<string>(t_CurDequeueData.solution.pointDatas);
							t_Solution.pathDatas.Add(nearPath);
							int solutionPointData = t_Solution.pointDatas.Count();
							foreach (var pointData in nearPath.pointDatas)
							{
								string pointKey = pointData.x.ToString() + pointData.y.ToString();
								if (!t_Solution.pointDatas.Contains(pointKey))
								{
									t_Solution.pointDatas.Add(pointKey);
								}
							}

							if(solutionPointData <= t_Solution.pointDatas.Count()){
								t_Solution.WaitTimes ++;
							}

							if (IsWin(t_Solution))
							{
								foreach (var pathData in t_Solution.pathDatas)
								{
									result.Add(pathData.direction);
								}

								return result;
							}

							if (t_Solution.pathDatas.Count < 100 && t_Solution.pointDatas.Count > t_Solution.pathDatas.Count && t_Solution.WaitTimes < 4)
							{
								bool needAdd = true;
								int duplicatePointCount = 0;
								DequeueData needChangeDequeue = null;
								foreach (DequeueData dequeueData in queueList){
									if (dequeueData.point == nearPath.endPoint){
										duplicatePointCount ++;
										needAdd = false;
										if (dequeueData.solution.pointDatas.Count < t_Solution.pointDatas.Count){
											dequeueData.solution = t_Solution;
											//needChangeDequeue = dequeueData.solution;
										}
									}
								}

								if (needAdd){
									solutionDatas.Add(t_Solution);
									queueList.Add (new DequeueData(nearPath.endPoint, t_Solution));
								}
//								queue.Enqueue(new DequeueData(nearPath.endPoint, t_Solution));
							}
						}
					}
				}

				if (t_CurDequeueData.solution != null)
				{
					solutionDatas.Remove(t_CurDequeueData.solution);
				}
			}
		}
		
		result.Add(Direction.None);
		return result;
	}

	private bool IsLoopPath(PathData oldPath, PathData newPath)
	{
		PointData tPoint = newPath.startPoint;
		int count = 0;
//		if (tPoint.x > 0 && m_Squares[tPoint.x - 1, tPoint.y] > 0) count++; 
//		if (tPoint.x < heigh-1 && m_Squares[tPoint.x + 1, tPoint.y] > 0) count++; 
//		if (tPoint.y > 0 && m_Squares[tPoint.x, tPoint.y - 1] > 0) count++; 
//		if (tPoint.y < width-1 && m_Squares[tPoint.x, tPoint.y + 1] > 0) count++;
		if (tPoint.x > 0 && m_PointDatas[tPoint.x - 1, tPoint.y].value > 0) count++; 
		if (tPoint.x < heigh-1 && m_PointDatas[tPoint.x + 1, tPoint.y].value > 0) count++; 
		if (tPoint.y > 0 && m_PointDatas[tPoint.x, tPoint.y - 1].value > 0) count++; 
		if (tPoint.y < width-1 && m_PointDatas[tPoint.x, tPoint.y + 1].value > 0) count++;
		if (count <= 1) return false;

		if (oldPath.startPoint.x == newPath.endPoint.x && oldPath.startPoint.y == newPath.endPoint.y && oldPath.endPoint.x == newPath.startPoint.x && oldPath.endPoint.y == newPath.startPoint.y)
		{
			return true;
		}

		return false;
	}

	private List<PathData> GetNearPoints(PointData point)
	{
		List<PathData> t_NearPaths = new List<PathData>();
		
		int t_Y = point.y;
		List<PointData> t_PointData = new List<PointData>();
		if (t_Y > 0)
		{
			t_PointData.Add(point);
//			while (m_Squares[point.x, t_Y-1] > 0)
			while (m_PointDatas[point.x, t_Y-1].value > 0)
			{
				t_Y--;
				t_PointData.Add(m_PointDatas[point.x, t_Y]);
			}

			if ( t_Y != point.y)
			{
				t_NearPaths.Add(new PathData(point, m_PointDatas[point.x, t_Y], Direction.Left, t_PointData));
			}
		}
		
		t_Y = point.y;
		t_PointData.Clear();
		if (t_Y < width)
		{
			t_PointData.Add(point);
//			while (m_Squares[point.x, t_Y+1] > 0)
			while (m_PointDatas[point.x, t_Y+1].value > 0)
			{
				t_Y++;
				t_PointData.Add(m_PointDatas[point.x, t_Y]);
			}

			if ( t_Y != point.y)
			{
				t_NearPaths.Add(new PathData(point, m_PointDatas[point.x, t_Y], Direction.Right, t_PointData));
			}
		}
		
		int t_X = point.x;
		t_PointData.Clear();
		if (t_X > 0)
		{
			t_PointData.Add(point);
//			while (m_Squares[t_X-1, point.y] > 0)
			while (m_PointDatas[t_X-1, point.y].value > 0)
			{
				t_X--;
				t_PointData.Add(m_PointDatas[t_X, point.y]);
			}

			if ( t_X != point.x)
			{
				t_NearPaths.Add(new PathData(point, m_PointDatas[t_X, point.y], Direction.Up, t_PointData));
			}
		}
		
		t_X = point.x;
		t_PointData.Clear();
		if (t_X < heigh)
		{
			t_PointData.Add(point);
//			while (m_Squares[t_X+1, point.y] > 0)
			while (m_PointDatas[t_X+1, point.y].value > 0)
			{
				t_X++;
				t_PointData.Add(m_PointDatas[t_X, point.y]);
			}

			if ( t_X != point.x)
			{
				t_NearPaths.Add(new PathData(point, m_PointDatas[t_X, point.y], Direction.Down, t_PointData));
			}
		}

		return t_NearPaths;
	}

	private bool IsWin (SolutionData pSolution)
	{
		if (pSolution.pointDatas.Count >= m_CurPointCount)
		{
			return true;
		}

		return false;
	}
}

public class PointData
{
	public int x;
	public int y;
	public int value;

	public PointData(int px, int py, int pvalue)
	{
		x = px;
		y = py;
		value = pvalue;
	}
}

public class PathData
{
	public PointData startPoint;
	public PointData endPoint;
	public Direction direction;
	public List<PointData> pointDatas; 

	public PathData(PointData pStart, PointData pEnd, Direction pDir, List<PointData> pPointDatas)
	{
		startPoint = pStart;
		endPoint = pEnd;
		direction = pDir;
		pointDatas = new List<PointData>(pPointDatas);
	}
}

public class DequeueData
{
	public PointData point;
	public SolutionData solution;

	public DequeueData(PointData pPointData, SolutionData pSolutionData)
	{
		point = pPointData;
		solution = pSolutionData;
	}
}

public class SolutionData
{
	public List<PathData> pathDatas;
	public List<string> pointDatas;
	public int WaitTimes = 0;
}

public enum Direction
{
	None = 0,
	Right = 1,
	Left = 2,
	Up = 3,
	Down = 4
}
