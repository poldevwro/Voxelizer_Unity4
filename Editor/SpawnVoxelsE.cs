/// Voxelizer, v.1.20. Copyright 2014 - Boni Games.
/// <summary>
/// This is the editor version of voxelizing class. It uses Raycast method to detect edges of the Object_To_Voxelize.
/// It then creates voxels in these spots. Optionally, if Fill is set to True, it also creates voxels inside the object.
/// For detailed parameter description please see Voxelizer_ReadMe.
/// </summary>

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public sealed class SpawnVoxelsE : ScriptableWizard {
	
	private float x1, x2, y1, y2, z1, z2;
	private float correction_left, correction_right, correction_up, correction_down, correction_back;
	private float correction_x, correction_y, correction_z;
	private int x_voxels, y_voxels, z_voxels, nr_mesh_triangles;
	private int mesh_nr = 0, mesh_id = 0, nr_voxels_created = 0;
	private Bounds bounds;
	private RaycastHit ray_hit1, ray_hit2, ray_hit3, ray_hit4, ray_hit5, ray_hit6, ray_hit7, ray_hit8, ray_hit9;
	private GameObject voxel, voxel_mark, bounding_box, voxel_parent;
	private List<Vector3> voxel_positions;
	private List<GameObject> voxel_objects;
	private Dictionary<GameObject, List<GameObject>> voxel_neighbours = new Dictionary<GameObject, List<GameObject>>();
	private Dictionary<Vector3, Vector3> voxel_pairs = new Dictionary<Vector3, Vector3>();
	private Dictionary<Vector3, Dictionary<string, Vector2[]>> voxel_material_dic = new Dictionary<Vector3, Dictionary<string, Vector2[]>>();
	private Dictionary<vector_decimal, int> fill_raycast = new Dictionary<vector_decimal, int>();
	private bool voxel_looking_for_pair;
	private Vector3 voxel_to_pair, voxel_position;
	private Vector2 r_downleft, r_downright, r_upleft, r_upright, r_mid, r_left, r_right, r_up, r_down;
	private Vector2 l_downleft, l_downright, l_upleft, l_upright, l_mid, l_left, l_right, l_up, l_down;
	private Vector2 u_downleft, u_downright, u_upleft, u_upright, u_mid, u_left, u_right, u_up, u_down;
	private Vector2 d_downleft, d_downright, d_upleft, d_upright, d_mid, d_left, d_right, d_up, d_down;
	private Vector2 f_downleft, f_downright, f_upleft, f_upright, f_mid, f_left, f_right, f_up, f_down;
	private Vector2 b_downleft, b_downright, b_upleft, b_upright, b_mid, b_left, b_right, b_up, b_down;
	private Vector2 default_downleft, default_downright, default_upleft, default_upright, default_mid, default_left, default_right, default_up, default_down;
	private Vector2[] uvs2;
	private LayerMask object_layer;
	private vector_decimal voxel_mark_dec, position_dec;
	public struct vector_decimal 
	{
		public decimal xd, yd, zd;
		public vector_decimal (decimal xd_, decimal yd_, decimal zd_)
		{
			this.xd = xd_;
			this.yd = yd_;
			this.zd = zd_;
		}
		public override string ToString ()
		{
			return string.Format ("({0:0.000}, {1:0.000}, {2:0.000})", this.xd, this.yd, this.zd);
		}
	}
//	public GameObject Object_To_Voxelize;
//	public GameObject Prefab_Voxel;
//	public Material Voxel_Material;
//	public bool Material_From_Object;
//	public bool Fill;
//	public Material Fill_Material;
//	public bool Voxel_Add_Rigidbody, Voxel_Add_Collider, Leave_Object=true;
//	public bool Info=true;

	[System.Serializable]
	public class V_Material
	{
		public bool Material_From_Object;
		public Material Custom_Material;
	}
	[System.Serializable]
	public class V_Fill
	{
		public bool Fill;
		public Material Fill_Material;
	}
	[System.Serializable]
	public class V_Components
	{
		public bool Add_Rigidbody;
		public bool Add_Collider;
	}
	[System.Serializable]
	public class JoinVoxels
	{
		public bool CreateJoints;
		public bool ConnectRigidbody;
		public float BreakForce=Mathf.Infinity;
	}

	public GameObject Object_To_Voxelize;
	public GameObject Prefab_Voxel;
	public V_Material Voxel_Material;
	public V_Fill Fill_Object;
	public V_Components Voxel_Components;
	public JoinVoxels Voxel_Joints;
	public bool Leave_Object=true;
	public bool Info = true;

	[MenuItem ("GameObject/Voxelize Wizard")]
    static void CreateWizard ()
	{
        ScriptableWizard.DisplayWizard<SpawnVoxelsE>("Voxelize Wizard", "Voxelize");
		EditorWindow.GetWindow<SpawnVoxelsE>().position = new Rect(100, 100, 550, 350);
    }
	
	void OnWizardUpdate()
	{
		helpString = "Please choose object to voxelize from Scene.\nPlease choose voxel prefab.";
		errorString = "";
		if (Object_To_Voxelize != null && Prefab_Voxel != null && Object_To_Voxelize.GetComponent<MeshRenderer>() != null && Object_To_Voxelize.GetComponent<MeshFilter>() != null && Object_To_Voxelize.GetComponent<MeshFilter>().sharedMesh != null
			&& !(Voxel_Material.Material_From_Object == true && Object_To_Voxelize.GetComponent<MeshCollider>() == null))
		{
			isValid = true;
		}
		else
		{
			isValid = false;
		}
		if (Object_To_Voxelize == null)
		{
			errorString += "No object to voxelize has been assigned. ";
		}
		if (Prefab_Voxel == null)
		{
			errorString += "No voxel prefab has been assigned. ";
		}
		if (Object_To_Voxelize != null && Object_To_Voxelize.GetComponent<MeshRenderer>() == null)
		{
			errorString += "Object_To_Voxelize requires MeshRenderer. ";
		}
		if (Object_To_Voxelize != null && Object_To_Voxelize.GetComponent<MeshFilter>() == null)
		{
			errorString += "Object_To_Voxelize requires MeshFilter. ";
		}
		if (Object_To_Voxelize != null && Object_To_Voxelize.GetComponent<MeshFilter>() != null && Object_To_Voxelize.GetComponent<MeshFilter>().sharedMesh == null)
		{
			errorString += "MeshFilter component of Object_To_Voxelize requires Mesh assigned. ";
		}
		if (Object_To_Voxelize != null && Voxel_Material.Material_From_Object == true && (Object_To_Voxelize.GetComponent<MeshCollider>() == null || Object_To_Voxelize.GetComponent<MeshCollider>().enabled == false))
		{
			errorString += "Material_From_Object option requires active MeshCollider. ";
		}
		if (Object_To_Voxelize != null && (Object_To_Voxelize.GetComponent<Collider>() == null || Object_To_Voxelize.GetComponent<Collider>().enabled == false))
		{
			errorString += "Object_To_Voxelize requires active Collider. ";
		}
	}
		
	void OnWizardCreate ()
	{
		if (Voxel_Joints.CreateJoints == true)
		{
			voxel_objects = new List<GameObject>();
		}
		DateTime d1 = DateTime.Now;
		EditorUtility.DisplayCancelableProgressBar ("Voxelizing...","",0);
		bounds = Object_To_Voxelize.transform.GetComponent<MeshRenderer>().bounds;
		bounds.Expand(1);
		bounding_box = Instantiate(Prefab_Voxel, bounds.center, Prefab_Voxel.transform.rotation) as GameObject;
		bounding_box.name = "Bounding_box";
		bounding_box.transform.localScale = bounds.size;
		nr_mesh_triangles = Prefab_Voxel.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3;
		object_layer = 1<<Object_To_Voxelize.gameObject.layer;
		x1 = bounding_box.transform.position.x - bounding_box.transform.localScale.x/2;
		x2 = bounding_box.transform.position.x + bounding_box.transform.localScale.x/2;
		y1 = bounding_box.transform.position.y - bounding_box.transform.localScale.y/2;
		y2 = bounding_box.transform.position.y + bounding_box.transform.localScale.y/2;
		z1 = bounding_box.transform.position.z - bounding_box.transform.localScale.z/2;
		z2 = bounding_box.transform.position.z + bounding_box.transform.localScale.z/2;
		x_voxels = Mathf.FloorToInt(bounding_box.transform.localScale.x/Prefab_Voxel.transform.localScale.x);
		y_voxels = Mathf.FloorToInt(bounding_box.transform.localScale.y/Prefab_Voxel.transform.localScale.y);
		z_voxels = Mathf.FloorToInt(bounding_box.transform.localScale.z/Prefab_Voxel.transform.localScale.z);
		
		voxel_mark = Instantiate(Prefab_Voxel,
					new Vector3(0,0,0),
					Prefab_Voxel.transform.rotation) as GameObject;

		correction_left = ((Object_To_Voxelize.transform.position.x - Object_To_Voxelize.transform.localScale.x/2) - (bounding_box.transform.position.x - bounding_box.transform.localScale.x/2))/Prefab_Voxel.transform.localScale.x;
		correction_left = correction_left - Mathf.Floor(correction_left);
		correction_left = correction_left * Prefab_Voxel.transform.localScale.x;
		correction_right = ((bounding_box.transform.position.x + bounding_box.transform.localScale.x/2) - (Object_To_Voxelize.transform.position.x + Object_To_Voxelize.transform.localScale.x/2))/Prefab_Voxel.transform.localScale.x;
		correction_right = correction_right - Mathf.Floor(correction_right);
		correction_right = correction_right * Prefab_Voxel.transform.localScale.x;
		correction_down = ((Object_To_Voxelize.transform.position.y - Object_To_Voxelize.transform.localScale.y/2) - (bounding_box.transform.position.y - bounding_box.transform.localScale.y/2))/Prefab_Voxel.transform.localScale.y;
		correction_down = correction_down - Mathf.Floor(correction_down);
		correction_down = correction_down * Prefab_Voxel.transform.localScale.y;
		correction_up = ((bounding_box.transform.position.y + bounding_box.transform.localScale.y/2) - (Object_To_Voxelize.transform.position.y + Object_To_Voxelize.transform.localScale.y/2))/Prefab_Voxel.transform.localScale.y;
		correction_up = correction_up - Mathf.Floor(correction_up);
		correction_up = correction_up * Prefab_Voxel.transform.localScale.y;
		correction_back = ((Object_To_Voxelize.transform.position.z - Object_To_Voxelize.transform.localScale.z/2) - (bounding_box.transform.position.z - bounding_box.transform.localScale.z/2))/Prefab_Voxel.transform.localScale.z;
		correction_back = correction_back - Mathf.Floor(correction_back);
		correction_back = correction_back * Prefab_Voxel.transform.localScale.z;
		
		voxel_positions = new List<Vector3>();
		for (int k=0; k<z_voxels; k++)
		{
			for (int j=0; j<y_voxels; j++)
			{
				voxel_looking_for_pair = false;
				for (int i=0; i<x_voxels; i++)
				{
					voxel_mark.transform.position = new Vector3(x1+correction_left+Prefab_Voxel.transform.localScale.x/2+i*Prefab_Voxel.transform.localScale.x, 
											y1+correction_down+Prefab_Voxel.transform.localScale.y/2+j*Prefab_Voxel.transform.localScale.y, 
											z1+correction_back+Prefab_Voxel.transform.localScale.z/2+k*Prefab_Voxel.transform.localScale.z);
					voxel_mark_dec = new vector_decimal((decimal)x1+(decimal)correction_left+(decimal)Prefab_Voxel.transform.localScale.x/2+(decimal)i*(decimal)Prefab_Voxel.transform.localScale.x,
															(decimal)y1+(decimal)correction_down+(decimal)Prefab_Voxel.transform.localScale.y/2+(decimal)j*(decimal)Prefab_Voxel.transform.localScale.y, 
															(decimal)z1+(decimal)correction_back+(decimal)Prefab_Voxel.transform.localScale.z/2+(decimal)k*(decimal)Prefab_Voxel.transform.localScale.z);
					voxel_mark_dec = new vector_decimal(decimal.Round(voxel_mark_dec.xd, 3),
															decimal.Round(voxel_mark_dec.yd, 3), 
															decimal.Round(voxel_mark_dec.zd, 3));
					if (Fill_Object.Fill == true)
					{
						fill_raycast.Add(voxel_mark_dec, 0);
						
						if (Physics.Raycast(new Vector3(x1-1, voxel_mark.transform.position.y, voxel_mark.transform.position.z), voxel_mark.transform.TransformDirection(Vector3.right), Mathf.Abs(voxel_mark.transform.position.x - (x1 - 1)), object_layer) == true)
							fill_raycast[voxel_mark_dec] += 1;
						if (Physics.Raycast(new Vector3(x2+1, voxel_mark.transform.position.y, voxel_mark.transform.position.z), voxel_mark.transform.TransformDirection(Vector3.left), Mathf.Abs(x2 + 1 - voxel_mark.transform.position.x), object_layer) == true)
							fill_raycast[voxel_mark_dec] += 1;
						if (Physics.Raycast(new Vector3(voxel_mark.transform.position.x, y1-1, voxel_mark.transform.position.z), voxel_mark.transform.TransformDirection(Vector3.up), Mathf.Abs(voxel_mark.transform.position.y - (y1 - 1)), object_layer) == true)
							fill_raycast[voxel_mark_dec] += 1;
						if (Physics.Raycast(new Vector3(voxel_mark.transform.position.x, y2+1, voxel_mark.transform.position.z), voxel_mark.transform.TransformDirection(Vector3.down), Mathf.Abs(y2 + 1 - voxel_mark.transform.position.y), object_layer) == true)
							fill_raycast[voxel_mark_dec] += 1;
						if (Physics.Raycast(new Vector3(voxel_mark.transform.position.x, voxel_mark.transform.position.y, z1-1), voxel_mark.transform.TransformDirection(Vector3.forward), Mathf.Abs(voxel_mark.transform.position.z - (z1 - 1)), object_layer) == true)
							fill_raycast[voxel_mark_dec] += 1;
						if (Physics.Raycast(new Vector3(voxel_mark.transform.position.x, voxel_mark.transform.position.y, z2+1), voxel_mark.transform.TransformDirection(Vector3.back), Mathf.Abs(z2 + 1 - voxel_mark.transform.position.z), object_layer) == true)
							fill_raycast[voxel_mark_dec] += 1;
					}
					
					if (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0), voxel_mark.transform.TransformDirection(Vector3.right), voxel_mark.transform.localScale.x, object_layer) == true ||
						Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0), voxel_mark.transform.TransformDirection(Vector3.left), voxel_mark.transform.localScale.x, object_layer) == true ||
						Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0), voxel_mark.transform.TransformDirection(Vector3.up), voxel_mark.transform.localScale.x, object_layer) == true ||
						Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0), voxel_mark.transform.TransformDirection(Vector3.down), voxel_mark.transform.localScale.x, object_layer) == true ||
						Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f), voxel_mark.transform.TransformDirection(Vector3.forward), voxel_mark.transform.localScale.x, object_layer) == true ||
						Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f), voxel_mark.transform.TransformDirection(Vector3.back), voxel_mark.transform.localScale.x, object_layer) == true)
					{
						voxel_position = new Vector3((float)voxel_mark_dec.xd, (float)voxel_mark_dec.yd, (float)voxel_mark_dec.zd);
						
						if (Voxel_Material.Material_From_Object == true)
						{
							if (!voxel_material_dic.ContainsKey(voxel_position))
								voxel_material_dic[voxel_position] = new Dictionary<string, Vector2[]>();
							if (nr_mesh_triangles == 12)
							{
								ResetCorrections();
								if (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0), voxel_mark.transform.TransformDirection(Vector3.right), voxel_mark.transform.localScale.x, object_layer) == true)
								{
									voxel_material_dic[voxel_position]["R"] = new Vector2[4];
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+correction_y,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit1, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+correction_y,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit2, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;								
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,-Prefab_Voxel.transform.localScale.x/2+correction_y,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit3, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,-Prefab_Voxel.transform.localScale.x/2+correction_y,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit4, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;
									}
									ResetCorrections();
									voxel_material_dic[voxel_position]["R"][0] = ray_hit2.textureCoord;
									voxel_material_dic[voxel_position]["R"][1] = ray_hit1.textureCoord;
									voxel_material_dic[voxel_position]["R"][2] = ray_hit3.textureCoord;
									voxel_material_dic[voxel_position]["R"][3] = ray_hit4.textureCoord;
								}
								if (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0), voxel_mark.transform.TransformDirection(Vector3.left), voxel_mark.transform.localScale.x, object_layer) == true)
								{
									voxel_material_dic[voxel_position]["L"] = new Vector2[4];
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+correction_y,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit1, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+correction_y,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit2, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;	
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,-Prefab_Voxel.transform.localScale.x/2+correction_y,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit3, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,-Prefab_Voxel.transform.localScale.x/2+correction_y,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit4, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;
									}
									ResetCorrections();
									voxel_material_dic[voxel_position]["L"][0] = ray_hit1.textureCoord;
									voxel_material_dic[voxel_position]["L"][1] = ray_hit2.textureCoord;
									voxel_material_dic[voxel_position]["L"][2] = ray_hit4.textureCoord;
									voxel_material_dic[voxel_position]["L"][3] = ray_hit3.textureCoord;
								}
								if (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0), voxel_mark.transform.TransformDirection(Vector3.up), voxel_mark.transform.localScale.x, object_layer) == true)
								{
									voxel_material_dic[voxel_position]["U"] = new Vector2[4];
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,0,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit1, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,0,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit2, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;	
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,0,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit3, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,0,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit4, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;
									}
									ResetCorrections();
									voxel_material_dic[voxel_position]["U"][0] = ray_hit2.textureCoord;
									voxel_material_dic[voxel_position]["U"][1] = ray_hit4.textureCoord;
									voxel_material_dic[voxel_position]["U"][2] = ray_hit3.textureCoord;
									voxel_material_dic[voxel_position]["U"][3] = ray_hit1.textureCoord;
								}
								if (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0), voxel_mark.transform.TransformDirection(Vector3.down), voxel_mark.transform.localScale.x, object_layer) == true)
								{
									voxel_material_dic[voxel_position]["D"] = new Vector2[4];
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,0,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit1, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,0,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit2, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;	
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,0,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit3, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,0,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit4, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;
									}
									ResetCorrections();
									voxel_material_dic[voxel_position]["D"][0] = ray_hit1.textureCoord;
									voxel_material_dic[voxel_position]["D"][1] = ray_hit3.textureCoord;
									voxel_material_dic[voxel_position]["D"][2] = ray_hit4.textureCoord;
									voxel_material_dic[voxel_position]["D"][3] = ray_hit2.textureCoord;
								}
								if (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f), voxel_mark.transform.TransformDirection(Vector3.forward), voxel_mark.transform.localScale.x, object_layer) == true)
								{
									voxel_material_dic[voxel_position]["F"] = new Vector2[4];
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit1, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,-Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit2, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;										
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit3, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;									
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,-Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit4, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;						
									}
									ResetCorrections();
									voxel_material_dic[voxel_position]["F"][0] = ray_hit1.textureCoord;
									voxel_material_dic[voxel_position]["F"][1] = ray_hit3.textureCoord;
									voxel_material_dic[voxel_position]["F"][2] = ray_hit4.textureCoord;
									voxel_material_dic[voxel_position]["F"][3] = ray_hit2.textureCoord;
								}
								if (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f), voxel_mark.transform.TransformDirection(Vector3.back), voxel_mark.transform.localScale.x, object_layer) == true)
								{
									voxel_material_dic[voxel_position]["B"] = new Vector2[4];
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit1, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,-Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit2, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit3, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;	
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,-Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit4, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;								
									}
									ResetCorrections();
									voxel_material_dic[voxel_position]["B"][0] = ray_hit3.textureCoord;
									voxel_material_dic[voxel_position]["B"][1] = ray_hit1.textureCoord;
									voxel_material_dic[voxel_position]["B"][2] = ray_hit2.textureCoord;
									voxel_material_dic[voxel_position]["B"][3] = ray_hit4.textureCoord;
								}
							}
							if (nr_mesh_triangles == 48)
							{
								ResetCorrections();
								if (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0), voxel_mark.transform.TransformDirection(Vector3.right), voxel_mark.transform.localScale.x, object_layer) == true)
								{
									voxel_material_dic[voxel_position]["R"] = new Vector2[9];
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+correction_y,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit1, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+correction_y,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit2, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;								
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,-Prefab_Voxel.transform.localScale.x/2+correction_y,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit3, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,-Prefab_Voxel.transform.localScale.x/2+correction_y,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit4, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;
									}
									ResetCorrections();
									
									Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit5, voxel_mark.transform.localScale.x*10, object_layer);
									
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit6, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,-Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit7, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit8, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,0,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.right), out ray_hit9, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;								
									}
									ResetCorrections();
									
									voxel_material_dic[voxel_position]["R"][0] = ray_hit2.textureCoord;
									voxel_material_dic[voxel_position]["R"][1] = ray_hit1.textureCoord;
									voxel_material_dic[voxel_position]["R"][2] = ray_hit3.textureCoord;
									voxel_material_dic[voxel_position]["R"][3] = ray_hit4.textureCoord;
									voxel_material_dic[voxel_position]["R"][4] = ray_hit9.textureCoord;
									voxel_material_dic[voxel_position]["R"][5] = ray_hit5.textureCoord;
									voxel_material_dic[voxel_position]["R"][6] = ray_hit6.textureCoord;
									voxel_material_dic[voxel_position]["R"][7] = ray_hit7.textureCoord;
									voxel_material_dic[voxel_position]["R"][8] = ray_hit8.textureCoord;
								}
								if (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0), voxel_mark.transform.TransformDirection(Vector3.left), voxel_mark.transform.localScale.x, object_layer) == true)
								{
									voxel_material_dic[voxel_position]["L"] = new Vector2[9];
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+correction_y,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit1, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+correction_y,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit2, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;	
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,-Prefab_Voxel.transform.localScale.x/2+correction_y,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit3, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,-Prefab_Voxel.transform.localScale.x/2+correction_y,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit4, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;
									}
									ResetCorrections();
									
									Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit5, voxel_mark.transform.localScale.x*10, object_layer);
									
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit6, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,-Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit7, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit8, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(Prefab_Voxel.transform.localScale.x/2+0.01f,0,0)+new Vector3(0,0,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.left), out ray_hit9, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;								
									}
									ResetCorrections();
									
									voxel_material_dic[voxel_position]["L"][0] = ray_hit1.textureCoord;
									voxel_material_dic[voxel_position]["L"][1] = ray_hit2.textureCoord;
									voxel_material_dic[voxel_position]["L"][2] = ray_hit4.textureCoord;
									voxel_material_dic[voxel_position]["L"][3] = ray_hit3.textureCoord;
									voxel_material_dic[voxel_position]["L"][4] = ray_hit8.textureCoord;
									voxel_material_dic[voxel_position]["L"][5] = ray_hit5.textureCoord;
									voxel_material_dic[voxel_position]["L"][6] = ray_hit6.textureCoord;
									voxel_material_dic[voxel_position]["L"][7] = ray_hit7.textureCoord;
									voxel_material_dic[voxel_position]["L"][8] = ray_hit9.textureCoord;
								}
								if (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0), voxel_mark.transform.TransformDirection(Vector3.up), voxel_mark.transform.localScale.x, object_layer) == true)
								{
									voxel_material_dic[voxel_position]["U"] = new Vector2[9];
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,0,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit1, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,0,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit2, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;	
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,0,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit3, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,0,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit4, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;
									}
									ResetCorrections();
									
									Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.x/2+0.01f,0), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit5, voxel_mark.transform.localScale.x*10, object_layer);
									
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.x/2+0.01f,0)+new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit6, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.x/2+0.01f,0)+new Vector3(0,0,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit7, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.x/2+0.01f,0)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,0,0), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit8, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,Prefab_Voxel.transform.localScale.x/2+0.01f,0)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,0,0), voxel_mark.transform.TransformDirection(Vector3.up), out ray_hit9, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;								
									}
									ResetCorrections();
									
									voxel_material_dic[voxel_position]["U"][0] = ray_hit2.textureCoord;
									voxel_material_dic[voxel_position]["U"][1] = ray_hit4.textureCoord;
									voxel_material_dic[voxel_position]["U"][2] = ray_hit3.textureCoord;
									voxel_material_dic[voxel_position]["U"][3] = ray_hit1.textureCoord;
									voxel_material_dic[voxel_position]["U"][4] = ray_hit8.textureCoord;
									voxel_material_dic[voxel_position]["U"][5] = ray_hit5.textureCoord;
									voxel_material_dic[voxel_position]["U"][6] = ray_hit7.textureCoord;
									voxel_material_dic[voxel_position]["U"][7] = ray_hit6.textureCoord;
									voxel_material_dic[voxel_position]["U"][8] = ray_hit9.textureCoord;
								}
								if (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0), voxel_mark.transform.TransformDirection(Vector3.down), voxel_mark.transform.localScale.x, object_layer) == true)
								{
									voxel_material_dic[voxel_position]["D"] = new Vector2[9];
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,0,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit1, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,0,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit2, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;	
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,0,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit3, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.y/2+0.01f,0)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,0,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit4, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;
									}
									ResetCorrections();
									
									Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+0.01f,0), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit5, voxel_mark.transform.localScale.x*10, object_layer);
									
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+0.01f,0)+new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit6, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_z < 0)
											correction_z = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+0.01f,0)+new Vector3(0,0,-Prefab_Voxel.transform.localScale.x/2+correction_z), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit7, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_z > 0)
											correction_z = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_z +=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+0.01f,0)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,0,0), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit8, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+0.01f,0)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,0,0), voxel_mark.transform.TransformDirection(Vector3.down), out ray_hit9, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;								
									}
									ResetCorrections();
									
									voxel_material_dic[voxel_position]["D"][0] = ray_hit1.textureCoord;
									voxel_material_dic[voxel_position]["D"][1] = ray_hit3.textureCoord;
									voxel_material_dic[voxel_position]["D"][2] = ray_hit4.textureCoord;
									voxel_material_dic[voxel_position]["D"][3] = ray_hit2.textureCoord;
									voxel_material_dic[voxel_position]["D"][4] = ray_hit8.textureCoord;
									voxel_material_dic[voxel_position]["D"][5] = ray_hit5.textureCoord;
									voxel_material_dic[voxel_position]["D"][6] = ray_hit6.textureCoord;
									voxel_material_dic[voxel_position]["D"][7] = ray_hit7.textureCoord;
									voxel_material_dic[voxel_position]["D"][8] = ray_hit9.textureCoord;
								}
								if (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f), voxel_mark.transform.TransformDirection(Vector3.forward), voxel_mark.transform.localScale.x, object_layer) == true)
								{
									voxel_material_dic[voxel_position]["F"] = new Vector2[9];
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit1, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,-Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit2, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;										
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit3, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;									
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,-Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit4, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;						
									}
									ResetCorrections();
									
									Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+0.01f), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit5, voxel_mark.transform.localScale.x*10, object_layer);
									
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+0.01f)+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit6, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+0.01f)+new Vector3(0,-Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit7, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+0.01f)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,0,0), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit8, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position-new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+0.01f)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,0,0), voxel_mark.transform.TransformDirection(Vector3.forward), out ray_hit9, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;								
									}
									ResetCorrections();
									
									voxel_material_dic[voxel_position]["F"][0] = ray_hit1.textureCoord;
									voxel_material_dic[voxel_position]["F"][1] = ray_hit3.textureCoord;
									voxel_material_dic[voxel_position]["F"][2] = ray_hit4.textureCoord;
									voxel_material_dic[voxel_position]["F"][3] = ray_hit2.textureCoord;
									voxel_material_dic[voxel_position]["F"][4] = ray_hit8.textureCoord;
									voxel_material_dic[voxel_position]["F"][5] = ray_hit5.textureCoord;
									voxel_material_dic[voxel_position]["F"][6] = ray_hit6.textureCoord;
									voxel_material_dic[voxel_position]["F"][7] = ray_hit7.textureCoord;
									voxel_material_dic[voxel_position]["F"][8] = ray_hit9.textureCoord;
								}
								if (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f), voxel_mark.transform.TransformDirection(Vector3.back), voxel_mark.transform.localScale.x, object_layer) == true)
								{
									voxel_material_dic[voxel_position]["B"] = new Vector2[9];
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit1, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,-Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit2, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit3, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;	
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.z/2+0.01f)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,-Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit4, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;								
									}
									ResetCorrections();
									
									Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+0.01f), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit5, voxel_mark.transform.localScale.x*10, object_layer);
									
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+0.01f)+new Vector3(0,Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit6, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_y < 0)
											correction_y = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+0.01f)+new Vector3(0,-Prefab_Voxel.transform.localScale.x/2+correction_y,0), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit7, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_y > 0)
											correction_y = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_y +=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+0.01f)+new Vector3(Prefab_Voxel.transform.localScale.x/2+correction_x,0,0), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit8, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (Prefab_Voxel.transform.localScale.x/2+correction_x < 0)
											correction_x = -Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x -=0.01f;
									}
									ResetCorrections();
									while (Physics.Raycast(voxel_mark.transform.position+new Vector3(0,0,Prefab_Voxel.transform.localScale.x/2+0.01f)+new Vector3(-Prefab_Voxel.transform.localScale.x/2+correction_x,0,0), voxel_mark.transform.TransformDirection(Vector3.back), out ray_hit9, voxel_mark.transform.localScale.x*10, object_layer) == false)
									{
										if (-Prefab_Voxel.transform.localScale.x/2+correction_x > 0)
											correction_x = Prefab_Voxel.transform.localScale.x/2;
										else
											correction_x +=0.01f;								
									}
									
									voxel_material_dic[voxel_position]["B"][0] = ray_hit3.textureCoord;
									voxel_material_dic[voxel_position]["B"][1] = ray_hit1.textureCoord;
									voxel_material_dic[voxel_position]["B"][2] = ray_hit2.textureCoord;
									voxel_material_dic[voxel_position]["B"][3] = ray_hit4.textureCoord;
									voxel_material_dic[voxel_position]["B"][4] = ray_hit9.textureCoord;
									voxel_material_dic[voxel_position]["B"][5] = ray_hit5.textureCoord;
									voxel_material_dic[voxel_position]["B"][6] = ray_hit6.textureCoord;
									voxel_material_dic[voxel_position]["B"][7] = ray_hit7.textureCoord;
									voxel_material_dic[voxel_position]["B"][8] = ray_hit8.textureCoord;
								}
							}
						}
						
						voxel_positions.Add(voxel_position);
						
						if (Voxel_Material.Material_From_Object == true)
						{
							if (EditorUtility.DisplayCancelableProgressBar ("Voxelizing...","",0.2f))
								return;
						}
						else
						{
							if (EditorUtility.DisplayCancelableProgressBar ("Voxelizing...","",0.8f))
								return;
						}
						
						if (Fill_Object.Fill == true)
						{
							if (voxel_looking_for_pair == false)
							{
								voxel_looking_for_pair = true;
								voxel_to_pair = voxel_position;
							}
							else
							{
								if (voxel_position.x - voxel_to_pair.x > Prefab_Voxel.transform.localScale.x*1.5f)
								{
									voxel_pairs.Add(voxel_to_pair, voxel_position);
								}
								voxel_to_pair = voxel_position;
							}
						}
					}
				}
			}
		}
		DestroyImmediate(voxel_mark);
		
		if (Voxel_Material.Material_From_Object == true)
		{			
			System.IO.DirectoryInfo mesh_directory = new System.IO.DirectoryInfo("Assets/Voxelizer/Meshes");
			Regex r;
			MatchCollection trafienia;
			int nr = 0;
			foreach(System.IO.FileInfo mesh_file in mesh_directory.GetFiles())
			{
				r = new Regex(@"(\d+).");
				trafienia = r.Matches(mesh_file.Name);
				foreach (Match m in trafienia)
				{
					int.TryParse(m.Groups[1].Value, out nr);
					if (nr > mesh_id)
						mesh_id = nr+1;
				}
			}
			
			mesh_nr = mesh_id+1;
			#pragma warning disable 0219
			foreach (Vector3 pos in voxel_positions)
			{
				if (nr_mesh_triangles == 12)
				{
					AssetDatabase.CopyAsset("Assets/Voxelizer/Voxel_mesh_master_24x12.asset", "Assets/Voxelizer/Meshes/Voxel_mesh"+mesh_nr.ToString()+".asset");
				}
				else if (nr_mesh_triangles == 48)
				{
					AssetDatabase.CopyAsset("Assets/Voxelizer/Voxel_mesh_master_54x48.asset", "Assets/Voxelizer/Meshes/Voxel_mesh"+mesh_nr.ToString()+".asset");
				}
				else
				{
					Debug.Log("Voxelizer: Error. Unrecognized voxel mesh in Prefab_Voxel.");
					return;
				}
				
				mesh_nr++;
				if (EditorUtility.DisplayCancelableProgressBar ("Voxelizing...","",0.5f))
				{
					AssetDatabase.Refresh();
					AssetDatabase.SaveAssets();
					return;
				}
			}
			#pragma warning restore 0219
			mesh_nr = mesh_id+1;
			
			AssetDatabase.Refresh();
		}

		voxel_parent = new GameObject(Object_To_Voxelize.name + "_voxels");
		foreach (Vector3 pos in voxel_positions)
		{
			if (Fill_Object.Fill == true && Voxel_Material.Material_From_Object == true)
			{
				if (EditorUtility.DisplayCancelableProgressBar ("Voxelizing...","",0.75f))
				{
					AssetDatabase.SaveAssets();
					return;
				}
			}
			else if (Fill_Object.Fill == true && Voxel_Material.Material_From_Object == false)
			{
				if (EditorUtility.DisplayCancelableProgressBar ("Voxelizing...","",0.9f))
					return;				
			}
			else if (Fill_Object.Fill == false && Voxel_Material.Material_From_Object == false)
			{
				if (EditorUtility.DisplayCancelableProgressBar ("Voxelizing...","",1))
					return;				
			}
			else if (Fill_Object.Fill == false && Voxel_Material.Material_From_Object == true)
			{
				if (EditorUtility.DisplayCancelableProgressBar ("Voxelizing...","",0.8f))
				{
					AssetDatabase.SaveAssets();
					return;
				}
			}

			voxel = Instantiate(Prefab_Voxel, pos, Prefab_Voxel.transform.rotation) as GameObject;
			voxel.transform.parent = voxel_parent.transform;
			nr_voxels_created++;
			voxel.name = Object_To_Voxelize.name + "_voxel_" + nr_voxels_created.ToString();

			if (Voxel_Joints.CreateJoints == true)
			{
				voxel_objects.Add(voxel);
			}

			if (Voxel_Material.Material_From_Object == true)
			{
				voxel.renderer.material = Object_To_Voxelize.renderer.sharedMaterial;
				voxel.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath("Assets/Voxelizer/Meshes/Voxel_mesh"+mesh_nr.ToString()+".asset", typeof(Mesh)) as Mesh;
				if (nr_mesh_triangles == 12)
				{
					SetVoxelUV12(pos);
					uvs2 = new Vector2[]
					{
						u_upright, u_upleft, u_downleft, u_downright,
						r_upright, r_upleft, r_downleft, r_downright,
						b_upright, b_upleft, b_downleft, b_downright,
						f_upright, f_upleft, f_downleft, f_downright,
						l_upright, l_upleft, l_downleft, l_downright,
						d_upright, d_upleft, d_downleft, d_downright
					};
				}
				else if (nr_mesh_triangles == 48)
				{
					SetVoxelUV48(pos);	
					uvs2 = new Vector2[]
					{
						u_upright, u_upleft, u_downleft, u_downright,
						r_upright, r_upleft, r_downleft, r_downright,
						b_upright, b_upleft, b_downleft, b_downright,
						f_upright, f_upleft, f_downleft, f_downright,
						l_upright, l_upleft, l_downleft, l_downright,
						d_upright, d_upleft, d_downleft, d_downright,
						u_right, u_mid, u_up, u_down, u_left,
						r_right, r_mid, r_up, r_down, r_left,
						b_right, b_mid, b_up, b_down, b_left,
						f_right, f_mid, f_up, f_down, f_left,
						l_right, l_mid, l_up, l_down, l_left,
						d_right, d_mid, d_up, d_down, d_left
					};
				}
				voxel.GetComponent<MeshFilter>().sharedMesh.uv = uvs2;
				mesh_nr++;
			}
			else
			{
				// Here you can manipulate material of voxels at the edges
				voxel.renderer.material = Voxel_Material.Custom_Material;
			}
			if (Voxel_Components.Add_Rigidbody == true)
				voxel.AddComponent<Rigidbody>();
			if (Voxel_Components.Add_Collider == true)
				voxel.AddComponent<BoxCollider>();
		}
		
		if (Fill_Object.Fill == true)
		{
			foreach (KeyValuePair<Vector3, Vector3> p in voxel_pairs)
			{
				if (Voxel_Material.Material_From_Object == true)
				{
					if (EditorUtility.DisplayCancelableProgressBar ("Voxelizing...","",0.8f))
					{
						AssetDatabase.SaveAssets();
						return;
					}
				}
				else
				{
					if (EditorUtility.DisplayCancelableProgressBar ("Voxelizing...","",1))
						return;
				}
				for (int i=1; i<Mathf.Round((p.Value.x-p.Key.x)/Prefab_Voxel.transform.localScale.x); i++)
				{
					position_dec = new vector_decimal((decimal)p.Key.x+(decimal)i*(decimal)Prefab_Voxel.transform.localScale.x,
						(decimal) p.Key.y, (decimal) p.Key.z);
					position_dec = new vector_decimal(decimal.Round(position_dec.xd, 3),
													  decimal.Round(position_dec.yd, 3), 
													  decimal.Round(position_dec.zd, 3));
					
					if (fill_raycast[position_dec] == 6)
					{
						voxel = Instantiate(Prefab_Voxel, 
							new Vector3((float)position_dec.xd, (float)position_dec.yd, (float)position_dec.zd), 
							Prefab_Voxel.transform.rotation) as GameObject;
						voxel.transform.parent = voxel_parent.transform;
						nr_voxels_created++;
						voxel.name = Object_To_Voxelize.name + "_voxel_fill_" + nr_voxels_created.ToString();

						if (Voxel_Joints.CreateJoints == true)
						{
							voxel_objects.Add(voxel);
						}

						// Here you can manipulate material of voxels that fill the object
						voxel.renderer.material = Fill_Object.Fill_Material;
						
						if (Voxel_Components.Add_Rigidbody == true)
							voxel.AddComponent<Rigidbody>();
						if (Voxel_Components.Add_Collider == true)
							voxel.AddComponent<BoxCollider>();
					}
				}
			}
		}

		if (Voxel_Joints.CreateJoints == true)
		{
			float distance;
			
			foreach (GameObject g in voxel_objects)
			{
				foreach (GameObject gg in voxel_objects)
				{
					distance = Vector3.Distance(g.transform.position, gg.transform.position);
					
					if (distance < Prefab_Voxel.transform.localScale.x*1.1f && distance != 0)
					{
						if (!voxel_neighbours.ContainsKey(g))
						{
							voxel_neighbours.Add(g, new List<GameObject>());
						}
						voxel_neighbours[g].Add(gg);
					}
				}
			}
			
			foreach(KeyValuePair<GameObject, List<GameObject>> kv in voxel_neighbours)
			{
				foreach(GameObject l in kv.Value)
				{
					FixedJoint fj = l.AddComponent<FixedJoint>();
					if (Voxel_Joints.ConnectRigidbody == true)
						fj.connectedBody = kv.Key.rigidbody;
					if (Voxel_Joints.BreakForce != Mathf.Infinity)
						fj.breakForce = Voxel_Joints.BreakForce;
				}
			}
		}

		DestroyImmediate(bounding_box);
		if (Voxel_Material.Material_From_Object == true)
			AssetDatabase.SaveAssets();
		if (Info == true)
		{
			Debug.Log("Voxelizer summary\nObject: "+Object_To_Voxelize.gameObject.name+", Number of created voxels: "+nr_voxels_created+", Time: "+(DateTime.Now-d1));
		}
		if (Leave_Object == false)
			DestroyImmediate(Object_To_Voxelize.gameObject);
		EditorUtility.ClearProgressBar();
	}
	
	void ResetCorrections()
	{
		correction_x = 0;
		correction_y = 0;
		correction_z = 0;
	}
	
	void SetVoxelUV12(Vector3 pos1)
	{
		string [] keys1 = new string[voxel_material_dic[pos1].Keys.Count];
		voxel_material_dic[pos1].Keys.CopyTo(keys1,0);
		default_upright = voxel_material_dic[pos1][keys1[0]][0];
		default_upleft = voxel_material_dic[pos1][keys1[0]][1];
		default_downleft = voxel_material_dic[pos1][keys1[0]][2];
		default_downright = voxel_material_dic[pos1][keys1[0]][3];
		
		r_upright = l_upright = u_upright = d_upright = f_upright = b_upright = default_upright;
		r_upleft = l_upleft = u_upleft = d_upleft = f_upleft = b_upleft = default_upleft;
		r_downleft = l_downleft = u_downleft = d_downleft = f_downleft = b_downleft = default_downleft;
		r_downright = l_downright = u_downright = d_downright = f_downright = b_downright = default_downright;

		if (voxel_material_dic[pos1].ContainsKey("R"))
		{
			r_upright = voxel_material_dic[pos1]["R"][0];
			r_upleft = voxel_material_dic[pos1]["R"][1];
			r_downleft = voxel_material_dic[pos1]["R"][2];
			r_downright = voxel_material_dic[pos1]["R"][3];
		}
		if (voxel_material_dic[pos1].ContainsKey("L"))
		{
			l_upright = voxel_material_dic[pos1]["L"][0];
			l_upleft = voxel_material_dic[pos1]["L"][1];
			l_downleft = voxel_material_dic[pos1]["L"][2];
			l_downright = voxel_material_dic[pos1]["L"][3];
		}
		if (voxel_material_dic[pos1].ContainsKey("U"))
		{
			u_upright = voxel_material_dic[pos1]["U"][0];
			u_upleft = voxel_material_dic[pos1]["U"][1];
			u_downleft = voxel_material_dic[pos1]["U"][2];
			u_downright = voxel_material_dic[pos1]["U"][3];
		}
		if (voxel_material_dic[pos1].ContainsKey("D"))
		{
			d_upright = voxel_material_dic[pos1]["D"][0];
			d_upleft = voxel_material_dic[pos1]["D"][1];
			d_downleft = voxel_material_dic[pos1]["D"][2];
			d_downright = voxel_material_dic[pos1]["D"][3];
		}
		if (voxel_material_dic[pos1].ContainsKey("F"))
		{
			f_upright = voxel_material_dic[pos1]["F"][0];
			f_upleft = voxel_material_dic[pos1]["F"][1];
			f_downleft = voxel_material_dic[pos1]["F"][2];
			f_downright = voxel_material_dic[pos1]["F"][3];
		}
		if (voxel_material_dic[pos1].ContainsKey("B"))
		{
			b_upright = voxel_material_dic[pos1]["B"][0];
			b_upleft = voxel_material_dic[pos1]["B"][1];
			b_downleft = voxel_material_dic[pos1]["B"][2];
			b_downright = voxel_material_dic[pos1]["B"][3];
		}
	}
	
	void SetVoxelUV48(Vector3 pos1)
	{
		string [] keys1 = new string[voxel_material_dic[pos1].Keys.Count];
		voxel_material_dic[pos1].Keys.CopyTo(keys1,0);
		default_upright = voxel_material_dic[pos1][keys1[0]][0];
		default_upleft = voxel_material_dic[pos1][keys1[0]][1];
		default_downleft = voxel_material_dic[pos1][keys1[0]][2];
		default_downright = voxel_material_dic[pos1][keys1[0]][3];
		default_right = voxel_material_dic[pos1][keys1[0]][4];
		default_mid = voxel_material_dic[pos1][keys1[0]][5];
		default_up = voxel_material_dic[pos1][keys1[0]][6];
		default_down = voxel_material_dic[pos1][keys1[0]][7];
		default_left = voxel_material_dic[pos1][keys1[0]][8];
		
		r_upright = l_upright = u_upright = d_upright = f_upright = b_upright = default_upright;
		r_upleft = l_upleft = u_upleft = d_upleft = f_upleft = b_upleft = default_upleft;
		r_downleft = l_downleft = u_downleft = d_downleft = f_downleft = b_downleft = default_downleft;
		r_downright = l_downright = u_downright = d_downright = f_downright = b_downright = default_downright;
		r_right = l_right = u_right = d_right = f_right = b_right = default_right;
		r_mid = l_mid = u_mid = d_mid = f_mid = b_mid = default_mid;
		r_up = l_up = u_up = d_up = f_up = b_up = default_up;
		r_down = l_down = u_down = d_down = f_down = b_down = default_down;
		r_left = l_left = u_left = d_left = f_left = b_left = default_left;
		
		if (voxel_material_dic[pos1].ContainsKey("R"))
		{
			r_upright = voxel_material_dic[pos1]["R"][0];
			r_upleft = voxel_material_dic[pos1]["R"][1];
			r_downleft = voxel_material_dic[pos1]["R"][2];
			r_downright = voxel_material_dic[pos1]["R"][3];
			r_right = voxel_material_dic[pos1]["R"][4];
			r_mid = voxel_material_dic[pos1]["R"][5];
			r_up = voxel_material_dic[pos1]["R"][6];
			r_down = voxel_material_dic[pos1]["R"][7];
			r_left = voxel_material_dic[pos1]["R"][8];
		}
		if (voxel_material_dic[pos1].ContainsKey("L"))
		{
			l_upright = voxel_material_dic[pos1]["L"][0];
			l_upleft = voxel_material_dic[pos1]["L"][1];
			l_downleft = voxel_material_dic[pos1]["L"][2];
			l_downright = voxel_material_dic[pos1]["L"][3];
			l_right = voxel_material_dic[pos1]["L"][4];
			l_mid = voxel_material_dic[pos1]["L"][5];
			l_up = voxel_material_dic[pos1]["L"][6];
			l_down = voxel_material_dic[pos1]["L"][7];
			l_left = voxel_material_dic[pos1]["L"][8];
		}
		if (voxel_material_dic[pos1].ContainsKey("U"))
		{
			u_upright = voxel_material_dic[pos1]["U"][0];
			u_upleft = voxel_material_dic[pos1]["U"][1];
			u_downleft = voxel_material_dic[pos1]["U"][2];
			u_downright = voxel_material_dic[pos1]["U"][3];
			u_right = voxel_material_dic[pos1]["U"][4];
			u_mid = voxel_material_dic[pos1]["U"][5];
			u_up = voxel_material_dic[pos1]["U"][6];
			u_down = voxel_material_dic[pos1]["U"][7];
			u_left = voxel_material_dic[pos1]["U"][8];
		}
		if (voxel_material_dic[pos1].ContainsKey("D"))
		{
			d_upright = voxel_material_dic[pos1]["D"][0];
			d_upleft = voxel_material_dic[pos1]["D"][1];
			d_downleft = voxel_material_dic[pos1]["D"][2];
			d_downright = voxel_material_dic[pos1]["D"][3];
			d_right = voxel_material_dic[pos1]["D"][4];
			d_mid = voxel_material_dic[pos1]["D"][5];
			d_up = voxel_material_dic[pos1]["D"][6];
			d_down = voxel_material_dic[pos1]["D"][7];
			d_left = voxel_material_dic[pos1]["D"][8];
		}
		if (voxel_material_dic[pos1].ContainsKey("F"))
		{
			f_upright = voxel_material_dic[pos1]["F"][0];
			f_upleft = voxel_material_dic[pos1]["F"][1];
			f_downleft = voxel_material_dic[pos1]["F"][2];
			f_downright = voxel_material_dic[pos1]["F"][3];
			f_right = voxel_material_dic[pos1]["F"][4];
			f_mid = voxel_material_dic[pos1]["F"][5];
			f_up = voxel_material_dic[pos1]["F"][6];
			f_down = voxel_material_dic[pos1]["F"][7];
			f_left = voxel_material_dic[pos1]["F"][8];
		}
		if (voxel_material_dic[pos1].ContainsKey("B"))
		{
			b_upright = voxel_material_dic[pos1]["B"][0];
			b_upleft = voxel_material_dic[pos1]["B"][1];
			b_downleft = voxel_material_dic[pos1]["B"][2];
			b_downright = voxel_material_dic[pos1]["B"][3];
			b_right = voxel_material_dic[pos1]["B"][4];
			b_mid = voxel_material_dic[pos1]["B"][5];
			b_up = voxel_material_dic[pos1]["B"][6];
			b_down = voxel_material_dic[pos1]["B"][7];
			b_left = voxel_material_dic[pos1]["B"][8];
		}
	}
	
	bool CompareApproximate(Vector3 a, Vector3 b)
	{
	    if(!Mathf.Approximately(a.x, b.x))
	        return false;
	    if(!Mathf.Approximately(a.y, b.y))
	        return false;
	    if(!Mathf.Approximately(a.z, b.z))
	        return false;
	    return true;
	}
}
