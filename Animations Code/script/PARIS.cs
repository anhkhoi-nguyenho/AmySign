using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour le signe PARIS en LSF.
/// Main gauche ouverte paume vers le ciel (support).
/// Main droite avec index/majeur à angle droit, le majeur tapote 2 fois la paume gauche.
/// </summary>
public class PARIS : MonoBehaviour
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
    public float globalSpeedMultiplier = 1.0f;

    [Header("Durées")]
    public float startDelay = 0.3f;
    public float tapDuration = 0.25f;
    public float liftDuration = 0.25f;
    public float holdAfterDuration = 0.5f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Main gauche — support paume vers le ciel")]
    public Vector3 leftHandPos = new Vector3(0.0f, 1.00f, 0.30f);
    public Vector3 leftElbowPos = new Vector3(-0.25f, 0.80f, 0.10f);
    public Vector3 leftWristEuler = new Vector3(-90f, 0f, 0f);

    [Header("Main droite — position fixe (juste au-dessus de la main gauche)")]
    public Vector3 rightHandPos = new Vector3(0.05f, 1.10f, 0.30f);
    public Vector3 rightElbowPos = new Vector3(0.30f, 0.90f, 0.10f);

    [Header("Main droite — Poignet")]
    public bool applyWristRotation = true;
    [Tooltip("Position basse : majeur appuie sur la paume gauche")]
    public Vector3 rightWristEulerDown = new Vector3(-45f, -180f, 0f);
    [Tooltip("Position haute : poignet remonté entre les tapes")]
    public Vector3 rightWristEulerUp = new Vector3(0f, -180f, 0f);
    public bool useLocalRotation = true;

    [Header("Doigts main gauche — tous tendus collés")]
    public bool forceManualHandshape = true;

    [Header("GAUCHE — Index tendu collé")]
    public Vector3 leftIndexPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 leftIndexPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 leftIndexPh3 = new Vector3(0f, 0f, 0f);

    [Header("GAUCHE — Majeur tendu collé")]
    public Vector3 leftMiddlePh1 = new Vector3(0f, 0f, 0f);
    public Vector3 leftMiddlePh2 = new Vector3(0f, 0f, 0f);
    public Vector3 leftMiddlePh3 = new Vector3(0f, 0f, 0f);

    [Header("GAUCHE — Annulaire tendu collé")]
    public Vector3 leftRingPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 leftRingPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 leftRingPh3 = new Vector3(0f, 0f, 0f);

    [Header("GAUCHE — Auriculaire tendu collé")]
    public Vector3 leftPinkyPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 leftPinkyPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 leftPinkyPh3 = new Vector3(0f, 0f, 0f);

    [Header("GAUCHE — Pouce sur le côté")]
    public Vector3 leftThumbPh1 = new Vector3(0f, 20f, -20f);
    public Vector3 leftThumbPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 leftThumbPh3 = new Vector3(0f, 0f, 0f);

    [Header("Doigts main droite")]
    [Header("DROITE — Index tendu (pointe la caméra)")]
    public Vector3 rightIndexPh1 = new Vector3(-20f, 0f, 0f);
    public Vector3 rightIndexPh2 = new Vector3(-15f, 0f, 0f);
    public Vector3 rightIndexPh3 = new Vector3(-10f, 0f, 0f);

    [Header("DROITE — Majeur tendu (angle droit avec l'index, vers le bas)")]
    public Vector3 rightMiddlePh1 = new Vector3(90f, 0f, 0f);
    public Vector3 rightMiddlePh2 = new Vector3(0f, 0f, 0f);
    public Vector3 rightMiddlePh3 = new Vector3(0f, 0f, 0f);

    [Header("DROITE — Annulaire replié")]
    public Vector3 rightRingPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightRingPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightRingPh3 = new Vector3(70f, 0f, 0f);

    [Header("DROITE — Auriculaire replié")]
    public Vector3 rightPinkyPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightPinkyPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightPinkyPh3 = new Vector3(70f, 0f, 0f);

    [Header("DROITE — Pouce sur le côté")]
    public Vector3 rightThumbPh1 = new Vector3(0f, -20f, 30f);
    public Vector3 rightThumbPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 rightThumbPh3 = new Vector3(0f, 0f, 0f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;
    private Vector3 currentRightWrist;

    private void Start()
    {
        if (playOnStart) PlayParis();
    }

    public void PlayParis()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayParisCoroutine());
    }

    private IEnumerator PlayParisCoroutine()
    {
        if (rightHandTarget == null || leftHandTarget == null) { Debug.LogError("[PARIS] Targets non assignés."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[PARIS] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[PARIS] Démarrage.");

        ApplyGaze();
        float safeSpeed = Mathf.Max(0.01f, globalSpeedMultiplier);

        // Position initiale : main droite en haut (poignet remonté)
        currentRightWrist = rightWristEulerUp;
        ApplyAllPositions();
        yield return new WaitForSeconds(startDelay / safeSpeed);

        // === TAPE 1 ===
        if (showDebugLogs) Debug.Log("[PARIS] Tape 1 : descente.");
        yield return RotateWrist(rightWristEulerUp, rightWristEulerDown, tapDuration / safeSpeed);

        if (showDebugLogs) Debug.Log("[PARIS] Tape 1 : remontée.");
        yield return RotateWrist(rightWristEulerDown, rightWristEulerUp, liftDuration / safeSpeed);

        // === TAPE 2 ===
        if (showDebugLogs) Debug.Log("[PARIS] Tape 2 : descente.");
        yield return RotateWrist(rightWristEulerUp, rightWristEulerDown, tapDuration / safeSpeed);

        if (showDebugLogs) Debug.Log("[PARIS] Tape 2 : remontée.");
        yield return RotateWrist(rightWristEulerDown, rightWristEulerUp, liftDuration / safeSpeed);

        // Maintien
        if (showDebugLogs) Debug.Log("[PARIS] Maintien.");
        float elapsed = 0f;
        float realHold = holdAfterDuration / safeSpeed;
        while (elapsed < realHold)
        {
            elapsed += Time.deltaTime;
            ApplyAllPositions();
            yield return null;
        }

        // Pose infinie
        while (true)
        {
            ApplyAllPositions();
            yield return null;
        }
    }

    private IEnumerator RotateWrist(Vector3 fromEuler, Vector3 toEuler, float dur)
    {
        Quaternion fromQ = Quaternion.Euler(fromEuler);
        Quaternion toQ = Quaternion.Euler(toEuler);
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            Quaternion currentQ = Quaternion.Slerp(fromQ, toQ, smoothT);
            currentRightWrist = currentQ.eulerAngles;
            // Application directe pour éviter problème conversion euler
            SetTargetPosition(rightHandTarget, CorrectPosition(rightHandPos));
            SetTargetPosition(leftHandTarget, CorrectPosition(leftHandPos));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));
            if (applyWristRotation)
            {
                if (useLocalRotation) { rightHandTarget.localRotation = currentQ; }
                else                  { rightHandTarget.rotation = currentQ; }
                Quaternion lQ = Quaternion.Euler(leftWristEuler);
                if (useLocalRotation) leftHandTarget.localRotation = lQ;
                else                  leftHandTarget.rotation = lQ;
            }
            if (forceManualHandshape) UpdateFingerRotations();
            yield return null;
        }
        currentRightWrist = toEuler;
    }

    private void ApplyAllPositions()
    {
        SetTargetPosition(rightHandTarget, CorrectPosition(rightHandPos));
        SetTargetPosition(leftHandTarget, CorrectPosition(leftHandPos));
        if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
        if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));
        ApplyWristRotations();
        if (forceManualHandshape) UpdateFingerRotations();
    }

    private void ApplyWristRotations()
    {
        if (!applyWristRotation) return;
        if (rightHandTarget != null)
        {
            Quaternion r = Quaternion.Euler(currentRightWrist);
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

        // Main droite
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

        // Main gauche
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
