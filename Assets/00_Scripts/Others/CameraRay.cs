using UnityEngine;

public class CameraRay : MonoBehaviour
{
    HeroHolder holder;
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var hit = Physics2D.Raycast(ray.origin, ray.direction);

            if(hit.collider != null)
            {
                holder?.HideRange();
                holder = hit.collider.GetComponent<HeroHolder>();
                holder?.ShowRange();
            } else
            {
                holder?.HideRange();
            }
        } 
        
        
    }
}
