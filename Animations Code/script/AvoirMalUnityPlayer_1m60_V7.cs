using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity V8 pour AVOIR-MAL LSF.
/// Avec rotation poignet gauche ajustable (sans toucher au bras).
/// </summary>
public class AvoirMalUnityPlayer_1m60_V7 : MonoBehaviour
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
    public float duration = 1.3f;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement position si besoin")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Os des doigts — Pouce")]
    public Transform thumb1;
    public Transform thumb2;
    public Transform thumb3;

    [Header("Os des doigts — Index")]
    public Transform index1;
    public Transform index2;
    public Transform index3;

    [Header("Os des doigts — Majeur")]
    public Transform middle1;
    public Transform middle2;
    public Transform middle3;

    [Header("Os des doigts — Annulaire")]
    public Transform ring1;
    public Transform ring2;
    public Transform ring3;

    [Header("Os des doigts — Auriculaire")]
    public Transform pinky1;
    public Transform pinky2;
    public Transform pinky3;

    [Header("Vitesse transition pose doigts")]
    public float poseTransitionDuration = 0.12f;

    [Header("Correction poignet — Phase 1")]
    public Vector3 startWristRotationEuler = new Vector3(0f, 90f, -90f);
    public Vector3 chinWristRotationEuler = new Vector3(0f, 90f, -90f);

    [Header("Correction poignet — Phase 2")]
    public Vector3 endWristRotationEuler = new Vector3(0f, 0f, -90f);

    public bool useLocalRotation = true;
    public bool applyWristRotationCorrection = true;

    [Header("Coude")]
    public bool applyElbowHintPosition = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    [Header("Pose thumb_up — Pouce dressé")]
    public Vector3 thumbUpThumb1 = new Vector3(-15f, 20f, 15f);
    public Vector3 thumbUpThumb2 = new Vector3(0f, 0f, 10f);
    public Vector3 thumbUpThumb3 = new Vector3(0f, 0f, 5f);

    [Header("Pose thumb_up — Phalanges autres doigts")]
    public Vector3 thumbUpIndexRots = new Vector3(0f, 0f, 75f);
    public Vector3 thumbUpMiddleRots = new Vector3(0f, 0f, 80f);
    public Vector3 thumbUpRingRots = new Vector3(0f, 0f, 80f);
    public Vector3 thumbUpPinkyRots = new Vector3(0f, 0f, 75f);

    [Header("Pose closed_fist — Pouce rabattu")]
    public Vector3 fistThumb1 = new Vector3(30f, 35f, 55f);
    public Vector3 fistThumb2 = new Vector3(0f, 0f, 45f);
    public Vector3 fistThumb3 = new Vector3(0f, 0f, 30f);

    [Header("Pose closed_fist — Poing fermé")]
    public Vector3 fistIndexRots = new Vector3(0f, 0f, 90f);
    public Vector3 fistMiddleRots = new Vector3(0f, 0f, 90f);
    public Vector3 fistRingRots = new Vector3(0f, 0f, 90f);
    public Vector3 fistPinkyRots = new Vector3(0f, 0f, 90f);

    private readonly Vector3[] handPath = new Vector3[]
    {
        new Vector3(0.04f, 0.75f, 0.10f),
        new Vector3(0.04f, 1.00f, 0.12f),
        new Vector3(0.04f, 1.18f, 0.14f),
        new Vector3(0.06f, 0.90f, 0.14f),
    };

    private readonly Vector3[] elbowPath = new Vector3[]
    {
        new Vector3(0.26f, 0.72f, 0.04f),
        new Vector3(0.26f, 0.86f, 0.08f),
        new Vector3(0.26f, 0.90f, 0.10f),
        new Vector3(0.26f, 0.76f, 0.08f),
    };

    private readonly float[] segmentWeights = new float[] { 0.25f, 0.35f, 0.40f };
    private const float CHIN_T = 0.60f;

    private readonly Vector3 gazeTarget = new Vector3(0.0f, 1.15f, 1.70f);
    private Coroutine currentCoroutine;
    private Coroutine poseCoroutine;

    private void Start()
    {
        if (playOnStart) PlayAvoirMal();
    }

    public void PlayAvoirMal()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayAvoirMalCoroutine());
    }

    public void StopAvoirMal()
    {
        if (currentCoroutine != null) { StopCoroutine(currentCoroutine); currentCoroutine = null; }
        if (poseCoroutine != null) { StopCoroutine(poseCoroutine); poseCoroutine = null; }
    }

    private void ApplyLeftWristRotation()
    {
        if (leftHandTarget != null)
            leftHandTarget.localRotation = Quaternion.Euler(leftWristRotationEuler);
    }

    private IEnumerator PlayAvoirMalCoroutine()
    {
        if (rightHandTarget == null) { Debug.LogError("[AVOIR-MAL] RightHandTarget non assigné."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[AVOIR-MAL] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[AVOIR-MAL] Démarrage.");

        SetFingerPose(1f);
        ApplyGaze();
        ApplyLeftWristRotation();

        float realDuration = Mathf.Max(0.05f, duration / Mathf.Max(0.01f, globalSpeedMultiplier));
        SetTargetPosition(rightHandTarget, CorrectPosition(handPath[0]));
        if (applyWristRotationCorrection) ApplyWristRotation(0f);
        if (applyElbowHintPosition && rightElbowHint != null)
            SetTargetPosition(rightElbowHint, CorrectPosition(elbowPath[0]));

        yield return MoveAlongPath(realDuration);
    }

    private IEnumerator MoveAlongPath(float totalDuration)
    {
        int segmentCount = handPath.Length - 1;

        float[] segmentDurations = new float[segmentCount];
        for (int i = 0; i < segmentCount; i++)
            segmentDurations[i] = totalDuration * segmentWeights[i];

        float[] globalStarts = new float[segmentCount];
        float[] globalEnds = new float[segmentCount];
        float cumulative = 0f;
        for (int i = 0; i < segmentCount; i++)
        {
            globalStarts[i] = cumulative;
            cumulative += segmentWeights[i];
            globalEnds[i] = cumulative;
        }

        for (int i = 0; i < segmentCount; i++)
        {
            if (i == 2)
            {
                if (poseCoroutine != null) StopCoroutine(poseCoroutine);
                poseCoroutine = StartCoroutine(TransitionPose(1f, 0f, poseTransitionDuration));
            }

            Vector3 handStart = CorrectPosition(handPath[i]);
            Vector3 handEnd = CorrectPosition(handPath[i + 1]);
            Vector3 elbowStart = CorrectPosition(elbowPath[Mathf.Min(i, elbowPath.Length - 1)]);
            Vector3 elbowEnd = CorrectPosition(elbowPath[Mathf.Min(i + 1, elbowPath.Length - 1)]);

            yield return MoveSegment(handStart, handEnd, elbowStart, elbowEnd,
                                     segmentDurations[i], globalStarts[i], globalEnds[i]);
        }
    }

    private IEnumerator MoveSegment(
        Vector3 handStart, Vector3 handEnd,
        Vector3 elbowStart, Vector3 elbowEnd,
        float segmentDuration,
        float globalStartT, float globalEndT)
    {
        float elapsed = 0f;
        while (elapsed < segmentDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / segmentDuration);
            float smoothT = globalStartT >= CHIN_T
                ? EaseOutCubic(t)
                : Mathf.SmoothStep(0f, 1f, t);

            SetTargetPosition(rightHandTarget, Vector3.Lerp(handStart, handEnd, smoothT));
            if (applyElbowHintPosition && rightElbowHint != null)
                SetTargetPosition(rightElbowHint, Vector3.Lerp(elbowStart, elbowEnd, smoothT));
            if (applyWristRotationCorrection)
                ApplyWristRotation(Mathf.Lerp(globalStartT, globalEndT, smoothT));

            ApplyLeftWristRotation();

            yield return null;
        }

        SetTargetPosition(rightHandTarget, handEnd);
        if (applyElbowHintPosition && rightElbowHint != null)
            SetTargetPosition(rightElbowHint, elbowEnd);
        if (applyWristRotationCorrection)
            ApplyWristRotation(globalEndT);
        ApplyLeftWristRotation();
    }

    private void ApplyWristRotation(float globalT)
    {
        if (rightHandTarget == null) return;

        Quaternion startRot = Quaternion.Euler(startWristRotationEuler);
        Quaternion chinRot = Quaternion.Euler(chinWristRotationEuler);
        Quaternion endRot = Quaternion.Euler(endWristRotationEuler);

        Quaternion targetRot;
        if (globalT <= CHIN_T)
        {
            float localT = Mathf.InverseLerp(0f, CHIN_T, globalT);
            targetRot = Quaternion.Slerp(startRot, chinRot, localT);
        }
        else
        {
            float localT = Mathf.InverseLerp(CHIN_T, 1f, globalT);
            targetRot = Quaternion.Slerp(chinRot, endRot, localT);
        }

        if (useLocalRotation) rightHandTarget.localRotation = targetRot;
        else rightHandTarget.rotation = targetRot;
    }

    private void SetFingerPose(float t)
    {
        SetBoneRotation(thumb1, Vector3.Lerp(fistThumb1, thumbUpThumb1, t));
        SetBoneRotation(thumb2, Vector3.Lerp(fistThumb2, thumbUpThumb2, t));
        SetBoneRotation(thumb3, Vector3.Lerp(fistThumb3, thumbUpThumb3, t));

        SetBoneRotation(index1, Vector3.Lerp(fistIndexRots, thumbUpIndexRots, t));
        SetBoneRotation(index2, Vector3.Lerp(fistIndexRots, thumbUpIndexRots * 0.9f, t));
        SetBoneRotation(index3, Vector3.Lerp(fistIndexRots * 0.8f, thumbUpIndexRots * 0.7f, t));
        SetBoneRotation(middle1, Vector3.Lerp(fistMiddleRots, thumbUpMiddleRots, t));
        SetBoneRotation(middle2, Vector3.Lerp(fistMiddleRots, thumbUpMiddleRots * 0.9f, t));
        SetBoneRotation(middle3, Vector3.Lerp(fistMiddleRots * 0.8f, thumbUpMiddleRots * 0.7f, t));
        SetBoneRotation(ring1, Vector3.Lerp(fistRingRots, thumbUpRingRots, t));
        SetBoneRotation(ring2, Vector3.Lerp(fistRingRots, thumbUpRingRots * 0.9f, t));
        SetBoneRotation(ring3, Vector3.Lerp(fistRingRots * 0.8f, thumbUpRingRots * 0.7f, t));
        SetBoneRotation(pinky1, Vector3.Lerp(fistPinkyRots, thumbUpPinkyRots, t));
        SetBoneRotation(pinky2, Vector3.Lerp(fistPinkyRots, thumbUpPinkyRots * 0.9f, t));
        SetBoneRotation(pinky3, Vector3.Lerp(fistPinkyRots * 0.8f, thumbUpPinkyRots * 0.7f, t));
    }

    private void SetBoneRotation(Transform bone, Vector3 euler)
    {
        if (bone != null) bone.localRotation = Quaternion.Euler(euler);
    }

    private IEnumerator TransitionPose(float fromT, float toT, float transitionDuration)
    {
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / transitionDuration));
            SetFingerPose(Mathf.Lerp(fromT, toT, t));
            yield return null;
        }
        SetFingerPose(toT);
    }

    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
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
}