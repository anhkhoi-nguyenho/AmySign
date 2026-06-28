using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour l'animation IDLE de l'avatar Amy.
/// La main droite reste levée près du visage en permanence.
/// Elle fait coucou (oscillation latérale), s'immobilise 7 sec, puis recommence en boucle.
/// </summary>
public class COUCOU : MonoBehaviour
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
    public float globalSpeedMultiplier = 1.0f;

    [Header("Durées")]
    [Tooltip("Durée de l'oscillation coucou en secondes")]
    public float coucouDuration = 2.0f;
    [Tooltip("Durée de la pause entre deux coucous (secondes)")]
    public float pauseDuration = 7.0f;

    [Header("Position centrale — main près du visage")]
    public Vector3 handCenterPos = new Vector3(0.30f, 1.15f, 0.20f);
    [Tooltip("Amplitude de l'oscillation latérale")]
    public float oscillationAmplitude = 0.06f;
    [Tooltip("Coude TRÈS BAS pour ne pas faire monter l'épaule")]
    public Vector3 elbowPos = new Vector3(0.35f, 0.75f, 0.05f);

    [Header("Poignet")]
    public bool applyWristRotation = true;
    [Tooltip("Rotation correcte trouvée pour ne pas tordre le poignet")]
    public Vector3 wristRotationEuler = new Vector3(0f, -180f, 0f);
    public bool useLocalRotation = true;

    [Header("Os du poignet — rotation directe")]
    [Tooltip("Tourner directement l'os mixamorig:RightHand après l'IK pour orienter la paume")]
    public bool rotateHandBone = true;
    [Tooltip("Rotation locale supplémentaire de l'os de la main")]
    public Vector3 handBoneExtraRotation = new Vector3(0f, 0f, 180f);

    [Header("Doigts — main ouverte")]
    public bool forceManualHandshape = true;

    public Vector3 indexPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 indexPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 indexPh3 = new Vector3(0f, 0f, 0f);

    public Vector3 middlePh1 = new Vector3(0f, 0f, 0f);
    public Vector3 middlePh2 = new Vector3(0f, 0f, 0f);
    public Vector3 middlePh3 = new Vector3(0f, 0f, 0f);

    public Vector3 ringPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 ringPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 ringPh3 = new Vector3(0f, 0f, 0f);

    public Vector3 pinkyPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 pinkyPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 pinkyPh3 = new Vector3(0f, 0f, 0f);

    public Vector3 thumbPh1 = new Vector3(0f, -20f, 20f);
    public Vector3 thumbPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 thumbPh3 = new Vector3(0f, 0f, 0f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayCoucou();
    }

    public void PlayCoucou()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayCoucouCoroutine());
    }

    public void StopCoucou()
    {
        if (currentCoroutine != null) { StopCoroutine(currentCoroutine); currentCoroutine = null; }
    }

    private IEnumerator PlayCoucouCoroutine()
    {
        if (rightHandTarget == null) { Debug.LogError("[COUCOU] RightHandTarget non assigné."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[COUCOU] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[COUCOU] Démarrage animation idle.");

        ApplyGaze();

        // BOUCLE INFINIE
        while (true)
        {
            // Phase 1 : oscillation coucou
            if (showDebugLogs) Debug.Log("[COUCOU] Coucou !");
            yield return DoCoucou();

            // Phase 2 : pause main immobile en position centrale
            if (showDebugLogs) Debug.Log("[COUCOU] Pause...");
            yield return DoPause();
        }
    }

    /// <summary>
    /// Oscillation latérale 3x autour de la position centrale.
    /// </summary>
    private IEnumerator DoCoucou()
    {
        Vector3[] path = new Vector3[]
        {
            handCenterPos,
            new Vector3(handCenterPos.x + oscillationAmplitude, handCenterPos.y, handCenterPos.z),
            new Vector3(handCenterPos.x - oscillationAmplitude, handCenterPos.y, handCenterPos.z),
            new Vector3(handCenterPos.x + oscillationAmplitude, handCenterPos.y, handCenterPos.z),
            new Vector3(handCenterPos.x - oscillationAmplitude, handCenterPos.y, handCenterPos.z),
            new Vector3(handCenterPos.x + oscillationAmplitude, handCenterPos.y, handCenterPos.z),
            handCenterPos
        };

        int segmentCount = path.Length - 1;
        float realDuration = coucouDuration / Mathf.Max(0.01f, globalSpeedMultiplier);
        float segmentDuration = realDuration / segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 startPos = path[i];
            Vector3 endPos = path[i + 1];

            float elapsed = 0f;
            while (elapsed < segmentDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / segmentDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                Vector3 currentPos = Vector3.Lerp(startPos, endPos, smoothT);
                SetTargetPosition(rightHandTarget, currentPos);
                if (rightElbowHint != null) SetTargetPosition(rightElbowHint, elbowPos);
                if (applyWristRotation) ApplyWristRot();
                if (forceManualHandshape) UpdateFingerRotations();

                yield return null;
            }
        }
    }

    /// <summary>
    /// Pause de 7 sec, main immobile en position centrale.
    /// </summary>
    private IEnumerator DoPause()
    {
        float realDuration = pauseDuration / Mathf.Max(0.01f, globalSpeedMultiplier);
        float elapsed = 0f;

        while (elapsed < realDuration)
        {
            elapsed += Time.deltaTime;
            SetTargetPosition(rightHandTarget, handCenterPos);
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, elbowPos);
            if (applyWristRotation) ApplyWristRot();
            if (forceManualHandshape) UpdateFingerRotations();

            yield return null;
        }
    }

    private void UpdateFingerRotations()
    {
        if (avatarRoot == null) return;

        // Rotation directe de l'os de la main pour orienter la paume
        if (rotateHandBone)
        {
            Transform handBone = FindDeepChild(avatarRoot, "mixamorig:RightHand");
            if (handBone != null)
            {
                handBone.localRotation = handBone.localRotation * Quaternion.Euler(handBoneExtraRotation);
            }
        }

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
        Vector3 gaze = gazeTarget;
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot != null)
            gaze = avatarRoot.TransformPoint(gaze);
        Vector3 direction = gaze - headTarget.position;
        if (direction.sqrMagnitude > 0.001f)
            headTarget.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }

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