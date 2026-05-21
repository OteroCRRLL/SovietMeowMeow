using UnityEngine;
using UnityEngine.AI;

public static class ReplayPlaybackUtility
{
    public static GameObject SpawnVisualReplayClone(GameObject prefab, Vector3 worldPosition, Quaternion worldRotation)
    {
        GameObject clone = Object.Instantiate(prefab, worldPosition, worldRotation);
        clone.SetActive(false);

        if (clone.GetComponent<ReplayPlaybackStripper>() == null)
        {
            clone.AddComponent<ReplayPlaybackStripper>();
        }

        PrepareVisualPrefab(clone);
        clone.SetActive(true);
        return clone;
    }

    public static GameObject SpawnStaticLevelGeometry(GameObject prefab, Vector3 worldPosition)
    {
        if (prefab == null) return null;

        GameObject clone = Object.Instantiate(prefab, worldPosition, Quaternion.identity);
        PrepareStaticLevel(clone);
        EnableLevelVisuals(clone);
        return clone;
    }

    public static void PrepareVisualPrefab(GameObject clone)
    {
        foreach (NavMeshAgent agent in clone.GetComponentsInChildren<NavMeshAgent>(true))
        {
            Object.Destroy(agent);
        }

        foreach (CharacterController controller in clone.GetComponentsInChildren<CharacterController>(true))
        {
            controller.enabled = false;
        }

        foreach (Rigidbody rigidbody in clone.GetComponentsInChildren<Rigidbody>(true))
        {
            rigidbody.isKinematic = true;
            rigidbody.detectCollisions = false;
        }

        foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = false;
        }

        foreach (AudioListener listener in clone.GetComponentsInChildren<AudioListener>(true))
        {
            listener.enabled = false;
        }

        foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null) continue;
            if (behaviour is ReplayObject || behaviour is ReplayPlaybackStripper) continue;
            if (behaviour is Animator) continue;
            if (behaviour.GetComponent<Camera>() != null) continue;
            if (!ShouldDisableForPlayback(behaviour)) continue;
            behaviour.enabled = false;
        }
    }

    public static void PrepareStaticLevel(GameObject clone)
    {
        foreach (NavMeshAgent agent in clone.GetComponentsInChildren<NavMeshAgent>(true))
        {
            Object.Destroy(agent);
        }

        foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || !ShouldDisableForPlayback(behaviour)) continue;
            behaviour.enabled = false;
        }
    }

    private static void EnableLevelVisuals(GameObject levelRoot)
    {
        foreach (Terrain terrain in levelRoot.GetComponentsInChildren<Terrain>(true))
        {
            terrain.enabled = true;
            terrain.drawHeightmap = true;
            terrain.drawTreesAndFoliage = true;
            terrain.drawInstanced = true;
        }

        foreach (Renderer renderer in levelRoot.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null) renderer.enabled = true;
        }
    }

    private static bool ShouldDisableForPlayback(MonoBehaviour behaviour)
    {
        if (behaviour is ReplayObject || behaviour is ReplayPlaybackStripper) return false;

        System.Type type = behaviour.GetType();
        string fullName = type.FullName ?? type.Name;
        string name = type.Name;

        if (behaviour is Camera) return false;
        if (name.Contains("UniversalAdditionalCameraData")) return false;
        if (name.Contains("UniversalAdditionalLightData")) return false;
        if (fullName.StartsWith("UnityEngine.Rendering")) return false;
        if (fullName.StartsWith("Unity.Rendering")) return false;

        return true;
    }

    public static bool TryConfigureReplayCameras(GameObject playerClone, RenderTexture forwardRT, RenderTexture reverseRT,
        out Camera forwardCamera, out Camera reverseCamera)
    {
        forwardCamera = null;
        reverseCamera = null;

        if (playerClone == null) return false;

        Transform cameraPivot = FindChildRecursive(playerClone.transform, "CameraPivot");
        if (cameraPivot != null)
        {
            foreach (Camera cam in cameraPivot.GetComponentsInChildren<Camera>(true))
            {
                AssignCameraRole(cam, ref forwardCamera, ref reverseCamera);
            }
        }

        if (forwardCamera == null || reverseCamera == null)
        {
            foreach (Camera cam in playerClone.GetComponentsInChildren<Camera>(true))
            {
                AssignCameraRole(cam, ref forwardCamera, ref reverseCamera);
            }
        }

        if (forwardCamera == null)
        {
            forwardCamera = CreateFallbackCamera(playerClone.transform, cameraPivot, "ReplayForwardCamera", reverseCamera);
        }

        if (reverseCamera == null)
        {
            reverseCamera = CreateFallbackCamera(playerClone.transform, cameraPivot, "ReplayReverseCamera", forwardCamera);
        }

        ConfigureCameraOutput(forwardCamera, forwardRT, 30);
        ConfigureCameraOutput(reverseCamera, reverseRT, 31);

        return forwardCamera != null && reverseCamera != null;
    }

    private static Camera CreateFallbackCamera(Transform playerRoot, Transform cameraPivot, string cameraName, Camera template)
    {
        Transform parent = cameraPivot != null ? cameraPivot : playerRoot;
        GameObject cameraObject = new GameObject(cameraName);
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition = Vector3.zero;
        cameraObject.transform.localRotation = Quaternion.identity;

        Camera camera = cameraObject.AddComponent<Camera>();
        if (template != null)
        {
            camera.fieldOfView = template.fieldOfView;
            camera.nearClipPlane = template.nearClipPlane;
            camera.farClipPlane = template.farClipPlane;
        }
        else
        {
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
        }

        return camera;
    }

    private static void AssignCameraRole(Camera cam, ref Camera forwardCamera, ref Camera reverseCamera)
    {
        if (cam == null) return;

        string lowerName = cam.gameObject.name.ToLowerInvariant();
        if (lowerName.Contains("forward"))
        {
            forwardCamera = cam;
        }
        else if (lowerName.Contains("reverse") || lowerName.Contains("backward") || lowerName.Contains("back"))
        {
            reverseCamera = cam;
        }
    }

    private static void ConfigureCameraOutput(Camera cam, RenderTexture target, float depth)
    {
        if (cam == null || target == null) return;

        cam.gameObject.SetActive(true);
        cam.enabled = true;
        cam.tag = "Untagged";
        cam.depth = depth;
        cam.targetTexture = target;
        cam.clearFlags = CameraClearFlags.Skybox;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null) return null;
        if (parent.name == childName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }
}
