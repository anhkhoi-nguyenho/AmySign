using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity V8 pour BONJOUR LSF.
/// V8 = V7 + main gauche maintenue en position repos.
/// </summary>
public class BonjourUnityPlayer_1m60_Corrige_V7 : MonoBehaviour
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
    public float duration = 1.35f;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement position si besoin")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Correction poignet — Phase 1 : repos → menton")]
    public Vector3 startWristRotationEuler = new Vector3(0f, 180f, 0f);
    public Vector3 chinWristRotationEuler = new Vector3(-20f, 180f, 0f);

    [Header("Correction poignet — Phase 2 : menton → départ")]
    public Vector3 leaveWristRotationEuler = new Vector3(0f, 180f, 0f);

    [Header("Correction poignet — Phase 3 : départ → fin")]
    public Vector3 endWristRotationEuler = new Vector3(-35f, 180f, 0f);

    public bool useLocalRotation = true;
    public bool applyWristRotationCorrection = true;

    [Header("Coude")]
    public bool applyElbowHintPosition = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly Vector3[] handPath = new Vector3[]
    {
        new Vector3(0.04f, 1.00f, 0.10f),
        new Vector3(0.04f, 1.08f, 0.12f),
        new Vector3(0.04f, 1.15f, 0.14f),
        new Vector3(0.04f, 1.10f, 0.16f),
        new Vector3(0.06f, 1.05f, 0.14f)
    };

    private readonly Vector3[] elbowPath = new Vector3[]
    {
        new Vector3(0.26f, 0.85f, 0.04f),
        new Vector3(0.26f, 0.88f, 0.06f),
        new Vector3(0.26f, 0.90f, 0.10f),
        new Vector3(0.26f, 0.88f, 0.10f),
        new Vector3(0.26f, 0.86f, 0.08f)
    };

    private const float CHIN_T = 0.45f;
    private const float LEAVE_T = 0.65f;

    private readonly Vector3 gazeTarget = new Vector3(0.0f, 1.15f, 1.70f);
    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayBonjour();
    }

    public void PlayBonjour()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayBonjourCoroutine());
    }

    public void StopBonjour()
    {
        if (currentCoroutine != null) { StopCoroutine(currentCoroutine); currentCoroutine = null; }
    }

    private IEnumerator PlayBonjourCoroutine()
    {
        if (rightHandTarget == null) { Debug.LogError("[BONJOUR] RightHandTarget non assigné."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[BONJOUR] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[BONJOUR] Démarrage.");

        ApplyGaze();
        ApplyLeftWristRotation();

        float realDuration = Mathf.Max(0.05f, duration / Mathf.Max(0.01f, globalSpeedMultiplier));

        SetTargetPosition(rightHandTarget, CorrectPosition(handPath[0]));
        if (applyWristRotationCorrection) ApplyWristRotation(0f);
        if (applyElbowHintPosition && rightElbowHint != null)
            SetTargetPosition(rightElbowHint, CorrectPosition(elbowPath[0]));

        yield return MoveAlongPath(realDuration);
    }

    private void ApplyLeftWristRotation()
    {
        if (leftHandTarget != null)
            leftHandTarget.localRotation = Quaternion.Euler(leftWristRotationEuler);
    }

    private IEnumerator MoveAlongPath(float totalDuration)
    {
        int segmentCount = handPath.Length - 1;
        float segmentDuration = totalDuration / segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 handStart = CorrectPosition(handPath[i]);
            Vector3 handEnd = CorrectPosition(handPath[i + 1]);
            Vector3 elbowStart = CorrectPosition(elbowPath[Mathf.Min(i, elbowPath.Length - 1)]);
            Vector3 elbowEnd = CorrectPosition(elbowPath[Mathf.Min(i + 1, elbowPath.Length - 1)]);

            float globalStartT = (float)i / segmentCount;
            float globalEndT = (float)(i + 1) / segmentCount;

            yield return MoveSegment(handStart, handEnd, elbowStart, elbowEnd,
                                     segmentDuration, globalStartT, globalEndT);
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
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            SetTargetPosition(rightHandTarget, Vector3.Lerp(handStart, handEnd, smoothT));
            if (applyElbowHintPosition && rightElbowHint != null)
                SetTargetPosition(rightElbowHint, Vector3.Lerp(elbowStart, elbowEnd, smoothT));
            if (applyWristRotationCorrection)
                ApplyWristRotation(Mathf.Lerp(globalStartT, globalEndT, smoothT));

            // Maintenir la main gauche au repos pendant tout le mouvement
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
        Quaternion leaveRot = Quaternion.Euler(leaveWristRotationEuler);
        Quaternion endRot = Quaternion.Euler(endWristRotationEuler);

        Quaternion targetRot;

        if (globalT <= CHIN_T)
        {
            float localT = Mathf.InverseLerp(0f, CHIN_T, globalT);
            targetRot = Quaternion.Slerp(startRot, chinRot, localT);
        }
        else if (globalT <= LEAVE_T)
        {
            float localT = Mathf.InverseLerp(CHIN_T, LEAVE_T, globalT);
            targetRot = Quaternion.Slerp(chinRot, leaveRot, localT);
        }
        else
        {
            float localT = Mathf.InverseLerp(LEAVE_T, 1f, globalT);
            targetRot = Quaternion.Slerp(leaveRot, endRot, localT);
        }

        if (useLocalRotation) rightHandTarget.localRotation = targetRot;
        else rightHandTarget.rotation = targetRot;
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