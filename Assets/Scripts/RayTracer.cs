using UnityEngine;
using System.Collections;

public class RayTracer : MonoBehaviour {

    public float resolution = 1.0f;
    public float maximumRaycastDistance = 100.0f;
    public int maximumIterations = 4;

    private Light[] lights;
    private Texture2D renderTexture;

    // Setup the lights and render texture for the scene.
    private void Awake() {
        lights = FindObjectsOfType(typeof(Light)) as Light[];

        int textureWidth = (int)(Screen.width * resolution);
        int textureHeight = (int)(Screen.height * resolution);
        renderTexture = new Texture2D(textureWidth, textureHeight);
    }

	// Use this for initialization.
	void Start () {
        RayTrace();
	}
	
    // Cast rays from the camera to each pixel in the scene and set the render texture pixels accordingly.
    private void RayTrace() {
        Color defaultColour = Color.black;
        for (int x = 0; x < renderTexture.width; x++) {
            for (int y = 0; y < renderTexture.height; y++) {
                Vector3 rayPosition = new Vector3(x / resolution, y / resolution, 0);
                Ray ray = GetComponent<Camera>().ScreenPointToRay(rayPosition);
                renderTexture.SetPixel(x, y, DetermineColour(ray, defaultColour, 0));
            }
        }
        renderTexture.Apply();
    }

    // Determines the overall colour of the pixel at the location of the ray collision.
    private Color DetermineColour(Ray ray, Color positionColour, int currentIteration) {
        if (currentIteration < maximumIterations) {
            RaycastHit hit;

            // Check the ray intersects a collider.
            if (Physics.Raycast(ray, out hit, maximumRaycastDistance)) {
                // Determine the basic material colour of the pixel.
                Material objectMaterial = hit.collider.gameObject.GetComponent<Renderer>().material;
                if (objectMaterial.mainTexture) {
                    Texture2D mainTexture = objectMaterial.mainTexture as Texture2D;
                    positionColour += mainTexture.GetPixelBilinear(hit.textureCoord.x, hit.textureCoord.y);
                } else {
                    positionColour += objectMaterial.color;
                }

                ObjectRayTracingInfo objectInfo = hit.collider.gameObject.GetComponent<ObjectRayTracingInfo>();
                Vector3 hitPosition = hit.point + hit.normal * 0.0001f;

                positionColour += HandleLights(objectInfo, hitPosition, hit.normal, ray.direction);

                //
                if (objectInfo.reflectiveCoefficient > 0f) {
                    float reflet = 2.0f * Vector3.Dot(ray.direction, hit.normal);
                    Ray newRay = new Ray(hitPosition, ray.direction - reflet * hit.normal);
                    positionColour += objectInfo.reflectiveCoefficient * DetermineColour(newRay, positionColour, ++currentIteration);
                }

                //
                if (objectInfo.transparentCoefficient > 0f) {
                    Ray newRay = new Ray(hit.point - hit.normal * 0.0001f, ray.direction);
                    positionColour += objectInfo.transparentCoefficient * DetermineColour(newRay, positionColour, ++currentIteration);
                }
            }
        }
        return positionColour;
    }

    //
    private Color HandleLights(ObjectRayTracingInfo objectInfo, Vector3 rayHitPosition, Vector3 hitSurfaceNormal, Vector3 rayDirection) {
        Color lightColour = RenderSettings.ambientLight;

        for (int i = 0; i < lights.Length; i++) {
            if (lights[i].enabled) {
                // Additively ray trace the light.
                lightColour += LightTrace(objectInfo, lights[i], rayHitPosition, hitSurfaceNormal, rayDirection);
            }
        }

        return lightColour;
    }

    //
    private Color LightTrace(ObjectRayTracingInfo objectInfo, Light light, Vector3 rayHitPosition, Vector3 hitSurfaceNormal, Vector3 rayDirection) {
        Vector3 lightDirection;
        float lightDistance, lightContribution, dotProduct;

        if (light.type == LightType.Directional) {
            lightDirection = -light.transform.forward;
            lightContribution = 0;

            // Determine the angle that the light reflects of the surface.
            dotProduct = Vector3.Dot(rayDirection, hitSurfaceNormal);
            if (dotProduct > 0) {
                // Is the object in shadow?
                if (Physics.Raycast(rayHitPosition, lightDirection, maximumRaycastDistance)) {
                    return Color.black;
                }

                //
                if (objectInfo.lambertCoefficient > 0)
                    lightContribution += objectInfo.lambertCoefficient * dotProduct;

                if (objectInfo.reflectiveCoefficient > 0) {
                    // Phong method of reflection.
                    if (objectInfo.phongCoefficient > 0) {
                        float reflet = 2.0f * Vector3.Dot(rayDirection, hitSurfaceNormal);
                        Vector3 phongDirection = rayDirection - reflet * hitSurfaceNormal;
                        float phongTerm = Max(Vector3.Dot(phongDirection, rayDirection), 0f);
                        phongTerm = objectInfo.reflectiveCoefficient * Mathf.Pow(phongTerm, objectInfo.phongPower) * objectInfo.phongCoefficient;

                        lightContribution += phongTerm;
                    }

                    // Blinn-Phong method of reflection.
                    if (objectInfo.blinnPhongCoefficient > 0) {
                        Vector3 blinnDirection = -light.transform.forward - rayDirection;
                        float temp = Mathf.Sqrt(Vector3.Dot(blinnDirection, blinnDirection));
                        if (temp > 0f) {
                            blinnDirection = (1f / temp) * blinnDirection;
                            float blinnTerm = Max(Vector3.Dot(blinnDirection, hitSurfaceNormal), 0f);
                            blinnTerm = objectInfo.reflectiveCoefficient * Mathf.Pow(blinnTerm, objectInfo.blinnPhongPower) * objectInfo.blinnPhongCoefficient;

                            lightContribution += blinnTerm;
                        }
                    }
                }
            } //else if (light.type == )

            return light.color * light.intensity * lightContribution;
        }

        return Color.black;
    }

    //
    private void OnGUI() {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), renderTexture);
    }
    
    // Returns the maximum of the two given values.
    private float Max(float value1, float value2) {
        return value1 > value2 ? value1 : value2;
    }
}
