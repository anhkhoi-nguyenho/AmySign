using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour le signe NON en LSF.
/// Index tendu, autres doigts repliés, pouce sur majeur.
/// Mouvement latéral droite-gauche-droite-gauche.
/// </summary>
public class NON : MonoBehaviour
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

    [Header("Référence avatar")]
    public Transform avatarRoot;

    [Header("Espace des positions")]
    public TargetPositionSpace targetPositionSpace = TargetPositionSpace.RelativeToAvatarRoot;

    [Header("Lecture")]
    public bool playOnStart = true;
    public float duration = 1.6f;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Poignet")]
    public bool applyWristRotation = true;
    public Vector3 wristRotationEuler = new Vector3(0f, 180f, 0f);
    public bool useLocalRotation = true;

    [Header("Doigts")]
    public bool forceManualHandshape = true;

    [Header("Index — tendu")]
    public Vector3 indexPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 indexPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 indexPh3 = new Vector3(0f, 0f, 0f);

    [Header("Majeur — replié")]
    public Vector3 middlePh1 = new Vector3(80f, 0f, 0f);
    public Vector3 middlePh2 = new Vector3(90f, 0f, 0f);
    public Vector3 middlePh3 = new Vector3(70f, 0f, 0f);

    [Header("Annulaire — replié")]
    public Vector3 ringPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 ringPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 ringPh3 = new Vector3(70f, 0f, 0f);

    [Header("Auriculaire — replié")]
    public Vector3 pinkyPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 pinkyPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 pinkyPh3 = new Vector3(70f, 0f, 0f);

    [Header("Pouce — posé sur majeur")]
    public Vector3 thumbPh1 = new Vector3(40f, 20f, 50f);
    public Vector3 thumbPh2 = new Vector3(40f, 0f, 30f);
    public Vector3 thumbPh3 = new Vector3(20f, 0f, 0f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    // ====================================================================
    // TRAJECTOIRE corrigée : Y=0.85 (devant ventre/bas poitrine)
    // ====================================================================
    private Vector3[] handPath = new Vector3[]
    {
        new Vector3(0.15f, 0.85f, 0.20f),  // centre
        new Vector3(0.25f, 0.85f, 0.20f),  // droite 1
        new Vector3(0.05f, 0.85f, 0.20f),  // gauche 1
        new Vector3(0.25f, 0.85f, 0.20f),  // droite 2
        new Vector3(0.05f, 0.85f, 0.20f),  // gauche 2
        new Vector3(0.15f, 0.85f, 0.20f)   // centre fin
    };

    private Vector3[] elbowPath = new Vector3[]
    {
        new Vector3(0.30f, 0.70f, 0.05f),
        new Vector3(0.30f, 0.70f, 0.05f),
        new Vector3(0.30f, 0.70f, 0.05f),
        new Vector3(0.30f, 0.70f, 0.05f),
        new Vector3(0.30f, 0.70f, 0.05f),
        new Vector3(0.30f, 0.70f, 0.05f)
    };

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayNon();
    }

    public void PlayNon()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayNonCoroutine());
    }

    private IEnumerator PlayNonCoroutine()
    {
        if (rightHandTarget == null) { Debug.LogError("[NON] RightHandTarget non assigné."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[NON] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[NON] Démarrage.");

        ApplyGaze();
        if (applyWristRotation) ApplyWristRot();
        if (forceManualHandshape) UpdateFingerRotations();

        int segmentCount = handPath.Length - 1;
        float safeSpeed = Mathf.Max(0.01f, globalSpeedMultiplier);
        float segmentDuration = duration / Mathf.Max(1, segmentCount) / safeSpeed;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 handStart = CorrectPosition(handPath[i]);
            Vector3 handEnd = CorrectPosition(handPath[i + 1]);
            Vector3 elbowStart = CorrectPosition(elbowPath[Mathf.Min(i, elbowPath.Length - 1)]);
            Vector3 elbowEnd = CorrectPosition(elbowPath[Mathf.Min(i + 1, elbowPath.Length - 1)]);

            float elapsed = 0f;
            while (elapsed < segmentDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / segmentDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                SetTargetPosition(rightHandTarget, Vector3.Lerp(handStart, handEnd, smoothT));
                if (rightElbowHint != null)
                    SetTargetPosition(rightElbowHint, Vector3.Lerp(elbowStart, elbowEnd, smoothT));
                if (applyWristRotation) ApplyWristRot();
                if (forceManualHandshape) UpdateFingerRotations();

                yield return null;
            }
        }

        Vector3 finalHand = CorrectPosition(handPath[handPath.Length - 1]);
        Vector3 finalElbow = CorrectPosition(elbowPath[elbowPath.Length - 1]);

        while (true)
        {
            SetTargetPosition(rightHandTarget, finalHand);
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, finalElbow);
            if (applyWristRotation) ApplyWristRot();
            if (forceManualHandshape) UpdateFingerRotations();
            yield return null;
        }
    }

    private void UpdateFingerRotations()
    {
        if (avatarRoot == null) return;

        ApplyBone("mixamorig:RightHandIndex1", Quaternion.Euler(indexPh1));
        ApplyBone("mixamorig:RightHandIndex2", Quaternion.Euler(indexPh2));
        ApplyBone("mixamorig:RightHandIndex3", Quaternion.Euler(indexPh3));

        ApplyBone("mixamorig:RightHandMiddle1", Quaternion.Euler(middlePh1));
        ApplyBone("mixamorig:RightHandMiddle2", Quaternion.Euler(middlePh2));
        ApplyBone("mixamorig:RightHandMiddle3", Quaternion.Euler(middlePh3));

        ApplyBone("mixamorig:RightHandRing1", Quaternion.Euler(ringPh1));
        ApplyBone("mixamorig:RightHandRing2", Quaternion.Euler(ringPh2));
        ApplyBone("mixamorig:RightHandRing3", Quaternion.Euler(ringPh3));

        ApplyBone("mixamorig:RightHandPinky1", Quaternion.Euler(pinkyPh1));
        ApplyBone("mixamorig:RightHandPinky2", Quaternion.Euler(pinkyPh2));
        ApplyBone("mixamorig:RightHandPinky3", Quaternion.Euler(pinkyPh3));

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
        if (parent == null) return null;
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}