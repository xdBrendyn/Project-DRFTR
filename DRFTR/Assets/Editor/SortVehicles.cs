using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Empty))]
public class SortVehicles : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Sort Cars"))
        {
            GameObject[] allCars = Resources.LoadAll<GameObject>("Packs\\Stylized Vehicles Pack\\Prefabs\\Combined\\WithoutLod");
            GameObject[] cars = allCars.Take(allCars.Length).ToArray();

            foreach (GameObject car in cars)
            {
                GameObject preset = Instantiate(Resources.Load<GameObject>("Prefabs\\CarPreset"));
                Transform WheelsParent = preset.transform.Find("Mesh").Find("Wheels");

                foreach (Transform child in car.transform)
                {
                    if (WheelsParent.Find("Wheel" + child.name))
                    {
                        Transform wheelParent = WheelsParent.Find("Wheel" + child.name);
                        wheelParent.localPosition = child.position;
                        GameObject wheel = Instantiate(child.gameObject, wheelParent);
                        wheel.name = child.name;
                        wheel.transform.SetAsFirstSibling();

                        Material outlineMat = Resources.Load<Material>("Materials\\Outline");
                        Material[] materials = wheel.GetComponent<MeshRenderer>().sharedMaterials;
                        Material[] newMaterials = new Material[materials.Length + 1];

                        for (int i = 0; i < materials.Length; i++)
                        {
                            newMaterials[i] = materials[i];
                        }

                        newMaterials[materials.Length] = outlineMat;
                        wheel.GetComponent<MeshRenderer>().sharedMaterials = newMaterials;
                        wheel.transform.localPosition = Vector3.zero;

                        wheelParent.GetChild(1).transform.localPosition = new Vector3(0, -wheelParent.transform.localPosition.y, 0);
                    }
                    else
                    {
                        GameObject body = Instantiate(child.gameObject, preset.transform.Find("Mesh"));
                        preset.name = child.name;
                        body.name = "Body";

                        GameObject outline = new GameObject();
                        outline.transform.SetParent(body.transform);
                        outline.transform.localPosition = Vector3.zero;
                        outline.name = "outline";

                        MeshCollider collider = body.AddComponent<MeshCollider>();

                        collider.convex = true;

                        MeshRenderer meshRenderer = outline.AddComponent<MeshRenderer>();
                        MeshFilter meshFilter = outline.AddComponent<MeshFilter>();

                        meshRenderer.sharedMaterial = Resources.Load<Material>("Materials\\Outline");
                        meshFilter.sharedMesh = outline.transform.parent.GetComponent<MeshFilter>().sharedMesh;

                        preset.GetComponent<VehicleController>().BodyMesh = body.transform;
                    }
                }
            }
        }
    }
}