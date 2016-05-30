using UnityEngine;
using System.Collections;

public class RayTracer : MonoBehaviour {

    public float resolution = 1.0f;
    public int finalRecursiveIteration = 4;

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
        Color colour = Color.black;
        for (int x = 0; x < renderTexture.width; x++) {
            for (int y = 0; y < renderTexture.height; y++) {
                Vector3 rayPosition = new Vector3(x / resolution, y / resolution, 0);
                Ray ray = GetComponent<Camera>().ScreenPointToRay(rayPosition);
                //renderTexture.SetPixel(x, y, TraceColour());
            }
        }
    }

    //
    private void TraceColour(Ray ray, Color colour, int recursiveIteration) {

    }
}
