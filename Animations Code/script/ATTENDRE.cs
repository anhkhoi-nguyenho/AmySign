using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour le signe ATTENDRE en LSF.
/// Deux mains paume vers le ciel, doigts oscillent : ouvert → plié → ouvert → plié → ouvert.
/// Le pouce gauche est miroir du pouce droit (Y et Z inversés).
/// </summary>
public class ATTENDRE : MonoBehaviour
{
    public enum TargetPositionSpace
    {
        LocalToTargetParent,
        World,
        RelativeToAvatarRoot
    }

    [Header("Cibles IK droite")]
    public Transform rightHandTarget;
    public Transform rightElbowHint;

    [Header("Cibles IK gauche")]
    public Transform leftHandTarget;
    public Transform leftElbowHint;

    [Header("Tête / regard")]
    public Transform headTarget;

    [Header("Référence avatar")]
    public Transform avatarRoot;

    [Header("Espace des positions")]
    public TargetPositionSpace targetPositionSpace = TargetPositionSpace.RelativeToAvatarRoot;

    [Header("Lecture")]
    public bool playOnStart = true;
    public float duration = 1.2f;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Positions main droite")]
    public Vector3 rightHandFinalPosition = new Vector3(0.18f, 0.95f, 0.20f);
    public Vector3 rightElbowFinalPosition = new Vector3(0.28f, 0.78f, 0.05f);

    [Header("Positions main gauche")]
    public Vector3 leftHandFinalPosition = new Vector3(-0.18f, 0.95f, 0.20f);
    public Vector3 leftElbowFinalPosition = new Vector3(-0.28f, 0.78f, 0.05f);

    [Header("Poignets — paumes vers le ciel")]
    public bool applyWristRotation = true;
    public Vector3 rightWristRotationEuler = new Vector3(-90f, 0f, 0f);
    public Vector3 leftWristRotationEuler = new Vector3(-90f, 0f, 0f);
    public bool useLocalRotation = true;

    [Header("Oscillation doigts")]
    public bool forceManualHandshape = true;

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    // -----------------------------
    // POSE 1 : DOIGTS OUVERTS (commun aux 2 mains sauf pouce)
    // -----------------------------
    private Vector3 openIndex1 = new Vector3(0f, 0f, 0f);
    private Vector3 openIndex2 = new Vector3(0f, 0f, 0f);
    private Vector3 openIndex3 = new Vector3(0f, 0f, 0f);
    private Vector3 openMiddle1 = new Vector3(0f, 0f, 0f);
    private Vector3 openMiddle2 = new Vector3(0f, 0f, 0f);
    private Vector3 openMiddle3 = new Vector3(0f, 0f, 0f);
    private Vector3 openRing1 = new Vector3(0f, 0f, 0f);
    private Vector3 openRing2 = new Vector3(0f, 0f, 0f);
    private Vector3 openRing3 = new Vector3(0f, 0f, 0f);
    private Vector3 openPinky1 = new Vector3(0f, 0f, 0f);
    private Vector3 openPinky2 = new Vector3(0f, 0f, 0f);
    private Vector3 openPinky3 = new Vector3(0f, 0f, 0f);

    // Pouce DROITE — position FIXE (toujours pliée)
    private Vector3 openThumb1Right = new Vector3(30f, -10f, 40f);
    private Vector3 openThumb2Right = new Vector3(20f, 0f, 20f);
    private Vector3 openThumb3Right = new Vector3(0f, 0f, 0f);

    // Pouce GAUCHE — position FIXE (Y et Z inversés)
    private Vector3 openThumb1Left = new Vector3(30f, 10f, -40f);
    private Vector3 openThumb2Left = new Vector3(20f, 0f, -20f);
    private Vector3 openThumb3Left = new Vector3(0f, 0f, 0f);

    // -----------------------------
    // POSE 2 : DOIGTS PLIÉS
    // -----------------------------
    private Vector3 foldedIndex1 = new Vector3(90f, 0f, 0f);
    private Vector3 foldedIndex2 = new Vector3(0f, 0f, 0f);
    private Vector3 foldedIndex3 = new Vector3(0f, 0f, 0f);
    private Vector3 foldedMiddle1 = new Vector3(90f, 0f, 0f);
    private Vector3 foldedMiddle2 = new Vector3(0f, 0f, 0f);
    private Vector3 foldedMiddle3 = new Vector3(0f, 0f, 0f);
    private Vector3 foldedRing1 = new Vector3(90f, 0f, 0f);
    private Vector3 foldedRing2 = new Vector3(0f, 0f, 0f);
    private Vector3 foldedRing3 = new Vector3(0f, 0f, 0f);
    private Vector3 foldedPinky1 = new Vector3(90f, 0f, 0f);
    private Vector3 foldedPinky2 = new Vector3(0f, 0f, 0f);
    private Vector3 foldedPinky3 = new Vector3(0f, 0f, 0f);

    // Pouce DROITE — plié
    private Vector3 foldedThumb1Right = new Vector3(30f, -10f, 40f);
    private Vector3 foldedThumb2Right = new Vector3(20f, 0f, 20f);
    private Vector3 foldedThumb3Right = new Vector3(0f, 0f, 0f);

    // Pouce GAUCHE — plié (Y et Z inversés)
    private Vector3 foldedThumb1Left = new Vector3(30f, 10f, -40f);
    private Vector3 foldedThumb2Left = new Vector3(20f, 0f, -20f);
    private Vector3 foldedThumb3Left = new Vector3(0f, 0f, 0f);

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayAttendre();
    }

    public void PlayAttendre()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayAttendreCoroutine());
    }

    private IEnumerator PlayAttendreCoroutine()
    {
        if (rightHandTarget == null || leftHandTarget == null) { Debug.LogError("[ATTENDRE] Targets non assignés."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[ATTENDRE] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[ATTENDRE] Démarrage : ouvert → plié → ouvert → plié → ouvert.");

        ApplyGaze();
        ApplyArmPose();
        ApplyWristRotations();

        float safeSpeed = Mathf.Max(0.01f, globalSpeedMultiplier);
        float totalDuration = duration / safeSpeed;

        // 5 phases : ouvert → plié → ouvert → plié → ouvert
        float[] phases = new float[] { 0f, 1f, 0f, 1f, 0f };
        float phaseDuration = totalDuration / (phases.Length - 1);

        for (int i = 0; i < phases.Length - 1; i++)
        {
            float startPose = phases[i];
            float endPose = phases[i + 1];
            float elapsed = 0f;

            while (elapsed < phaseDuration)
            {
                elapsed += Time.deltaTime;
                ApplyArmPose();
                ApplyWristRotations();

                float t = Mathf.Clamp01(elapsed / phaseDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                float fingerPose = Mathf.Lerp(startPose, endPose, smoothT);

                if (forceManualHandshape) ApplyFingerPose(fingerPose);
                yield return null;
            }
        }

        while (true)
        {
            ApplyArmPose();
            ApplyWristRotations();
            if (forceManualHandshape) ApplyFingerPose(0f);
            yield return null;
        }
    }

    private void ApplyArmPose()
    {
        SetTargetPosition(rightHandTarget, CorrectPosition(rightHandFinalPosition));
        SetTargetPosition(leftHandTarget, CorrectPosition(leftHandFinalPosition));
        if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowFinalPosition));
        if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowFinalPosition));
    }

    private void ApplyWristRotations()
    {
        if (!applyWristRotation) return;

        if (rightHandTarget != null)
        {
            Quaternion r = Quaternion.Euler(rightWristRotationEuler);
            if (useLocalRotation) rightHandTarget.localRotation = r;
            else rightHandTarget.rotation = r;
        }
        if (leftHandTarget != null)
        {
            Quaternion l = Quaternion.Euler(leftWristRotationEuler);
            if (useLocalRotation) leftHandTarget.localRotation = l;
            else leftHandTarget.rotation = l;
        }
    }

    private void ApplyFingerPose(float t)
    {
        if (avatarRoot == null) return;

        // Doigts (identiques 2 mains)
        Vector3 index1 = Vector3.Lerp(openIndex1, foldedIndex1, t);
        Vector3 index2 = Vector3.Lerp(openIndex2, foldedIndex2, t);
        Vector3 index3 = Vector3.Lerp(openIndex3, foldedIndex3, t);
        Vector3 middle1 = Vector3.Lerp(openMiddle1, foldedMiddle1, t);
        Vector3 middle2 = Vector3.Lerp(openMiddle2, foldedMiddle2, t);
        Vector3 middle3 = Vector3.Lerp(openMiddle3, foldedMiddle3, t);
        Vector3 ring1 = Vector3.Lerp(openRing1, foldedRing1, t);
        Vector3 ring2 = Vector3.Lerp(openRing2, foldedRing2, t);
        Vector3 ring3 = Vector3.Lerp(openRing3, foldedRing3, t);
        Vector3 pinky1 = Vector3.Lerp(openPinky1, foldedPinky1, t);
        Vector3 pinky2 = Vector3.Lerp(openPinky2, foldedPinky2, t);
        Vector3 pinky3 = Vector3.Lerp(openPinky3, foldedPinky3, t);

        // Pouce DROITE
        Vector3 thumbR1 = Vector3.Lerp(openThumb1Right, foldedThumb1Right, t);
        Vector3 thumbR2 = Vector3.Lerp(openThumb2Right, foldedThumb2Right, t);
        Vector3 thumbR3 = Vector3.Lerp(openThumb3Right, foldedThumb3Right, t);

        // Pouce GAUCHE (miroir)
        Vector3 thumbL1 = Vector3.Lerp(openThumb1Left, foldedThumb1Left, t);
        Vector3 thumbL2 = Vector3.Lerp(openThumb2Left, foldedThumb2Left, t);
        Vector3 thumbL3 = Vector3.Lerp(openThumb3Left, foldedThumb3Left, t);

        ApplyHandFingers("Right",
            index1, index2, index3,
            middle1, middle2, middle3,
            ring1, ring2, ring3,
            pinky1, pinky2, pinky3,
            thumbR1, thumbR2, thumbR3);

        ApplyHandFingers("Left",
            index1, index2, index3,
            middle1, middle2, middle3,
            ring1, ring2, ring3,
            pinky1, pinky2, pinky3,
            thumbL1, thumbL2, thumbL3);
    }

    private void ApplyHandFingers(
        string side,
        Vector3 index1, Vector3 index2, Vector3 index3,
        Vector3 middle1, Vector3 middle2, Vector3 middle3,
        Vector3 ring1, Vector3 ring2, Vector3 ring3,
        Vector3 pinky1, Vector3 pinky2, Vector3 pinky3,
        Vector3 thumb1, Vector3 thumb2, Vector3 thumb3)
    {
        ApplyBone("mixamorig:" + side + "HandIndex1", Quaternion.Euler(index1));
        ApplyBone("mixamorig:" + side + "HandIndex2", Quaternion.Euler(index2));
        ApplyBone("mixamorig:" + side + "HandIndex3", Quaternion.Euler(index3));

        ApplyBone("mixamorig:" + side + "HandMiddle1", Quaternion.Euler(middle1));
        ApplyBone("mixamorig:" + side + "HandMiddle2", Quaternion.Euler(middle2));
        ApplyBone("mixamorig:" + side + "HandMiddle3", Quaternion.Euler(middle3));

        ApplyBone("mixamorig:" + side + "HandRing1", Quaternion.Euler(ring1));
        ApplyBone("mixamorig:" + side + "HandRing2", Quaternion.Euler(ring2));
        ApplyBone("mixamorig:" + side + "HandRing3", Quaternion.Euler(ring3));

        ApplyBone("mixamorig:" + side + "HandPinky1", Quaternion.Euler(pinky1));
        ApplyBone("mixamorig:" + side + "HandPinky2", Quaternion.Euler(pinky2));
        ApplyBone("mixamorig:" + side + "HandPinky3", Quaternion.Euler(pinky3));

        ApplyBone("mixamorig:" + side + "HandThumb1", Quaternion.Euler(thumb1));
        ApplyBone("mixamorig:" + side + "HandThumb2", Quaternion.Euler(thumb2));
        ApplyBone("mixamorig:" + side + "HandThumb3", Quaternion.Euler(thumb3));
    }

    private void ApplyBone(string boneName, Quaternion rotation)
    {
        Transform bone = FindDeepChild(avatarRoot, boneName);
        if (bone != null) bone.localRotation = rotation;
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