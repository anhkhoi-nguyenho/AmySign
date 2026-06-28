using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour le signe AIDER en LSF.
/// Main gauche paume vers le ciel en diagonale, main droite en pouce levé posée dessus.
/// Le tout monte légèrement vers l'avant.
/// </summary>
public class AIDER : MonoBehaviour
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
    public float duration = 1.5f;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Positions main droite (pouce levé)")]
    public Vector3 rightHandStart = new Vector3(0.08f, 0.95f, 0.20f);
    public Vector3 rightHandEnd = new Vector3(0.08f, 1.05f, 0.30f);
    public Vector3 rightElbowPos = new Vector3(0.28f, 0.78f, 0.05f);

    [Header("Positions main gauche (paume vers ciel)")]
    public Vector3 leftHandStart = new Vector3(0.05f, 0.92f, 0.20f);
    public Vector3 leftHandEnd = new Vector3(0.05f, 1.02f, 0.30f);
    public Vector3 leftElbowPos = new Vector3(-0.20f, 0.78f, 0.05f);

    [Header("Poignets")]
    public bool applyWristRotation = true;
    [Tooltip("Main droite : pouce vers le haut")]
    public Vector3 rightWristEuler = new Vector3(0f, -180f, 0f);
    [Tooltip("Main gauche : paume vers le ciel")]
    public Vector3 leftWristEuler = new Vector3(-90f, 0f, 0f);
    public bool useLocalRotation = true;

    [Header("Doigts")]
    public bool forceManualHandshape = true;

    // ============ MAIN DROITE : poing avec pouce levé ============
    [Header("Main droite — Index replié")]
    public Vector3 rightIndexPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightIndexPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightIndexPh3 = new Vector3(70f, 0f, 0f);

    [Header("Main droite — Majeur replié")]
    public Vector3 rightMiddlePh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightMiddlePh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightMiddlePh3 = new Vector3(70f, 0f, 0f);

    [Header("Main droite — Annulaire replié")]
    public Vector3 rightRingPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightRingPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightRingPh3 = new Vector3(70f, 0f, 0f);

    [Header("Main droite — Auriculaire replié")]
    public Vector3 rightPinkyPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightPinkyPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightPinkyPh3 = new Vector3(70f, 0f, 0f);

    [Header("Main droite — Pouce LEVÉ")]
    public Vector3 rightThumbPh1 = new Vector3(0f, -30f, 30f);
    public Vector3 rightThumbPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 rightThumbPh3 = new Vector3(0f, 0f, 0f);

    // ============ MAIN GAUCHE : tous doigts tendus ============
    [Header("Main gauche — Index tendu")]
    public Vector3 leftIndexPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 leftIndexPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 leftIndexPh3 = new Vector3(0f, 0f, 0f);

    [Header("Main gauche — Majeur tendu")]
    public Vector3 leftMiddlePh1 = new Vector3(0f, 0f, 0f);
    public Vector3 leftMiddlePh2 = new Vector3(0f, 0f, 0f);
    public Vector3 leftMiddlePh3 = new Vector3(0f, 0f, 0f);

    [Header("Main gauche — Annulaire tendu")]
    public Vector3 leftRingPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 leftRingPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 leftRingPh3 = new Vector3(0f, 0f, 0f);

    [Header("Main gauche — Auriculaire tendu")]
    public Vector3 leftPinkyPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 leftPinkyPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 leftPinkyPh3 = new Vector3(0f, 0f, 0f);

    [Header("Main gauche — Pouce tendu sur le côté")]
    public Vector3 leftThumbPh1 = new Vector3(0f, 20f, -20f);
    public Vector3 leftThumbPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 leftThumbPh3 = new Vector3(0f, 0f, 0f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayAider();
    }

    public void PlayAider()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayAiderCoroutine());
    }

    private IEnumerator PlayAiderCoroutine()
    {
        if (rightHandTarget == null || leftHandTarget == null) { Debug.LogError("[AIDER] Targets non assignés."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[AIDER] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[AIDER] Démarrage.");

        ApplyGaze();

        // Position initiale
        SetTargetPosition(rightHandTarget, CorrectPosition(rightHandStart));
        SetTargetPosition(leftHandTarget, CorrectPosition(leftHandStart));
        if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
        if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));
        ApplyWristRotations();
        if (forceManualHandshape) UpdateFingerRotations();

        yield return new WaitForSeconds(0.3f);

        // Montée légère vers l'avant
        float safeSpeed = Mathf.Max(0.01f, globalSpeedMultiplier);
        float realDuration = duration / safeSpeed;
        float elapsed = 0f;

        while (elapsed < realDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / realDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentRight = Vector3.Lerp(rightHandStart, rightHandEnd, smoothT);
            Vector3 currentLeft = Vector3.Lerp(leftHandStart, leftHandEnd, smoothT);

            SetTargetPosition(rightHandTarget, CorrectPosition(currentRight));
            SetTargetPosition(leftHandTarget, CorrectPosition(currentLeft));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));
            ApplyWristRotations();
            if (forceManualHandshape) UpdateFingerRotations();

            yield return null;
        }

        // Pose maintenue
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

        // Main droite : poing avec pouce levé
        ApplyBone("mixamorig:RightHandIndex1", Quaternion.Euler(rightIndexPh1));
        ApplyBone("mixamorig:RightHandIndex2", Quaternion.Euler(rightIndexPh2));
        ApplyBone("mixamorig:RightHandIndex3", Quaternion.Euler(rightIndexPh3));
        ApplyBone("mixamorig:RightHandMiddle1", Quaternion.Euler(rightMiddlePh1));
        ApplyBone("mixamorig:RightHandMiddle2", Quaternion.Euler(rightMiddlePh2));
        ApplyBone("mixamorig:RightHandMiddle3", Quaternion.Euler(rightMiddlePh3));
        ApplyBone("mixamorig:RightHandRing1", Quaternion.Euler(rightRingPh1));
        ApplyBone("mixamorig:RightHandRing2", Quaternion.Euler(rightRingPh2));
        ApplyBone("mixamorig:RightHandRing3", Quaternion.Euler(rightRingPh3));
        ApplyBone("mixamorig:RightHandPinky1", Quaternion.Euler(rightPinkyPh1));
        ApplyBone("mixamorig:RightHandPinky2", Quaternion.Euler(rightPinkyPh2));
        ApplyBone("mixamorig:RightHandPinky3", Quaternion.Euler(rightPinkyPh3));
        ApplyBone("mixamorig:RightHandThumb1", Quaternion.Euler(rightThumbPh1));
        ApplyBone("mixamorig:RightHandThumb2", Quaternion.Euler(rightThumbPh2));
        ApplyBone("mixamorig:RightHandThumb3", Quaternion.Euler(rightThumbPh3));

        // Main gauche : tous les doigts tendus
        ApplyBone("mixamorig:LeftHandIndex1", Quaternion.Euler(leftIndexPh1));
        ApplyBone("mixamorig:LeftHandIndex2", Quaternion.Euler(leftIndexPh2));
        ApplyBone("mixamorig:LeftHandIndex3", Quaternion.Euler(leftIndexPh3));
        ApplyBone("mixamorig:LeftHandMiddle1", Quaternion.Euler(leftMiddlePh1));
        ApplyBone("mixamorig:LeftHandMiddle2", Quaternion.Euler(leftMiddlePh2));
        ApplyBone("mixamorig:LeftHandMiddle3", Quaternion.Euler(leftMiddlePh3));
        ApplyBone("mixamorig:LeftHandRing1", Quaternion.Euler(leftRingPh1));
        ApplyBone("mixamorig:LeftHandRing2", Quaternion.Euler(leftRingPh2));
        ApplyBone("mixamorig:LeftHandRing3", Quaternion.Euler(leftRingPh3));
        ApplyBone("mixamorig:LeftHandPinky1", Quaternion.Euler(leftPinkyPh1));
        ApplyBone("mixamorig:LeftHandPinky2", Quaternion.Euler(leftPinkyPh2));
        ApplyBone("mixamorig:LeftHandPinky3", Quaternion.Euler(leftPinkyPh3));
        ApplyBone("mixamorig:LeftHandThumb1", Quaternion.Euler(leftThumbPh1));
        ApplyBone("mixamorig:LeftHandThumb2", Quaternion.Euler(leftThumbPh2));
        ApplyBone("mixamorig:LeftHandThumb3", Quaternion.Euler(leftThumbPh3));
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
