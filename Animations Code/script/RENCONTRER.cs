using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour le signe RENCONTRER en LSF.
/// Les deux paumes se font face, index tendus vers le ciel.
/// Les mains se rapprochent jusqu'à se coller (pouces qui se touchent en premier).
/// </summary>
public class RENCONTRER : MonoBehaviour
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

    [Header("Positions main droite")]
    [Tooltip("Position de départ : main à droite, vers l'arrière, sur le côté droit")]
    public Vector3 rightHandStart = new Vector3(0.25f, 1.10f, 0.10f);
    [Tooltip("Position de fin : main à gauche du personnage, en avant, en diagonale")]
    public Vector3 rightHandEnd = new Vector3(-0.10f, 1.10f, 0.30f);
    public Vector3 rightElbowPos = new Vector3(0.30f, 0.85f, 0.05f);

    [Header("Positions main gauche")]
    [Tooltip("Position de départ : main à gauche, en avant")]
    public Vector3 leftHandStart = new Vector3(-0.25f, 1.10f, 0.30f);
    [Tooltip("Position de fin : main à gauche du personnage, en arrière, en diagonale (touche le pouce droit)")]
    public Vector3 leftHandEnd = new Vector3(-0.16f, 1.10f, 0.25f);
    public Vector3 leftElbowPos = new Vector3(-0.30f, 0.85f, 0.05f);

    [Header("Poignets — paumes face à face")]
    public bool applyWristRotation = true;
    [Tooltip("Main droite : paume vers la gauche")]
    public Vector3 rightWristEuler = new Vector3(0f, -90f, 0f);
    [Tooltip("Main gauche : paume vers la droite")]
    public Vector3 leftWristEuler = new Vector3(0f, 90f, 0f);
    public bool useLocalRotation = true;

    [Header("Doigts — index tendu, autres repliés")]
    public bool forceManualHandshape = true;

    [Header("Index — tendu vers le ciel")]
    public Vector3 indexPh1 = new Vector3(-20f, 0f, 0f);
    public Vector3 indexPh2 = new Vector3(-15f, 0f, 0f);
    public Vector3 indexPh3 = new Vector3(-10f, 0f, 0f);

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

    [Header("Pouce — posé sur le majeur")]
    public Vector3 thumbPh1 = new Vector3(40f, 20f, 50f);
    public Vector3 thumbPh2 = new Vector3(40f, 0f, 30f);
    public Vector3 thumbPh3 = new Vector3(20f, 0f, 0f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayRencontrer();
    }

    public void PlayRencontrer()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayRencontrerCoroutine());
    }

    private IEnumerator PlayRencontrerCoroutine()
    {
        if (rightHandTarget == null || leftHandTarget == null) { Debug.LogError("[RENCONTRER] Targets non assignés."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[RENCONTRER] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[RENCONTRER] Démarrage : les mains se rapprochent.");

        ApplyGaze();

        // Position initiale
        SetTargetPosition(rightHandTarget, CorrectPosition(rightHandStart));
        SetTargetPosition(leftHandTarget, CorrectPosition(leftHandStart));
        if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
        if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));
        ApplyWristRotations();
        if (forceManualHandshape) UpdateFingerRotations();

        yield return new WaitForSeconds(0.2f);

        // Rapprochement des deux mains
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

        // Pose maintenue (mains collées)
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

        // Main droite
        ApplyHandFingers("Right");
        // Main gauche
        ApplyHandFingers("Left");
    }

    private void ApplyHandFingers(string side)
    {
        ApplyBone("mixamorig:" + side + "HandIndex1", Quaternion.Euler(indexPh1));
        ApplyBone("mixamorig:" + side + "HandIndex2", Quaternion.Euler(indexPh2));
        ApplyBone("mixamorig:" + side + "HandIndex3", Quaternion.Euler(indexPh3));

        ApplyBone("mixamorig:" + side + "HandMiddle1", Quaternion.Euler(middlePh1));
        ApplyBone("mixamorig:" + side + "HandMiddle2", Quaternion.Euler(middlePh2));
        ApplyBone("mixamorig:" + side + "HandMiddle3", Quaternion.Euler(middlePh3));

        ApplyBone("mixamorig:" + side + "HandRing1", Quaternion.Euler(ringPh1));
        ApplyBone("mixamorig:" + side + "HandRing2", Quaternion.Euler(ringPh2));
        ApplyBone("mixamorig:" + side + "HandRing3", Quaternion.Euler(ringPh3));

        ApplyBone("mixamorig:" + side + "HandPinky1", Quaternion.Euler(pinkyPh1));
        ApplyBone("mixamorig:" + side + "HandPinky2", Quaternion.Euler(pinkyPh2));
        ApplyBone("mixamorig:" + side + "HandPinky3", Quaternion.Euler(pinkyPh3));

        ApplyBone("mixamorig:" + side + "HandThumb1", Quaternion.Euler(thumbPh1));
        ApplyBone("mixamorig:" + side + "HandThumb2", Quaternion.Euler(thumbPh2));
        ApplyBone("mixamorig:" + side + "HandThumb3", Quaternion.Euler(thumbPh3));
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
