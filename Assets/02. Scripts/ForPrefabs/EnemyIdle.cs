using UnityEngine;
using System.Collections;


public class EnemyIdle : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public float speed = 1.0f;
    public float amplitude = 1.0f;
    
    void Start()
    {
        StartCoroutine(Idleling());
    }

    IEnumerator Idleling()
    {
        

        while (true)
        {
            // 좌우로 움직임
            float sinValue = Mathf.Sin(Time.time * speed) * amplitude;
            
            transform.position = new Vector3(transform.position.x , transform.position.y + sinValue, transform.position.z);
            yield return new WaitForSeconds(0.02f);
        }
    }
    

}
