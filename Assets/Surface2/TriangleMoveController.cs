using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Klak.Motion;

public class TriangleMoveController: MonoBehaviour
{
    public float triangleBaseLength = 5.0f;
    public Transform shootingTarget;
    public float targetAdditionalDistance;
    private Vector3 center;
    private List<Vector3> points;
    private int pIndex;
    private Util.Timer smoothTimer;
    private SmoothFollow sf;
    private Vector3 shootingTargetPos;

    void Start()
    {
        //For triangle
        this.center = this.transform.position;
        float targetDistance = Mathf.Abs(this.transform.position.z - shootingTarget.position.z);
        this.points = new List<Vector3>() { Vector3.zero, Vector3.zero, Vector3.zero };
        this.shootingTargetPos = this.center;
        this.shootingTargetPos.z = center.z + targetDistance + targetAdditionalDistance;
        this.CalcTriangle();

        //For animation
        this.pIndex = 0;
        this.smoothTimer = new Util.Timer(0.5f);
        this.sf = this.GetComponent<SmoothFollow>();

        this.transform.position = center; //カメラのポジション
        this.transform.LookAt(shootingTargetPos); //カメラの向き
    }

    private void CalcTriangle()
    {
        float baseHalf = triangleBaseLength / 2.0f;
        float centerToBottomDistance = Mathf.Tan(30 * Mathf.Deg2Rad) * baseHalf;
        float centerToVertexDistance = Mathf.Cos(30 * Mathf.Deg2Rad) * baseHalf;
        this.points[0] = new Vector3(center.x - baseHalf, center.y - centerToBottomDistance, center.z); //左下
        this.points[1] = new Vector3(center.x + baseHalf, center.y - centerToBottomDistance, center.z); //右上
        this.points[2] = new Vector3(center.x, center.y + centerToVertexDistance, center.z); //真上
    }


    void Update()
    {
        //SmoothFolow setting
        //https://github.com/supertask/Nen2/blob/master/Assets/Nen/Sosakei/SoldierController.cs

        if (Input.GetKeyDown(KeyCode.M)) {
            this.StartMoving(this.points[pIndex]);
        }
        else if (Input.GetKeyDown(KeyCode.S)) {
            this.StartMoving(center);
        }

        this.Moving();
        this.StopMoving();
        this.smoothTimer.Clock();
    }

    public void StartMoving(Vector3 movingTargetPos)
    {
        if (this.smoothTimer.isStarted) { return; }

        this.sf.target.position = movingTargetPos;
        this.sf.target.LookAt(this.shootingTargetPos);
        this.sf.enabled = true;

        this.pIndex = (this.pIndex + 1) % 3; //0,1,2,0,1,2,..
        this.smoothTimer.Start();
    }

    public void Moving()
    {
        if (this.smoothTimer.isStarted) {
            this.transform.LookAt(this.shootingTargetPos);
        }
    }

    public void StopMoving()
    {
        if (this.smoothTimer.OnTime()) {
            this.sf.enabled = false;
        }
    }
}
