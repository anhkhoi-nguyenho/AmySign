using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity V8 pour le signe OÙ.
///
/// Nouveauté V8 :
///   Chaque doigt a ses propres champs X, Y, Z pour phalange 1 et phalange 2.
///   Comme ça on peut calibrer chaque doigt indépendamment dans l'inspecteur.
///   Le pouce garde ses champs séparés comme en V7.
///
/// Trajectoire identique à V5/V6/V7, rien de changé.
/// </summary>
public class OuUnityPlayer_1m60_V8 : MonoBehaviour
{
    public enum TargetPositionSpace
    {
        LocalToTargetParent,
        World,
        RelativeToAvatarRoot
    }

    [Header("Cibles IK")]
    public Transform rightHandTarget;
    public Transform leftHandTarget;
    public Transform rightElbowHint;
    public Transform leftElbowHint;
    public Transform headTarget;

    [Header("Référence avatar")]
    public Transform avatarRoot;

    [Header("Espace des positions")]
    public TargetPositionSpace targetPositionSpace = TargetPositionSpace.RelativeToAvatarRoot;

    [Header("Lecture")]
    public bool playOnStart = true;
    public float duration = 1.8f;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement position globale")]
    public Vector3 positionOffset = Vector3.zero;

    [Header("Poignets")]
    public bool applyWristRotation = true;
    public Vector3 rightWristRotationEuler = new Vector3(-20f, 180f, 0f);
    public Vector3 leftWristRotationEuler  = new Vector3(-20f, 180f, 0f);
    public bool useLocalRotation = true;

    [Header("Doigts — activer")]
    public bool forceManualHandshape = true;

    // -----------------------------------------------------------------------
    // INDEX
    // -----------------------------------------------------------------------
    [Header("Index — Phalange 1")]
    public Vector3 indexPh1 = new Vector3(40f, -8f, 0f);
    [Header("Index — Phalange 2")]
    public Vector3 indexPh2 = new Vector3(20f, 0f, 0f);

    // -----------------------------------------------------------------------
    // MAJEUR
    // -----------------------------------------------------------------------
    [Header("Majeur — Phalange 1")]
    public Vector3 middlePh1 = new Vector3(40f, -8f, 0f);
    [Header("Majeur — Phalange 2")]
    public Vector3 middlePh2 = new Vector3(20f, 0f, 0f);

    // -----------------------------------------------------------------------
    // ANNULAIRE
    // -----------------------------------------------------------------------
    [Header("Annulaire — Phalange 1")]
    public Vector3 ringPh1 = new Vector3(40f, -8f, 0f);
    [Header("Annulaire — Phalange 2")]
    public Vector3 ringPh2 = new Vector3(20f, 0f, 0f);

    // -----------------------------------------------------------------------
    // AURICULAIRE
    // -----------------------------------------------------------------------
    [Header("Auriculaire — Phalange 1")]
    public Vector3 pinkyPh1 = new Vector3(40f, -8f, 0f);
    [Header("Auriculaire — Phalange 2")]
    public Vector3 pinkyPh2 = new Vector3(20f, 0f, 0f);

    // -----------------------------------------------------------------------
    // POUCE
    // -----------------------------------------------------------------------
    [Header("Pouce — Phalange 1")]
    public Vector3 thumbPh1Right = new Vector3(40f,  0f,  60f); // main droite
    public Vector3 thumbPh1Left  = new Vector3(40f,  0f, -60f); // main gauche (miroir Z)
    [Header("Pouce — Phalange 2")]
    public Vector3 thumbPh2 = new Vector3(20f, 0f, 0f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    // -----------------------------------------------------------------------
    // TRAJECTOIRE — identique V5/V6/V7
    // -----------------------------------------------------------------------
    private readonly Vector3[] rightHandPath = new Vector3[]
    {
        new Vector3( 0.12f, 0.95f, 0.22f),
        new Vector3( 0.20f, 0.95f, 0.22f),
        new Vector3( 0.10f, 0.95f, 0.22f),
        new Vector3( 0.20f, 0.95f, 0.22f),
        new Vector3( 0.10f, 0.95f, 0.22f),
        new Vector3( 0.20f, 0.95f, 0.22f),
        new Vector3( 0.10f, 0.95f, 0.22f),
        new Vector3( 0.20f, 0.95f, 0.22f),
        new Vector3( 0.12f, 0.95f, 0.22f),
    };

    private readonly Vector3[] leftHandPath = new Vector3[]
    {
        new Vector3(-0.12f, 0.95f, 0.22f),
        new Vector3(-0.20f, 0.95f, 0.22f),
        new Vector3(-0.10f, 0.95f, 0.22f),
        new Vector3(-0.20f, 0.95f, 0.22f),
        new Vector3(-0.10f, 0.95f, 0.22f),
        new Vector3(-0.20f, 0.95f, 0.22f),
        new Vector3(-0.10f, 0.95f, 0.22f),
        new Vector3(-0.20f, 0.95f, 0.22f),
        new Vector3(-0.12f, 0.95f, 0.22f),
    };

    private readonly Vector3 rightElbowPosition = new Vector3( 0.22f, 0.82f, 0.05f);
    private readonly Vector3 leftElbowPosition  = new Vector3(-0.22f, 0.82f, 0.05f);
    private readonly Vector3 gazeTarget = new Vector3(0.0f, 1.15f, 1.70f);
    private Coroutine animationCoroutine;

    // -----------------------------------------------------------------------

    private void Start()
    {
        if (playOnStart) PlaySign();
    }

    public void PlaySign()
    {
        if (animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateOuSign());
    }

    private IEnumerator AnimateOuSign()
    {
        if (showDebugLogs) Debug.Log("[OU V8] Démarrage.");

        ApplyElbowHints();

        float elapsed = 0f;
        float adjustedDuration = duration / Mathf.Max(0.1f, globalSpeedMultiplier);

        while (elapsed < adjustedDuration)
        {
            float t = elapsed / adjustedDuration;
            MoveHandTargets(EvaluatePath(rightHandPath, t), EvaluatePath(leftHandPath, t));
            if (applyWristRotation)   ApplyWristRotations();
            if (forceManualHandshape) UpdateFingerRotations();
            ApplyGaze();
            elapsed += Time.deltaTime;
            yield return null;
        }

        MoveHandTargets(
            rightHandPath[rightHandPath.Length - 1],
            leftHandPath[leftHandPath.Length  - 1]);
        if (applyWristRotation)   ApplyWristRotations();
        if (forceManualHandshape) UpdateFingerRotations();

        if (showDebugLogs) Debug.Log("[OU V8] Animation terminée.");
    }

    // -----------------------------------------------------------------------

    private void MoveHandTargets(Vector3 rightPos, Vector3 leftPos)
    {
        if (rightHandTarget != null)
            rightHandTarget.position = TransformPointSpace(CorrectPosition(rightPos));
        if (leftHandTarget != null)
            leftHandTarget.position  = TransformPointSpace(CorrectPosition(leftPos));
    }

    private void ApplyElbowHints()
    {
        if (rightElbowHint != null)
            rightElbowHint.position = TransformPointSpace(CorrectPosition(rightElbowPosition));
        if (leftElbowHint != null)
            leftElbowHint.position  = TransformPointSpace(CorrectPosition(leftElbowPosition));
    }

    private void ApplyWristRotations()
    {
        if (rightHandTarget != null)
        {
            Quaternion r = Quaternion.Euler(rightWristRotationEuler);
            if (useLocalRotation) rightHandTarget.localRotation = r;
            else                  rightHandTarget.rotation = r;
        }
        if (leftHandTarget != null)
        {
            Quaternion l = Quaternion.Euler(leftWristRotationEuler);
            if (useLocalRotation) leftHandTarget.localRotation = l;
            else                  leftHandTarget.rotation = l;
        }
    }

    // -----------------------------------------------------------------------
    // POSE DES DOIGTS V8 — chaque doigt indépendant
    // -----------------------------------------------------------------------
    private void UpdateFingerRotations()
    {
        if (avatarRoot == null) return;

        // ---- MAIN DROITE ----
        ApplyBone("mixamorig:RightHandIndex1",  Quaternion.Euler(indexPh1));
        ApplyBone("mixamorig:RightHandIndex2",  Quaternion.Euler(indexPh2));
        ApplyBone("mixamorig:RightHandMiddle1", Quaternion.Euler(middlePh1));
        ApplyBone("mixamorig:RightHandMiddle2", Quaternion.Euler(middlePh2));
        ApplyBone("mixamorig:RightHandRing1",   Quaternion.Euler(ringPh1));
        ApplyBone("mixamorig:RightHandRing2",   Quaternion.Euler(ringPh2));
        ApplyBone("mixamorig:RightHandPinky1",  Quaternion.Euler(pinkyPh1));
        ApplyBone("mixamorig:RightHandPinky2",  Quaternion.Euler(pinkyPh2));
        ApplyBone("mixamorig:RightHandThumb1",  Quaternion.Euler(thumbPh1Right));
        ApplyBone("mixamorig:RightHandThumb2",  Quaternion.Euler(thumbPh2));

        // ---- MAIN GAUCHE — miroir Y inversé pour les doigts ----
        ApplyBone("mixamorig:LeftHandIndex1",  Quaternion.Euler(MirrorY(indexPh1)));
        ApplyBone("mixamorig:LeftHandIndex2",  Quaternion.Euler(MirrorY(indexPh2)));
        ApplyBone("mixamorig:LeftHandMiddle1", Quaternion.Euler(MirrorY(middlePh1)));
        ApplyBone("mixamorig:LeftHandMiddle2", Quaternion.Euler(MirrorY(middlePh2)));
        ApplyBone("mixamorig:LeftHandRing1",   Quaternion.Euler(MirrorY(ringPh1)));
        ApplyBone("mixamorig:LeftHandRing2",   Quaternion.Euler(MirrorY(ringPh2)));
        ApplyBone("mixamorig:LeftHandPinky1",  Quaternion.Euler(MirrorY(pinkyPh1)));
        ApplyBone("mixamorig:LeftHandPinky2",  Quaternion.Euler(MirrorY(pinkyPh2)));
        ApplyBone("mixamorig:LeftHandThumb1",  Quaternion.Euler(thumbPh1Left));
        ApplyBone("mixamorig:LeftHandThumb2",  Quaternion.Euler(thumbPh2));
    }

    /// <summary>
    /// Inverse Y pour le miroir gauche/droite.
    /// </summary>
    private Vector3 MirrorY(Vector3 v) => new Vector3(v.x, -v.y, v.z);

    private void ApplyBone(string boneName, Quaternion rotation)
    {
        Transform bone = FindDeepChild(avatarRoot, boneName);
        if (bone != null) bone.localRotation = rotation;
    }

    // -----------------------------------------------------------------------

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

    private Vector3 EvaluatePath(Vector3[] path, float t)
    {
        if (path == null || path.Length == 0) return Vector3.zero;
        if (path.Length == 1) return path[0];
        float scaledT = Mathf.SmoothStep(0f, 1f, t) * (path.Length - 1);
        int index = Mathf.FloorToInt(scaledT);
        if (index >= path.Length - 1) return path[path.Length - 1];
        return Vector3.Lerp(path[index], path[index + 1], scaledT - index);
    }

    private Vector3 CorrectPosition(Vector3 pos) => pos + positionOffset;

    private Vector3 TransformPointSpace(Vector3 pos)
    {
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot != null)
            return avatarRoot.TransformPoint(pos);
        return pos;
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
