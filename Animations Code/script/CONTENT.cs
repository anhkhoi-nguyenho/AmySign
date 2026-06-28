using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour le signe CONTENT en LSF.
/// La paume est posée sur le torse, doigts légèrement écartés.
/// La main fait des petits cercles sur la poitrine.
/// </summary>
public class CONTENT : MonoBehaviour
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
    public float duration = 2.0f;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Position cercle sur torse")]
    [Tooltip("Centre du cercle sur le torse")]
    public Vector3 circleCenter = new Vector3(0.10f, 1.10f, 0.10f);
    [Tooltip("Rayon du cercle (petits ronds)")]
    public float circleRadius = 0.04f;
    [Tooltip("Nombre de cercles complets")]
    public int numberOfCycles = 2;
    public Vector3 elbowPos = new Vector3(0.28f, 0.85f, 0.05f);

    [Header("Poignet — paume vers torse")]
    public bool applyWristRotation = true;
    public Vector3 wristRotationEuler = new Vector3(0f, -180f, 0f);
    public bool useLocalRotation = true;

    [Header("Doigts — tous légèrement écartés et tendus")]
    public bool forceManualHandshape = true;

    [Header("Index — tendu incliné légèrement gauche")]
    public Vector3 indexPh1 = new Vector3(-10f, -8f, 0f);
    public Vector3 indexPh2 = new Vector3(-5f, 0f, 0f);
    public Vector3 indexPh3 = new Vector3(0f, 0f, 0f);

    [Header("Majeur — tendu droit")]
    public Vector3 middlePh1 = new Vector3(-10f, 0f, 0f);
    public Vector3 middlePh2 = new Vector3(-5f, 0f, 0f);
    public Vector3 middlePh3 = new Vector3(0f, 0f, 0f);

    [Header("Annulaire — tendu incliné légèrement droite")]
    public Vector3 ringPh1 = new Vector3(-10f, 8f, 0f);
    public Vector3 ringPh2 = new Vector3(-5f, 0f, 0f);
    public Vector3 ringPh3 = new Vector3(0f, 0f, 0f);

    [Header("Auriculaire — tendu incliné droite")]
    public Vector3 pinkyPh1 = new Vector3(-10f, 16f, 0f);
    public Vector3 pinkyPh2 = new Vector3(-5f, 0f, 0f);
    public Vector3 pinkyPh3 = new Vector3(0f, 0f, 0f);

    [Header("Pouce — légèrement écarté")]
    public Vector3 thumbPh1 = new Vector3(0f, -20f, 25f);
    public Vector3 thumbPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 thumbPh3 = new Vector3(0f, 0f, 0f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayContent();
    }

    public void PlayContent()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayContentCoroutine());
    }

    private IEnumerator PlayContentCoroutine()
    {
        if (rightHandTarget == null) { Debug.LogError("[CONTENT] RightHandTarget non assigné."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[CONTENT] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[CONTENT] Démarrage : cercles sur torse.");

        ApplyGaze();

        float safeSpeed = Mathf.Max(0.01f, globalSpeedMultiplier);
        float realDuration = duration / safeSpeed;

        // Mouvement circulaire continu
        float elapsed = 0f;
        float totalAngle = numberOfCycles * 2f * Mathf.PI;

        while (elapsed < realDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / realDuration);
            float angle = t * totalAngle;

            // Calcul de la position sur le cercle (dans le plan X-Y, à hauteur Z fixe)
            float offsetX = Mathf.Cos(angle) * circleRadius;
            float offsetY = Mathf.Sin(angle) * circleRadius;
            Vector3 currentPos = new Vector3(
                circleCenter.x + offsetX,
                circleCenter.y + offsetY,
                circleCenter.z
            );

            SetTargetPosition(rightHandTarget, CorrectPosition(currentPos));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(elbowPos));
            if (applyWristRotation) ApplyWristRot();
            if (forceManualHandshape) UpdateFingerRotations();

            yield return null;
        }

        // Pose maintenue au centre à la fin
        while (true)
        {
            SetTargetPosition(rightHandTarget, CorrectPosition(circleCenter));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(elbowPos));
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
        else                  rightHandTarget.rotation = r;
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
