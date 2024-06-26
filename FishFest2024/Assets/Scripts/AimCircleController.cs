using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class AimCircleController : MonoBehaviour
{
    private Animator _circleAnimator;
    private SpriteRenderer _sr;

    private LineRenderer _lrAim;

    private RotationConstraint _rc;

    [SerializeField]
    float aimLineStartingDistance = 0.25f;
    [SerializeField]
    float aimLineLength = 2f;

    private void Awake()
    {
        _circleAnimator = GetComponent<Animator>();
        _sr = GetComponent<SpriteRenderer>();
        _lrAim = GetComponentInChildren<LineRenderer>();
        _rc = GetComponent<RotationConstraint>();
    }

    private void Start()
    {
        _sr.enabled = false;
        _lrAim.enabled = false;

        _lrAim.positionCount = 2;
        _lrAim.SetPosition(0, Vector3.zero);

        ConstraintSource source = new ConstraintSource();
        source.sourceTransform = Camera.main.transform;
        source.weight = 1f;
        _rc.AddSource(source);
    }

    public void EnableAim()
    {
        _sr.enabled = true;
        _circleAnimator.Play("AimCircleExpand");

        _lrAim.enabled = true;
    }

    public void updateAimDirection(Vector2 direction)
    {
        _lrAim.SetPosition(0, direction * aimLineStartingDistance);
        _lrAim.SetPosition(1, direction * aimLineLength);
    }

    public void DisableAim()
    {
        _circleAnimator.Play("Default");
        _sr.enabled = false;
        _lrAim.enabled = false;
    }
}
