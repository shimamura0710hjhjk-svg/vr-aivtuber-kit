using UnityEngine;

public class ModelInteractionController : MonoBehaviour
{
    [Header("Interaction")]
    public AITuberController aituberController;
    public Camera interactionCamera;
    public float headThreshold = 0.55f;
    public float bellyThreshold = 0.25f;
    public float maxRayDistance = 100f;

    private void Start()
    {
        if (interactionCamera == null)
        {
            interactionCamera = Camera.main;
        }

        if (aituberController == null)
        {
            Debug.LogWarning("AITuberController is not assigned on ModelInteractionController.");
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryHandlePointer(Input.mousePosition, "tap");
        }

        if (Input.GetMouseButtonDown(1))
        {
            TryHandlePointer(Input.mousePosition, "punch");
        }

        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                TryHandlePointer(touch.position, "tap");
            }
        }
    }

    private void TryHandlePointer(Vector2 screenPosition, string interactionType)
    {
        if (interactionCamera == null)
        {
            return;
        }

        Ray ray = interactionCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
        {
            if (hit.collider != null && (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform)))
            {
                string region = GetHitRegion(hit.point);
                NotifyInteraction(interactionType, region);
            }
        }
    }

    private string GetHitRegion(Vector3 worldHitPoint)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldHitPoint);
        if (localPoint.y > headThreshold)
        {
            return "head";
        }
        if (localPoint.y > bellyThreshold)
        {
            return "belly";
        }
        return "body";
    }

    private void NotifyInteraction(string interactionType, string region)
    {
        if (aituberController == null)
        {
            return;
        }

        if (interactionType == "punch")
        {
            aituberController.ReactToInteraction("punch", region);
            return;
        }

        if (region == "head" || region == "belly")
        {
            aituberController.ReactToInteraction("pet", region);
            return;
        }

        aituberController.ReactToInteraction("tap", region);
    }
}
