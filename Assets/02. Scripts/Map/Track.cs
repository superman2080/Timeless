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

    [HideInInspector] public float speed;
    [HideInInspector] public Vector3 disposePos;
    public TrackType trackType;


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
        transform.Translate(Vector3.back * speed * Time.deltaTime);
        if(transform.position.z <= disposePos.z)
            gameObject.SetActive(false);
    }
}
