using UnityEngine;
using System.Collections;

public class ObjectRayTracingInfo : MonoBehaviour {

    // Three main coefficients.
    public float lambertCoeff = 1f;
    public float reflectiveCoeff = 0f;
    public float transparentCoeff = 0f;

    // Phong variables.
    public float phongCoeff = 1f;
    public float phongPower = 2f;

    // Blinn variables.
    public float blinnPhongCoeff = 1f;
    public float blinnPhongPower = 2f;

    public Color defaultColor = Color.gray;

    void Awake() {
        // Assign the material if non exists.
        if (!GetComponent<Renderer>().material.mainTexture) {
            GetComponent<Renderer>().material.color = defaultColor;
        }
    }
}