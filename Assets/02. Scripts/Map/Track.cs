using UnityEngine;

public class Track : MonoBehaviour
{
    public Renderer meshRenderer;
    public Vector3 TrackSize
    {
        get
        {
            return meshRenderer.bounds.size;
        }
    }

    public TrackType trackType;
    private Vector3 disposePos;

    private void Start()
    {
        disposePos = GameManager.Instance.positionLimits.disposePos;
    }

    private void OnEnable()
    {
        if(disposePos == null)
        {
            Debug.LogError("Error: Unexpect dispose position");
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        transform.Translate(Vector3.back * GameManager.Instance.mapSpeed * Time.deltaTime);
        if(transform.position.z <= disposePos.z)
            gameObject.SetActive(false);
    }
}
