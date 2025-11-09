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


    private void OnEnable()
    {
        if(GameManager.Instance.GeneratePos == null)
        {
            Debug.LogError("Error: Unexpect dispose position");
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        transform.Translate(Vector3.back * GameManager.Instance.mapSpeed * Time.deltaTime);
        if(transform.position.z <= GameManager.Instance.DisposePos.z)
            gameObject.SetActive(false);
    }
}
