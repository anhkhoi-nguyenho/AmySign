using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour le signe HABITER en LSF.
/// Deux mains symétriques devant le torse, paumes vers la caméra.
/// Doigts passent de tendus ouverts à pince fermée pendant que les mains avancent et baissent.
/// Doigts droits et gauches contrôlables indépendamment.
/// </summary>
public class HABITER : MonoBehaviour
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
    public float holdOpenDuration = 0.6f;
    public float transitionDuration = 0.3f;
    public float holdPinchDuration = 0.6f;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Positions main droite")]
    public Vector3 rightHandStart = new Vector3(0.12f, 1.10f, 0.20f);
    public Vector3 rightHandEnd = new Vector3(0.12f, 1.00f, 0.30f);
    public Vector3 rightElbowPos = new Vector3(0.32f, 0.85f, 0.05f);

    [Header("Positions main gauche")]
    public Vector3 leftHandStart = new Vector3(-0.12f, 1.10f, 0.20f);
    public Vector3 leftHandEnd = new Vector3(-0.12f, 1.00f, 0.30f);
    public Vector3 leftElbowPos = new Vector3(-0.32f, 0.85f, 0.05f);

    [Header("Poignets")]
    public bool applyWristRotation = true;
    public Vector3 rightWristEuler = new Vector3(0f, 0f, 0f);
    public Vector3 leftWristEuler = new Vector3(0f, 0f, 0f);
    public bool useLocalRotation = true;

    [Header("Doigts")]
    public bool forceManualHandshape = true;

    // ============ MAIN DROITE — POSE OUVERTE ============
    [Header("DROITE OUVERT — Index")]
    public Vector3 openRightIndexPh1 = new Vector3(-10f, -15f, 0f);
    public Vector3 openRightIndexPh2 = new Vector3(-5f, 0f, 0f);
    public Vector3 openRightIndexPh3 = new Vector3(0f, 0f, 0f);

    [Header("DROITE OUVERT — Majeur")]
    public Vector3 openRightMiddlePh1 = new Vector3(-10f, -5f, 0f);
    public Vector3 openRightMiddlePh2 = new Vector3(-5f, 0f, 0f);
    public Vector3 openRightMiddlePh3 = new Vector3(0f, 0f, 0f);

    [Header("DROITE OUVERT — Annulaire")]
    public Vector3 openRightRingPh1 = new Vector3(-10f, 5f, 0f);
    public Vector3 openRightRingPh2 = new Vector3(-5f, 0f, 0f);
    public Vector3 openRightRingPh3 = new Vector3(0f, 0f, 0f);

    [Header("DROITE OUVERT — Auriculaire")]
    public Vector3 openRightPinkyPh1 = new Vector3(-10f, 15f, 0f);
    public Vector3 openRightPinkyPh2 = new Vector3(-5f, 0f, 0f);
    public Vector3 openRightPinkyPh3 = new Vector3(0f, 0f, 0f);

    [Header("DROITE OUVERT — Pouce")]
    public Vector3 openRightThumbPh1 = new Vector3(0f, -25f, 30f);
    public Vector3 openRightThumbPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 openRightThumbPh3 = new Vector3(0f, 0f, 0f);

    // ============ MAIN DROITE — POSE PINCE ============
    [Header("DROITE PINCE — Index")]
    public Vector3 pinchRightIndexPh1 = new Vector3(45f, 5f, 0f);
    public Vector3 pinchRightIndexPh2 = new Vector3(45f, 0f, 0f);
    public Vector3 pinchRightIndexPh3 = new Vector3(30f, 0f, 0f);

    [Header("DROITE PINCE — Majeur")]
    public Vector3 pinchRightMiddlePh1 = new Vector3(45f, 0f, 0f);
    public Vector3 pinchRightMiddlePh2 = new Vector3(45f, 0f, 0f);
    public Vector3 pinchRightMiddlePh3 = new Vector3(30f, 0f, 0f);

    [Header("DROITE PINCE — Annulaire")]
    public Vector3 pinchRightRingPh1 = new Vector3(45f, -3f, 0f);
    public Vector3 pinchRightRingPh2 = new Vector3(45f, 0f, 0f);
    public Vector3 pinchRightRingPh3 = new Vector3(30f, 0f, 0f);

    [Header("DROITE PINCE — Auriculaire")]
    public Vector3 pinchRightPinkyPh1 = new Vector3(45f, -6f, 0f);
    public Vector3 pinchRightPinkyPh2 = new Vector3(45f, 0f, 0f);
    public Vector3 pinchRightPinkyPh3 = new Vector3(30f, 0f, 0f);

    [Header("DROITE PINCE — Pouce")]
    public Vector3 pinchRightThumbPh1 = new Vector3(25f, 20f, 50f);
    public Vector3 pinchRightThumbPh2 = new Vector3(30f, 0f, 25f);
    public Vector3 pinchRightThumbPh3 = new Vector3(15f, 0f, 0f);

    // ============ MAIN GAUCHE — POSE OUVERTE ============
    [Header("GAUCHE OUVERT — Index")]
    public Vector3 openLeftIndexPh1 = new Vector3(-10f, -15f, 0f);
    public Vector3 openLeftIndexPh2 = new Vector3(-5f, 0f, 0f);
    public Vector3 openLeftIndexPh3 = new Vector3(0f, 0f, 0f);

    [Header("GAUCHE OUVERT — Majeur")]
    public Vector3 openLeftMiddlePh1 = new Vector3(-10f, -5f, 0f);
    public Vector3 openLeftMiddlePh2 = new Vector3(-5f, 0f, 0f);
    public Vector3 openLeftMiddlePh3 = new Vector3(0f, 0f, 0f);

    [Header("GAUCHE OUVERT — Annulaire")]
    public Vector3 openLeftRingPh1 = new Vector3(-10f, 5f, 0f);
    public Vector3 openLeftRingPh2 = new Vector3(-5f, 0f, 0f);
    public Vector3 openLeftRingPh3 = new Vector3(0f, 0f, 0f);

    [Header("GAUCHE OUVERT — Auriculaire")]
    public Vector3 openLeftPinkyPh1 = new Vector3(-10f, 15f, 0f);
    public Vector3 openLeftPinkyPh2 = new Vector3(-5f, 0f, 0f);
    public Vector3 openLeftPinkyPh3 = new Vector3(0f, 0f, 0f);

    [Header("GAUCHE OUVERT — Pouce")]
    public Vector3 openLeftThumbPh1 = new Vector3(0f, -25f, 30f);
    public Vector3 openLeftThumbPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 openLeftThumbPh3 = new Vector3(0f, 0f, 0f);

    // ============ MAIN GAUCHE — POSE PINCE ============
    [Header("GAUCHE PINCE — Index")]
    public Vector3 pinchLeftIndexPh1 = new Vector3(45f, 5f, 0f);
    public Vector3 pinchLeftIndexPh2 = new Vector3(45f, 0f, 0f);
    public Vector3 pinchLeftIndexPh3 = new Vector3(30f, 0f, 0f);

    [Header("GAUCHE PINCE — Majeur")]
    public Vector3 pinchLeftMiddlePh1 = new Vector3(45f, 0f, 0f);
    public Vector3 pinchLeftMiddlePh2 = new Vector3(45f, 0f, 0f);
    public Vector3 pinchLeftMiddlePh3 = new Vector3(30f, 0f, 0f);

    [Header("GAUCHE PINCE — Annulaire")]
    public Vector3 pinchLeftRingPh1 = new Vector3(45f, -3f, 0f);
    public Vector3 pinchLeftRingPh2 = new Vector3(45f, 0f, 0f);
    public Vector3 pinchLeftRingPh3 = new Vector3(30f, 0f, 0f);

    [Header("GAUCHE PINCE — Auriculaire")]
    public Vector3 pinchLeftPinkyPh1 = new Vector3(45f, -6f, 0f);
    public Vector3 pinchLeftPinkyPh2 = new Vector3(45f, 0f, 0f);
    public Vector3 pinchLeftPinkyPh3 = new Vector3(30f, 0f, 0f);

    [Header("GAUCHE PINCE — Pouce")]
    public Vector3 pinchLeftThumbPh1 = new Vector3(25f, 20f, 50f);
    public Vector3 pinchLeftThumbPh2 = new Vector3(30f, 0f, 25f);
    public Vector3 pinchLeftThumbPh3 = new Vector3(15f, 0f, 0f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;
    private float fingerPose = 0f;

    private void Start()
    {
        if (playOnStart) PlayHabiter();
    }

    public void PlayHabiter()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayHabiterCoroutine());
    }

    private IEnumerator PlayHabiterCoroutine()
    {
        if (rightHandTarget == null || leftHandTarget == null) { Debug.LogError("[HABITER] Targets non assignés."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[HABITER] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[HABITER] Démarrage.");

        ApplyGaze();
        float safeSpeed = Mathf.Max(0.01f, globalSpeedMultiplier);

        // PHASE 1 : maintien position ouverte
        if (showDebugLogs) Debug.Log("[HABITER] Phase 1 : maintien position ouverte.");
        fingerPose = 0f;
        float elapsed = 0f;
        float realHoldOpen = holdOpenDuration / safeSpeed;
        while (elapsed < realHoldOpen)
        {
            elapsed += Time.deltaTime;
            SetTargetPosition(rightHandTarget, CorrectPosition(rightHandStart));
            SetTargetPosition(leftHandTarget, CorrectPosition(leftHandStart));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));
            ApplyWristRotations();
            if (forceManualHandshape) UpdateFingerRotations();
            yield return null;
        }

        // PHASE 2 : transition rapide
        if (showDebugLogs) Debug.Log("[HABITER] Phase 2 : transition rapide vers pince.");
        elapsed = 0f;
        float realTransition = transitionDuration / safeSpeed;
        while (elapsed < realTransition)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / realTransition);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentRight = Vector3.Lerp(rightHandStart, rightHandEnd, smoothT);
            Vector3 currentLeft = Vector3.Lerp(leftHandStart, leftHandEnd, smoothT);

            SetTargetPosition(rightHandTarget, CorrectPosition(currentRight));
            SetTargetPosition(leftHandTarget, CorrectPosition(currentLeft));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));
            ApplyWristRotations();

            fingerPose = smoothT;
            if (forceManualHandshape) UpdateFingerRotations();

            yield return null;
        }

        // PHASE 3 : maintien position pince
        if (showDebugLogs) Debug.Log("[HABITER] Phase 3 : maintien position pince.");
        fingerPose = 1f;
        elapsed = 0f;
        float realHoldPinch = holdPinchDuration / safeSpeed;
        while (elapsed < realHoldPinch)
        {
            elapsed += Time.deltaTime;
            SetTargetPosition(rightHandTarget, CorrectPosition(rightHandEnd));
            SetTargetPosition(leftHandTarget, CorrectPosition(leftHandEnd));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));
            ApplyWristRotations();
            if (forceManualHandshape) UpdateFingerRotations();
            yield return null;
        }

        while (true)
        {
            SetTargetPosition(rightHandTarget, CorrectPosition(rightHandEnd));
            SetTargetPosition(leftHandTarget, CorrectPosition(leftHandEnd));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));
            ApplyWristRotations();
            if (forceManualHandshape) UpdateFingerRotations();
            yield return null;
        }
    }

    private void ApplyWristRotations()
    {
        if (!applyWristRotation) return;

        if (rightHandTarget != null)
        {
            Quaternion r = Quaternion.Euler(rightWristEuler);
            if (useLocalRotation) rightHandTarget.localRotation = r;
            else                  rightHandTarget.rotation = r;
        }
        if (leftHandTarget != null)
        {
            Quaternion l = Quaternion.Euler(leftWristEuler);
            if (useLocalRotation) leftHandTarget.localRotation = l;
            else                  leftHandTarget.rotation = l;
        }
    }

    private void UpdateFingerRotations()
    {
        if (avatarRoot == null) return;
        float t = fingerPose;

        // ============ MAIN DROITE ============
        ApplyBone("mixamorig:RightHandIndex1", Quaternion.Euler(Vector3.Lerp(openRightIndexPh1, pinchRightIndexPh1, t)));
        ApplyBone("mixamorig:RightHandIndex2", Quaternion.Euler(Vector3.Lerp(openRightIndexPh2, pinchRightIndexPh2, t)));
        ApplyBone("mixamorig:RightHandIndex3", Quaternion.Euler(Vector3.Lerp(openRightIndexPh3, pinchRightIndexPh3, t)));

        ApplyBone("mixamorig:RightHandMiddle1", Quaternion.Euler(Vector3.Lerp(openRightMiddlePh1, pinchRightMiddlePh1, t)));
        ApplyBone("mixamorig:RightHandMiddle2", Quaternion.Euler(Vector3.Lerp(openRightMiddlePh2, pinchRightMiddlePh2, t)));
        ApplyBone("mixamorig:RightHandMiddle3", Quaternion.Euler(Vector3.Lerp(openRightMiddlePh3, pinchRightMiddlePh3, t)));

        ApplyBone("mixamorig:RightHandRing1", Quaternion.Euler(Vector3.Lerp(openRightRingPh1, pinchRightRingPh1, t)));
        ApplyBone("mixamorig:RightHandRing2", Quaternion.Euler(Vector3.Lerp(openRightRingPh2, pinchRightRingPh2, t)));
        ApplyBone("mixamorig:RightHandRing3", Quaternion.Euler(Vector3.Lerp(openRightRingPh3, pinchRightRingPh3, t)));

        ApplyBone("mixamorig:RightHandPinky1", Quaternion.Euler(Vector3.Lerp(openRightPinkyPh1, pinchRightPinkyPh1, t)));
        ApplyBone("mixamorig:RightHandPinky2", Quaternion.Euler(Vector3.Lerp(openRightPinkyPh2, pinchRightPinkyPh2, t)));
        ApplyBone("mixamorig:RightHandPinky3", Quaternion.Euler(Vector3.Lerp(openRightPinkyPh3, pinchRightPinkyPh3, t)));

        ApplyBone("mixamorig:RightHandThumb1", Quaternion.Euler(Vector3.Lerp(openRightThumbPh1, pinchRightThumbPh1, t)));
        ApplyBone("mixamorig:RightHandThumb2", Quaternion.Euler(Vector3.Lerp(openRightThumbPh2, pinchRightThumbPh2, t)));
        ApplyBone("mixamorig:RightHandThumb3", Quaternion.Euler(Vector3.Lerp(openRightThumbPh3, pinchRightThumbPh3, t)));

        // ============ MAIN GAUCHE ============
        ApplyBone("mixamorig:LeftHandIndex1", Quaternion.Euler(Vector3.Lerp(openLeftIndexPh1, pinchLeftIndexPh1, t)));
        ApplyBone("mixamorig:LeftHandIndex2", Quaternion.Euler(Vector3.Lerp(openLeftIndexPh2, pinchLeftIndexPh2, t)));
        ApplyBone("mixamorig:LeftHandIndex3", Quaternion.Euler(Vector3.Lerp(openLeftIndexPh3, pinchLeftIndexPh3, t)));

        ApplyBone("mixamorig:LeftHandMiddle1", Quaternion.Euler(Vector3.Lerp(openLeftMiddlePh1, pinchLeftMiddlePh1, t)));
        ApplyBone("mixamorig:LeftHandMiddle2", Quaternion.Euler(Vector3.Lerp(openLeftMiddlePh2, pinchLeftMiddlePh2, t)));
        ApplyBone("mixamorig:LeftHandMiddle3", Quaternion.Euler(Vector3.Lerp(openLeftMiddlePh3, pinchLeftMiddlePh3, t)));

        ApplyBone("mixamorig:LeftHandRing1", Quaternion.Euler(Vector3.Lerp(openLeftRingPh1, pinchLeftRingPh1, t)));
        ApplyBone("mixamorig:LeftHandRing2", Quaternion.Euler(Vector3.Lerp(openLeftRingPh2, pinchLeftRingPh2, t)));
        ApplyBone("mixamorig:LeftHandRing3", Quaternion.Euler(Vector3.Lerp(openLeftRingPh3, pinchLeftRingPh3, t)));

        ApplyBone("mixamorig:LeftHandPinky1", Quaternion.Euler(Vector3.Lerp(openLeftPinkyPh1, pinchLeftPinkyPh1, t)));
        ApplyBone("mixamorig:LeftHandPinky2", Quaternion.Euler(Vector3.Lerp(openLeftPinkyPh2, pinchLeftPinkyPh2, t)));
        ApplyBone("mixamorig:LeftHandPinky3", Quaternion.Euler(Vector3.Lerp(openLeftPinkyPh3, pinchLeftPinkyPh3, t)));

        ApplyBone("mixamorig:LeftHandThumb1", Quaternion.Euler(Vector3.Lerp(openLeftThumbPh1, pinchLeftThumbPh1, t)));
        ApplyBone("mixamorig:LeftHandThumb2", Quaternion.Euler(Vector3.Lerp(openLeftThumbPh2, pinchLeftThumbPh2, t)));
        ApplyBone("mixamorig:LeftHandThumb3", Quaternion.Euler(Vector3.Lerp(openLeftThumbPh3, pinchLeftThumbPh3, t)));
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
