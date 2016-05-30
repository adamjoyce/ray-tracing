using UnityEngine;
using System.Collections;

public class ObjectRayTracingInfo : MonoBehaviour {

    // Three main coefficients.
    public float lambertCoefficient = 1f;
    public float reflectiveCoefficient = 0f;
    public float transparentCoefficient = 0f;

    // Phong variables.
    public float phongCoefficient = 1f;
    public float phongPower = 2f;

    // Blinn variables.
    public float blinnPhongCoefficient = 1f;
    public float blinnPhongPower = 2f;

    public Color defaultColor = Color.gray;

    void Awake() {
        // Assign the material if non exists.
        if (!GetComponent<Renderer>().material.mainTexture) {
            GetComponent<Renderer>().material.color = defaultColor;
        }
    }
}