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

                // Solve reflection pixel colour by casting a new reflection ray.
                if (objectInfo.reflectiveCoefficient > 0f) {
                    float reflet = 2.0f * Vector3.Dot(ray.direction, hit.normal);
                    Ray newRay = new Ray(hitPosition, ray.direction - reflet * hit.normal);
                    positionColour += objectInfo.reflectiveCoefficient * DetermineColour(newRay, positionColour, ++currentIteration);
                }

                // Solve transparent pixels by casting a ray from the hit point past the transparent object.
                if (objectInfo.transparentCoefficient > 0f) {
                    Ray newRay = new Ray(hit.point - hit.normal * 0.0001f, ray.direction);
                    positionColour += objectInfo.transparentCoefficient * DetermineColour(newRay, positionColour, ++currentIteration);
                }
            }
        }
        return positionColour;
    }

    // Handles the light reflections in the scene.
    private Color HandleLights(ObjectRayTracingInfo objectInfo, Vector3 rayHitPosition, Vector3 surfaceNormal, Vector3 rayDirection) {
        Color lightColour = RenderSettings.ambientLight;

        for (int i = 0; i < lights.Length; i++) {
            if (lights[i].enabled) {
                lightColour += LightTrace(objectInfo, lights[i], rayHitPosition, surfaceNormal, rayDirection);
            }
        }

        return lightColour;
    }

    // Handles Lambertian contribution to the lighting colour, as well as reflective contributions determined using Phong or Blinn-Phong methods.
    private Color LightTrace(ObjectRayTracingInfo objectInfo, Light light, Vector3 rayHitPosition, Vector3 surfaceNormal, Vector3 rayDirection) {
        Vector3 lightDirection;
        float lightDistance, lightContribution, dotDirectionNormal;

        if (light.type == LightType.Directional) {
            lightContribution = 0;
            lightDirection = -light.transform.forward;

            // Determine the angle that the light reflects of the surface.
            dotDirectionNormal = Vector3.Dot(lightDirection, surfaceNormal);
            if (dotDirectionNormal > 0) {
                // Returns the colour black if the hit position is in shadow.
                if (Physics.Raycast(rayHitPosition, lightDirection, maximumRaycastDistance)) {
                    return Color.black;
                }

                lightContribution += CalculateLightContribution(objectInfo, dotDirectionNormal, rayDirection, surfaceNormal, light);
            }

            return light.color * light.intensity * lightContribution;
        } 
        else if (light.type == LightType.Spot) {
            lightContribution = 0;
            lightDirection = (light.transform.position - rayHitPosition).normalized;
            dotDirectionNormal = Vector3.Dot(lightDirection, surfaceNormal);
            lightDistance = Vector3.Distance(rayHitPosition, light.transform.position);

            // Ensure the light is within range of the object and the angle of incidence positive.
            if (lightDistance < light.range && dotDirectionNormal > 0f) {
                float dotDirectionLight = Vector3.Dot(rayDirection, -light.transform.forward);

                // Ensure the object being lit falls within the spot light's radius.
                if (dotDirectionLight > (1 - light.spotAngle / 180f)) {
                    // Returns the colour black if the hit position is in shadow.
                    if (Physics.Raycast(rayHitPosition, lightDirection, maximumRaycastDistance)) {
                        return Color.black;
                    }

                    lightContribution += CalculateLightContribution(objectInfo, dotDirectionNormal, rayDirection, surfaceNormal, light);
                }
            }

            if (lightContribution == 0) {
                return Color.black;
            }

            return light.color * light.intensity * lightContribution;
        }

        return Color.black;
    }

    //
    private float CalculateLightContribution(ObjectRayTracingInfo objectInfo, float dotDirectionNormal, Vector3 rayDirection, Vector3 surfaceNormal, Light light) {
        float lightContribution = 0;

        if (objectInfo.lambertCoefficient > 0) {
            lightContribution += objectInfo.lambertCoefficient * dotDirectionNormal;
        }

        if (objectInfo.reflectiveCoefficient > 0) {
            if (objectInfo.phongCoefficient > 0) {
                lightContribution += Phong(objectInfo, rayDirection, surfaceNormal);
            }

            if (objectInfo.blinnPhongCoefficient > 0) {
                lightContribution += BlinnPhong(objectInfo, light, rayDirection, surfaceNormal);
            }
        }

        return lightContribution;
    }

    // Calculate the Phong reflection term.
    private float Phong(ObjectRayTracingInfo objectInfo, Vector3 rayDirection, Vector3 hitSurfaceNormal) {
        float reflet = 2.0f * Vector3.Dot(rayDirection, hitSurfaceNormal);
        Vector3 phongDirection = rayDirection - reflet * hitSurfaceNormal;
        float phongTerm = Max(Vector3.Dot(phongDirection, rayDirection), 0f);
        phongTerm = objectInfo.reflectiveCoefficient * Mathf.Pow(phongTerm, objectInfo.phongPower) * objectInfo.phongCoefficient;
        return phongTerm;
    }

    // Calculate the Blinn-Phong reflection term.
    private float BlinnPhong(ObjectRayTracingInfo objectInfo, Light light, Vector3 rayDirection, Vector3 hitSurfaceNormal) {
        Vector3 blinnDirection = -light.transform.forward - rayDirection;
        float temp = Mathf.Sqrt(Vector3.Dot(blinnDirection, blinnDirection));
        if (temp > 0f) {
            blinnDirection = (1f / temp) * blinnDirection;
            float blinnTerm = Max(Vector3.Dot(blinnDirection, hitSurfaceNormal), 0f);
            blinnTerm = objectInfo.reflectiveCoefficient * Mathf.Pow(blinnTerm, objectInfo.blinnPhongPower) * objectInfo.blinnPhongCoefficient;
            return blinnTerm;
        }
        return 0f;
    }

    // Returns the maximum of the two given values.
    private float Max(float value1, float value2) {
        return value1 > value2 ? value1 : value2;
    }

    // Draws the GUI, in this case the main render texture.
    private void OnGUI() {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), renderTexture);
    }
}
