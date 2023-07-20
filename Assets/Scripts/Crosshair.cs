﻿using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour {
    public Image aimPointReticle;
    public Image hitPointReticle;

    public float smoothTime = 0.2f;

    private Camera screenCamera;
    private RectTransform crossHairRectTransform;

    private Vector2 currentHitPointVelocity;
    private Vector2 targetPoint;

    private void Awake() {
        screenCamera = Camera.main;
        crossHairRectTransform = hitPointReticle.GetComponent<RectTransform>();
    }

    public void SetActiveCrosshair(bool active) {
        aimPointReticle.enabled = active;
        hitPointReticle.enabled = active;
    }
    //worldPoint 값을 targetPoint 값으로 변경.
    //그 위치로 crossHairRectTransform 이동.
    public void UpdatePosition(Vector3 worldPoint) {
        targetPoint = screenCamera.WorldToScreenPoint(worldPoint);
    }

    private void Update() {
        if (!hitPointReticle.enabled) return;        //활성화 된 순간에만 crossHairRectTransform 이동

        crossHairRectTransform.position = Vector2.SmoothDamp(crossHairRectTransform.position, targetPoint,
            ref currentHitPointVelocity, smoothTime);
    }
}