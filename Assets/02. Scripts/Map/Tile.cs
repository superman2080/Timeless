using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector3 TileSize
    {
        get
        {
            return gameObject.GetComponent<Renderer>().bounds.size;
        }
    }

    [HideInInspector] public float speed;
    [HideInInspector] public Vector3 disposePos;


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
