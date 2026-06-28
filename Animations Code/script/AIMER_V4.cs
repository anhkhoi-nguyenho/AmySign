using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity V4 pour le signe AIMER en LSF.
/// Corrections :
///   1. Retour du système de bascule de poignet (StartEuler -> EndEuler) pour que la paume se tourne vers le ciel à la fin.
///   2. Pouce corrigé à Vector3.zero pour rester totalement tendu et ouvert comme les autres doigts.
///   3. Maintien des coordonnées de hauteur/profondeur pour éviter d'entrer dans le ventre.
/// </summary>
public class AIMER : MonoBehaviour
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

    [Header("Cibles IK gauche (Inutile pour ce signe)")]
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
    public float duration = 1.0f;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Positions main droite")]
    [Tooltip("Départ : Posée contre le torse (hauteur et profondeur ajustées pour ne pas rentrer dans le corps)")]
    public Vector3 rightHandStart = new Vector3(0.06f, 1.02f, 0.18f);
    [Tooltip("Arrivée : Avancée vers l'avant en offrande")]
    public Vector3 rightHandEnd = new Vector3(0.14f, 1.08f, 0.35f);
    public Vector3 rightElbowPos = new Vector3(0.32f, 0.80f, 0.02f);

    [Header("Poignet — Rotation et Bascule (Paume Torse -> Paume Ciel)")]
    public bool applyWristRotation = true;
    [Tooltip("Rotation de départ : Paume orientée vers le torse, main en diagonale")]
    public Vector3 rightWristStartEuler = new Vector3(45f, 180f, -30f);
    [Tooltip("Rotation d'arrivée : Paume orientée vers le ciel, tout en maintenant la diagonale du bras")]
    public Vector3 rightWristEndEuler = new Vector3(-30f, 210f, -40f);
    public bool useLocalRotation = true;

    [Header("Doigts — Configuration manuelle (Tous Ouverts, Tendus & Espacés)")]
    public bool forceManualHandshape = true;

    [Header("Index DROITE — Tendu et ouvert")]
    public Vector3 rightIndexPh1 = Vector3.zero;
    public Vector3 rightIndexPh2 = Vector3.zero;
    public Vector3 rightIndexPh3 = Vector3.zero;

    [Header("Majeur DROITE — Tendu et ouvert")]
    public Vector3 rightMiddlePh1 = Vector3.zero;
    public Vector3 rightMiddlePh2 = Vector3.zero;
    public Vector3 rightMiddlePh3 = Vector3.zero;

    [Header("Annulaire DROITE — Tendu et ouvert")]
    public Vector3 rightRingPh1 = Vector3.zero;
    public Vector3 rightRingPh2 = Vector3.zero;
    public Vector3 rightRingPh3 = Vector3.zero;

    [Header("Auriculaire DROITE — Tendu et ouvert")]
    public Vector3 rightPinkyPh1 = Vector3.zero;
    public Vector3 rightPinkyPh2 = Vector3.zero;
    public Vector3 rightPinkyPh3 = Vector3.zero;

    [Header("Pouce DROITE — Entièrement TENDU et OUVERT (Correction)")]
    public Vector3 rightThumbPh1 = Vector3.zero; // Corrigé à zero pour qu'il ne se renferme pas
    public Vector3 rightThumbPh2 = Vector3.zero;
    public Vector3 rightThumbPh3 = Vector3.zero;

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayAimer();
    }

    public void PlayAimer()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayAimerCoroutine());
    }

    private IEnumerator PlayAimerCoroutine()
    {
        if (rightHandTarget == null) { Debug.LogError("[AIMER] Target main droite non assigné."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[AIMER] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[AIMER] Démarrage V4 : Bascule du poignet réactivée, pouce redressé.");

        ApplyGaze();

        float safeSpeed = Mathf.Max(0.01f, globalSpeedMultiplier);
        float realDuration = duration / safeSpeed;

        // Positionnement initial sur le torse
        SetTargetPosition(rightHandTarget, CorrectPosition(rightHandStart));
        if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
        if (applyWristRotation) SetWristRotation(rightWristStartEuler);
        if (forceManualHandshape) UpdateFingerRotations();

        yield return new WaitForSeconds(0.15f);

        // Glissement vers l'avant avec rotation progressive (Slerp)
        float elapsed = 0f;
        while (elapsed < realDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / realDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentRight = Vector3.Lerp(rightHandStart, rightHandEnd, smoothT);

            SetTargetPosition(rightHandTarget, CorrectPosition(currentRight));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            
            // La paume bascule progressivement vers la position finale ciel
            if (applyWristRotation && rightHandTarget != null)
            {
                Quaternion r = Quaternion.Slerp(Quaternion.Euler(rightWristStartEuler), Quaternion.Euler(rightWristEndEuler), smoothT);
                if (useLocalRotation) rightHandTarget.localRotation = r;
                else                  rightHandTarget.rotation = r;
            }

            if (forceManualHandshape) UpdateFingerRotations();

            yield return null;
        }

        // Maintien infini à la position finale (paume ciel)
        while (true)
        {
            SetTargetPosition(rightHandTarget, CorrectPosition(rightHandEnd));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            if (applyWristRotation) SetWristRotation(rightWristEndEuler);
            if (forceManualHandshape) UpdateFingerRotations();
            yield return null;
        }
    }

    private void SetWristRotation(Vector3 angles)
    {
        if (rightHandTarget == null) return;
        Quaternion r = Quaternion.Euler(angles);
        if (useLocalRotation) rightHandTarget.localRotation = r;
        else                  rightHandTarget.rotation = r;
    }

    private void UpdateFingerRotations()
    {
        if (avatarRoot == null) return;

        // Tous les doigts (y compris le pouce) appliquent leurs valeurs de l'Inspecteur
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