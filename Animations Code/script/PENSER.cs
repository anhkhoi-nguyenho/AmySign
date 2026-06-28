using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour le signe PENSER en LSF.
/// Clonant scrupuleusement la structure, l'indépendance des variables (droite/gauche), 
/// et le traitement de la tête/regard du script fonctionnel POSSIBLE.cs.
/// </summary>
public class PENSER : MonoBehaviour
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
    public float duration = 1.2f;
    public float globalSpeedMultiplier = 1.0f;
    [Tooltip("Durée flash de la transition du Boom vers la gauche")]
    public float transitionToPhase2Duration = 0.15f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Positions main droite")]
    [Tooltip("Partie 1 : Écartée de la tempe droite")]
    public Vector3 rightHandP1 = new Vector3(0.18f, 1.42f, 0.15f);
    [Tooltip("Partie 2 : Devant à gauche après l'explosion")]
    public Vector3 rightHandP2 = new Vector3(-0.05f, 1.30f, 0.28f);
    public Vector3 rightElbowPos = new Vector3(0.38f, 1.15f, 0.05f);

    [Header("Positions main gauche")]
    [Tooltip("Partie 1 : Touche la tempe gauche")]
    public Vector3 leftHandP1 = new Vector3(-0.14f, 1.42f, 0.10f);
    [Tooltip("Partie 2 : Devant à gauche après l'explosion")]
    public Vector3 leftHandP2 = new Vector3(-0.18f, 1.30f, 0.28f);
    public Vector3 leftElbowPos = new Vector3(-0.35f, 1.15f, 0.05f);

    [Header("Poignets — Partie 1 (Aux Tempes)")]
    public bool applyWristRotation = true;
    [Tooltip("Paume vers la caméra")]
    public Vector3 rightWristEulerP1 = new Vector3(0f, 0f, 0f);
    [Tooltip("Paume vers l'avatar")]
    public Vector3 leftWristEulerP1 = new Vector3(0f, 180f, 0f);
    public bool useLocalRotation = true;

    [Header("Poignets — Partie 2 (Projection Face à Face)")]
    [Tooltip("Paume face à la main gauche")]
    public Vector3 rightWristEulerP2 = new Vector3(0f, 270f, 0f);
    [Tooltip("Paume face à la main droite")]
    public Vector3 leftWristEulerP2 = new Vector3(0f, 90f, 0f);

    [Header("Doigts — Contrôle manuel obligatoire")]
    public bool forceManualHandshape = true;

    // =======================================================================
    // VARIABLES DES DOIGTS INDÉPENDANTES (REPRISES DE POSSIBLE.CS)
    // =======================================================================
    [Header("Index DROITE")]
    public Vector3 rightIndexPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightIndexPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightIndexPh3 = new Vector3(70f, 0f, 0f);

    [Header("Index GAUCHE")]
    public Vector3 leftIndexPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 leftIndexPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 leftIndexPh3 = new Vector3(70f, 0f, 0f);

    [Header("Majeur DROITE")]
    public Vector3 rightMiddlePh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightMiddlePh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightMiddlePh3 = new Vector3(70f, 0f, 0f);

    [Header("Majeur GAUCHE")]
    public Vector3 leftMiddlePh1 = new Vector3(80f, 0f, 0f);
    public Vector3 leftMiddlePh2 = new Vector3(90f, 0f, 0f);
    public Vector3 leftMiddlePh3 = new Vector3(70f, 0f, 0f);

    [Header("Annulaire DROITE")]
    public Vector3 rightRingPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightRingPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightRingPh3 = new Vector3(70f, 0f, 0f);

    [Header("Annulaire GAUCHE")]
    public Vector3 leftRingPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 leftRingPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 leftRingPh3 = new Vector3(70f, 0f, 0f);

    [Header("Auriculaire DROITE")]
    public Vector3 rightPinkyPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightPinkyPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightPinkyPh3 = new Vector3(70f, 0f, 0f);

    [Header("Auriculaire GAUCHE")]
    public Vector3 leftPinkyPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 leftPinkyPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 leftPinkyPh3 = new Vector3(70f, 0f, 0f);

    [Header("Pouce DROITE")]
    public Vector3 rightThumbPh1 = new Vector3(0f, -30f, 30f);
    public Vector3 rightThumbPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 rightThumbPh3 = new Vector3(0f, 0f, 0f);

    [Header("Pouce GAUCHE")]
    public Vector3 leftThumbPh1 = new Vector3(0f, -30f, 30f);
    public Vector3 leftThumbPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 leftThumbPh3 = new Vector3(0f, 0f, 0f);

    // Variables pour la Partie 2 (Ouvertes et courbées)
    [Header("--- PARAMÈTRES PARTIE 2 (Doigts Ouverts/Courbés) ---")]
    public Vector3 openCurvedPh1 = new Vector3(25f, 0f, 0f);
    public Vector3 openCurvedPh2 = new Vector3(15f, 0f, 0f);
    public Vector3 openCurvedPh3 = new Vector3(10f, 0f, 0f);
    public Vector3 openThumbPh1   = new Vector3(10f, -10f, 10f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayPenser();
    }

    public void PlayPenser()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayPenserCoroutine());
    }

    private IEnumerator PlayPenserCoroutine()
    {
        if (rightHandTarget == null || leftHandTarget == null) { Debug.LogError("[PENSER] Targets non assignés."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[PENSER] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[PENSER] Démarrage du geste.");

        ApplyGaze();

        float safeSpeed = Mathf.Max(0.01f, globalSpeedMultiplier);
        float realDuration = duration / safeSpeed;
        float p1Duration = realDuration * 0.4f; // Temps d'attente/maintien aux tempes

        // --- ÉTAPE 1 : Exécution et maintien de la Partie 1 (Poings aux tempes) ---
        float elapsedP1 = 0f;
        while (elapsedP1 < p1Duration)
        {
            elapsedP1 += Time.deltaTime;

            SetTargetPosition(rightHandTarget, CorrectPosition(rightHandP1));
            SetTargetPosition(leftHandTarget, CorrectPosition(leftHandP1));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));

            ApplyWristRotations(true);
            if (forceManualHandshape) UpdateFingerRotations(true, 0f);

            yield return null;
        }

        if (showDebugLogs) Debug.Log("[PENSER] BOOM ! Déplacement flash et ouverture à gauche.");

        // --- ÉTAPE 2 : Transition explosive (Lerp) vers la Partie 2 ---
        float elapsedTransition = 0f;
        while (elapsedTransition < transitionToPhase2Duration)
        {
            elapsedTransition += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTransition / transitionToPhase2Duration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentRight = Vector3.Lerp(rightHandP1, rightHandP2, smoothT);
            Vector3 currentLeft = Vector3.Lerp(leftHandP1, leftHandP2, smoothT);

            SetTargetPosition(rightHandTarget, CorrectPosition(currentRight));
            SetTargetPosition(leftHandTarget, CorrectPosition(currentLeft));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));

            // Slerp des poignets
            if (applyWristRotation)
            {
                if (rightHandTarget != null) rightHandTarget.localRotation = Quaternion.Slerp(Quaternion.Euler(rightWristEulerP1), Quaternion.Euler(rightWristEulerP2), smoothT);
                if (leftHandTarget != null) leftHandTarget.localRotation = Quaternion.Slerp(Quaternion.Euler(leftWristEulerP1), Quaternion.Euler(leftWristEulerP2), smoothT);
            }

            if (forceManualHandshape) UpdateFingerRotations(false, smoothT);

            yield return null;
        }

        // --- ÉTAPE 3 : Maintien continu permanent (Pour édition dynamique comme dans POSSIBLE.cs) ---
        while (true)
        {
            SetTargetPosition(rightHandTarget, CorrectPosition(rightHandP2));
            SetTargetPosition(leftHandTarget, CorrectPosition(leftHandP2));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));

            ApplyWristRotations(false);
            if (forceManualHandshape) UpdateFingerRotations(false, 1f);

            yield return null;
        }
    }

    private void ApplyWristRotations(bool isPartie1)
    {
        if (!applyWristRotation) return;

        Vector3 rightRot = isPartie1 ? rightWristEulerP1 : rightWristEulerP2;
        Vector3 leftRot = isPartie1 ? leftWristEulerP1 : leftWristEulerP2;

        if (rightHandTarget != null)
        {
            Quaternion r = Quaternion.Euler(rightRot);
            if (useLocalRotation) rightHandTarget.localRotation = r;
            else                  rightHandTarget.rotation = r;
        }
        if (leftHandTarget != null)
        {
            Quaternion l = Quaternion.Euler(leftRot);
            if (useLocalRotation) leftHandTarget.localRotation = l;
            else                  leftHandTarget.rotation = l;
        }
    }

    private void UpdateFingerRotations(bool isPartie1, float interpolationFactor)
    {
        if (avatarRoot == null) return;

        if (isPartie1)
        {
            // Main Droite (Valeurs de l'Inspecteur Partie 1)
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

            // Main Gauche (Valeurs de l'Inspecteur Partie 1)
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
        else
        {
            // Mode Transition / Maintien Partie 2 (Ouverture progressive vers les valeurs de courbure)
            Vector3 targetPh1 = Vector3.Lerp(rightIndexPh1, openCurvedPh1, interpolationFactor);
            Vector3 targetPh2 = Vector3.Lerp(rightIndexPh2, openCurvedPh2, interpolationFactor);
            Vector3 targetPh3 = Vector3.Lerp(rightIndexPh3, openCurvedPh3, interpolationFactor);
            Vector3 targetThumb = Vector3.Lerp(rightThumbPh1, openThumbPh1, interpolationFactor);

            // Application Droite
            ApplyBone("mixamorig:RightHandIndex1", Quaternion.Euler(targetPh1));
            ApplyBone("mixamorig:RightHandIndex2", Quaternion.Euler(targetPh2));
            ApplyBone("mixamorig:RightHandIndex3", Quaternion.Euler(targetPh3));
            ApplyBone("mixamorig:RightHandMiddle1", Quaternion.Euler(targetPh1));
            ApplyBone("mixamorig:RightHandMiddle2", Quaternion.Euler(targetPh2));
            ApplyBone("mixamorig:RightHandMiddle3", Quaternion.Euler(targetPh3));
            ApplyBone("mixamorig:RightHandRing1", Quaternion.Euler(targetPh1));
            ApplyBone("mixamorig:RightHandRing2", Quaternion.Euler(targetPh2));
            ApplyBone("mixamorig:RightHandRing3", Quaternion.Euler(targetPh3));
            ApplyBone("mixamorig:RightHandPinky1", Quaternion.Euler(targetPh1));
            ApplyBone("mixamorig:RightHandPinky2", Quaternion.Euler(targetPh2));
            ApplyBone("mixamorig:RightHandPinky3", Quaternion.Euler(targetPh3));
            ApplyBone("mixamorig:RightHandThumb1", Quaternion.Euler(targetThumb));
            ApplyBone("mixamorig:RightHandThumb2", Quaternion.Euler(Vector3.Lerp(rightThumbPh2, openCurvedPh2, interpolationFactor)));
            ApplyBone("mixamorig:RightHandThumb3", Quaternion.Euler(Vector3.Lerp(rightThumbPh3, openCurvedPh3, interpolationFactor)));

            // Application Gauche (Indépendante)
            Vector3 targetLeftPh1 = Vector3.Lerp(leftIndexPh1, openCurvedPh1, interpolationFactor);
            Vector3 targetLeftPh2 = Vector3.Lerp(leftIndexPh2, openCurvedPh2, interpolationFactor);
            Vector3 targetLeftPh3 = Vector3.Lerp(leftIndexPh3, openCurvedPh3, interpolationFactor);
            Vector3 targetLeftThumb = Vector3.Lerp(leftThumbPh1, openThumbPh1, interpolationFactor);

            ApplyBone("mixamorig:LeftHandIndex1", Quaternion.Euler(targetLeftPh1));
            ApplyBone("mixamorig:LeftHandIndex2", Quaternion.Euler(targetLeftPh2));
            ApplyBone("mixamorig:LeftHandIndex3", Quaternion.Euler(targetLeftPh3));
            ApplyBone("mixamorig:LeftHandMiddle1", Quaternion.Euler(targetLeftPh1));
            ApplyBone("mixamorig:LeftHandMiddle2", Quaternion.Euler(targetLeftPh2));
            ApplyBone("mixamorig:LeftHandMiddle3", Quaternion.Euler(targetLeftPh3));
            ApplyBone("mixamorig:LeftHandRing1", Quaternion.Euler(targetLeftPh1));
            ApplyBone("mixamorig:LeftHandRing2", Quaternion.Euler(targetLeftPh2));
            ApplyBone("mixamorig:LeftHandRing3", Quaternion.Euler(targetLeftPh3));
            ApplyBone("mixamorig:LeftHandPinky1", Quaternion.Euler(targetLeftPh1));
            ApplyBone("mixamorig:LeftHandPinky2", Quaternion.Euler(targetLeftPh2));
            ApplyBone("mixamorig:LeftHandPinky3", Quaternion.Euler(targetLeftPh3));
            ApplyBone("mixamorig:LeftHandThumb1", Quaternion.Euler(targetLeftThumb));
            ApplyBone("mixamorig:LeftHandThumb2", Quaternion.Euler(Vector3.Lerp(leftThumbPh2, openCurvedPh2, interpolationFactor)));
            ApplyBone("mixamorig:LeftHandThumb3", Quaternion.Euler(Vector3.Lerp(leftThumbPh3, openCurvedPh3, interpolationFactor)));
        }
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