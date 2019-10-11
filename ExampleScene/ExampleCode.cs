using UnityEngine;
using System.Collections;

public class ExampleCode : MonoBehaviour {
	
	private GameObject robot, sphere, capsule, pen;
	
	void Awake()
	{
		robot = GameObject.Find("Robot");
		sphere = GameObject.Find("Sphere");
		capsule = GameObject.Find("Capsule");
	}
	
    void OnGUI() 
	{
        if (sphere != null && GUI.Button(new Rect(10, 70, 150, 50), "Voxelize Sphere"))
		{
            sphere.GetComponent<SpawnVoxels>().Voxelize();
		}
        if (robot != null && GUI.Button(new Rect(10, 120, 150, 50), "Voxelize Robot"))
		{
            robot.GetComponent<SpawnVoxels>().Voxelize();
		}
        if (capsule != null && GUI.Button(new Rect(10, 170, 150, 50), "Voxelize Capsule"))
		{
            capsule.GetComponent<SpawnVoxels>().Voxelize();
		}
    }
}
