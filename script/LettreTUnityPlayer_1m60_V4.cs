using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity V5 pour la lettre T en LSF.
/// V5 = V4 + rotation poignet gauche ajustable.
/// </summary>
public class LettreTUnityPlayer_1m60_V4 : MonoBehaviour
{
    public enum TargetPositionSpace
    {
        LocalToTargetParent,
        World,
        RelativeToAvatarRoot
    }

    [Header("Cibles IK")]
    public Transform rightHandTarget;
    public Transform rightElbowHint;
    public Transform headTarget;

    [Header("Main gauche — rotation poignet uniquement")]
    public Transform leftHandTarget;
    [Tooltip("Rotation du poignet gauche (laisser le bras où il est)")]
    public Vector3 leftWristRotationEuler = new Vector3(0f, 0f, 0f);

    [Header("Référence avatar")]
    public Transform avatarRoot;

    [Header("Espace des positions")]
    public TargetPositionSpace targetPositionSpace = TargetPositionSpace.RelativeToAvatarRoot;

    [Header("Lecture")]
    public bool playOnStart = true;
    public float duration = 0.8f;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement position si besoin")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Poignet")]
    public bool applyWristRotation = true;
    public Vector3 wristRotationEuler = new Vector3(0f, 340f, -30f);
    public bool useLocalRotation = true;

    [Header("Doigts — activer")]
    public bool forceManualHandshape = true;

    [Header("Majeur — tendu vers le haut")]
    public Vector3 middlePh1 = new Vector3(0f, 0f, 0f);
    public Vector3 middlePh2 = new Vector3(0f, 0f, 0f);
    public Vector3 middlePh3 = new Vector3(0f, 0f, 0f);

    [Header("Annulaire — tendu vers le haut")]
    public Vector3 ringPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 ringPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 ringPh3 = new Vector3(0f, 0f, 0f);

    [Header("Auriculaire — tendu vers le haut")]
    public Vector3 pinkyPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 pinkyPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 pinkyPh3 = new Vector3(0f, 0f, 0f);

    [Header("Index — replié sur le pouce")]
    public Vector3 indexPh1 = new Vector3(60f, 0f, 0f);
    public Vector3 indexPh2 = new Vector3(80f, 0f, 0f);
    public Vector3 indexPh3 = new Vector3(60f, 0f, 0f);

    [Header("Pouce — replié sous l'index")]
    public Vector3 thumbPh1 = new Vector3(30f, 30f, 50f);
    public Vector3 thumbPh2 = new Vector3(40f, 0f, 20f);
    public Vector3 thumbPh3 = new Vector3(20f, 0f, 0f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly Vector3 handFinalPosition = new Vector3(0.12f, 1.05f, 0.16f);
    private readonly Vector3 elbowFinalPosition = new Vector3(0.32f, 0.82f, 0.10f);
    private readonly Vector3 gazeTarget = new Vector3(0.0f, 1.15f, 1.70f);
    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayLettreT();
    }

    public void PlayLettreT()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayLettreTCoroutine());
    }

    private void ApplyLeftWristRotation()
    {
        if (leftHandTarget != null)
            leftHandTarget.localRotation = Quaternion.Euler(leftWristRotationEuler);
    }

    private IEnumerator PlayLettreTCoroutine()
    {
        if (rightHandTarget == null) { Debug.LogError("[LETTRE T] RightHandTarget non assigné."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[LETTRE T] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[LETTRE T] Démarrage.");

        ApplyGaze();
        ApplyLeftWristRotation();

        SetTargetPosition(rightHandTarget, CorrectPosition(handFinalPosition));
        if (rightElbowHint != null)
            SetTargetPosition(rightElbowHint, CorrectPosition(elbowFinalPosition));

        yield return null;

        if (applyWristRotation) ApplyWristRot();
        if (forceManualHandshape) UpdateFingerRotations();

        if (showDebugLogs) Debug.Log("[LETTRE T] Position maintenue.");

        while (true)
        {
            if (forceManualHandshape) UpdateFingerRotations();
            ApplyLeftWristRotation();
            yield return null;
        }
    }

    private void UpdateFingerRotations()
    {
        if (avatarRoot == null) return;

        ApplyBone("mixamorig:RightHandMiddle1", Quaternion.Euler(middlePh1));
        ApplyBone("mixamorig:RightHandMiddle2", Quaternion.Euler(middlePh2));
        ApplyBone("mixamorig:RightHandMiddle3", Quaternion.Euler(middlePh3));

        ApplyBone("mixamorig:RightHandRing1", Quaternion.Euler(ringPh1));
        ApplyBone("mixamorig:RightHandRing2", Quaternion.Euler(ringPh2));
        ApplyBone("mixamorig:RightHandRing3", Quaternion.Euler(ringPh3));

        ApplyBone("mixamorig:RightHandPinky1", Quaternion.Euler(pinkyPh1));
        ApplyBone("mixamorig:RightHandPinky2", Quaternion.Euler(pinkyPh2));
        ApplyBone("mixamorig:RightHandPinky3", Quaternion.Euler(pinkyPh3));

        ApplyBone("mixamorig:RightHandIndex1", Quaternion.Euler(indexPh1));
        ApplyBone("mixamorig:RightHandIndex2", Quaternion.Euler(indexPh2));
        ApplyBone("mixamorig:RightHandIndex3", Quaternion.Euler(indexPh3));

        ApplyBone("mixamorig:RightHandThumb1", Quaternion.Euler(thumbPh1));
        ApplyBone("mixamorig:RightHandThumb2", Quaternion.Euler(thumbPh2));
        ApplyBone("mixamorig:RightHandThumb3", Quaternion.Euler(thumbPh3));
    }

    private void ApplyBone(string boneName, Quaternion rotation)
    {
        Transform bone = FindDeepChild(avatarRoot, boneName);
        if (bone != null) bone.localRotation = rotation;
    }

    private void ApplyWristRot()
    {
        if (rightHandTarget == null) return;
        Quaternion r = Quaternion.Euler(wristRotationEuler);
        if (useLocalRotation) rightHandTarget.localRotation = r;
        else rightHandTarget.rotation = r;
    }

    private void ApplyGaze()
    {
        if (headTarget == null) return;
        Vector3 gaze = CorrectPosition(gazeTarget);
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot != null)
            gaze = avatarRoot.TransformPoint(gaze);
        Vector3 direction = gaze - headTarget.position;
        if (direction.sqrMagnitude > 0.001f)
            headTarget.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

    private Vector3 CorrectPosition(Vector3 p) => p * positionScale + positionOffset;

    private void SetTargetPosition(Transform target, Vector3 position)
    {
        if (target == null) return;
        switch (targetPositionSpace)
        {
            case TargetPositionSpace.LocalToTargetParent:
                target.localPosition = position; break;
            case TargetPositionSpace.World:
                target.position = position; break;
            case TargetPositionSpace.RelativeToAvatarRoot:
                target.position = avatarRoot != null
                    ? avatarRoot.TransformPoint(position) : position;
                break;
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}