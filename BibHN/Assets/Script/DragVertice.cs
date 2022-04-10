using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragVertice : MonoBehaviour
{
    private Vector3 mOffset;
    private float mZCoord;
    public DistortGO distort;

    private void OnMouseDown()
    {
        mZCoord = Camera.main.WorldToScreenPoint(gameObject.transform.position).z;
        mOffset = gameObject.transform.position - GetMouseAsWorldPoint();
        
    }

    private Vector3 GetMouseAsWorldPoint()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = mZCoord;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }

    private void OnMouseDrag()
    {
        transform.position = GetMouseAsWorldPoint() + mOffset;
        distort.MoveVertice(this.gameObject);
        distort.setColor(gameObject, true, false);
    }

    private void OnMouseUp()
    {
        distort.setColor(gameObject, false, false);
    }

    private void OnMouseEnter()
    {
        distort.setColor(gameObject, false, true);

    }

    private void OnMouseExit()
    {
        distort.setColor(gameObject, false, false);
    }
}
