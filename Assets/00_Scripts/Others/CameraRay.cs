using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraRay : NetworkBehaviour
{
    HeroHolder holder;
    HeroHolder colHolder;

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            MouseButtonDown();
        }

        if (Input.GetMouseButton(0)) {
            MouseButton();
        }

        if (Input.GetMouseButtonUp(0))
        {
            MouseButtonUp();
        }
    }

    private void MouseButtonDown()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hit = Physics2D.Raycast(ray.origin, ray.direction);

        if(holder != null) holder.HideRange();
        holder = null;

        if (hit.collider != null)
        {
            holder = hit.collider.GetComponent<HeroHolder>();
            if (NetworkManager.Singleton.LocalClientId != holder.clientId)
            {
                holder = null;
            }
            
        }
    }

    private void MouseButton()
    {
        if (holder == null)
            return;

        
        holder?.ShowCircle(true);

        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var hit = Physics2D.Raycast(ray.origin, ray.direction);
        if (hit.collider != null)
        {
            if(colHolder != null) colHolder.ShowSquare(false);

            colHolder = hit.collider.GetComponent<HeroHolder>();
            if(colHolder != null && colHolder != holder)
            {
                colHolder.ShowSquare(true);
            } else
            {
                colHolder = null;
            }
        }
    }

    private void MouseButtonUp()
    {
        if(colHolder == null)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var hit = Physics2D.Raycast(ray.origin, ray.direction);
            if (hit.collider != null)
            {
                var h = hit.collider.GetComponent<HeroHolder>();
                if (h != null && h == holder)
                {
                    if (holder != null)  holder.ShowRange();
                }
            }
        } else
        {
            if(holder != null)
            {
                Spawner.instance.SwapHoldersChanges(NetworkManager.Singleton.LocalClientId, holder.idx, colHolder.idx);
            }
        }

        if(holder != null) holder.ShowCircle(false);
        if(colHolder != null) colHolder.ShowSquare(false);

        colHolder = null;
    }
}
