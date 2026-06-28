using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour le signe PAS en LSF.
/// Clonant scrupuleusement la structure et l'indépendance des variables de POSSIBLE.cs.
/// Seule la main droite est active : elle part du centre (entre poitrine et ventre) 
/// et effectue un mouvement rapide vers la droite avec l'index tendu et la paume face caméra.
/// </summary>
public class PAS : MonoBehaviour
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
    public float duration = 0.5f; // Geste décrit comme assez rapide
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Positions main droite")]
    [Tooltip("Départ : entre la poitrine et le ventre, plutôt vers le milieu")]
    public Vector3 rightHandStart = new Vector3(0.05f, 0.95f, 0.20f);
    [Tooltip("Arrivée : projection rapide vers la droite")]
    public Vector3 rightHandEnd = new Vector3(0.28f, 0.95f, 0.20f);
    public Vector3 rightElbowPos = new Vector3(0.35f, 0.80f, -0.05f);

    [Header("Poignets — Paume face caméra")]
    public bool applyWristRotation = true;
    [Tooltip("Ajusté pour que la paume regarde l'avant/caméra")]
    public Vector3 rightWristEuler = new Vector3(0f, 0f, 0f);
    public bool useLocalRotation = true;

    [Header("Doigts — Configuration manuelle")]
    public bool forceManualHandshape = true;

    // Configuration de la forme de la main (Index tendu, les autres fermés)
    [Header("Index DROITE — Tendu")]
    public Vector3 rightIndexPh1 = new Vector3(0f, 0f, 0f);
    public Vector3 rightIndexPh2 = new Vector3(0f, 0f, 0f);
    public Vector3 rightIndexPh3 = new Vector3(0f, 0f, 0f);

    [Header("Majeur DROITE — Replié")]
    public Vector3 rightMiddlePh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightMiddlePh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightMiddlePh3 = new Vector3(70f, 0f, 0f);

    [Header("Annulaire DROITE — Replié")]
    public Vector3 rightRingPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightRingPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightRingPh3 = new Vector3(70f, 0f, 0f);

    [Header("Auriculaire DROITE — Replié")]
    public Vector3 rightPinkyPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightPinkyPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightPinkyPh3 = new Vector3(70f, 0f, 0f);

    [Header("Pouce DROITE — Replié sur les autres doigts")]
    public Vector3 rightThumbPh1 = new Vector3(40f, 20f, 50f);
    public Vector3 rightThumbPh2 = new Vector3(40f, 0f, 30f);
    public Vector3 rightThumbPh3 = new Vector3(20f, 0f, 0f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayPas();
    }

    public void PlayPas()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayPasCoroutine());
    }

    private IEnumerator PlayPasCoroutine()
    {
        if (rightHandTarget == null) { Debug.LogError("[PAS] Target main droite non assigné."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[PAS] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[PAS] Démarrage du geste : Balayage de l'index vers la droite.");

        ApplyGaze();

        float safeSpeed = Mathf.Max(0.01f, globalSpeedMultiplier);
        float realDuration = duration / safeSpeed;

        // Positionnement initial au point de départ
        SetTargetPosition(rightHandTarget, CorrectPosition(rightHandStart));
        if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
        ApplyWristRotations();
        if (forceManualHandshape) UpdateFingerRotations();

        // Petite attente de stabilisation comme dans ton architecture
        yield return new WaitForSeconds(0.1f);

        // Mouvement rectiligne fluide (Lerp + SmoothStep) du centre vers la droite
        float elapsed = 0f;
        while (elapsed < realDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / realDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentRight = Vector3.Lerp(rightHandStart, rightHandEnd, smoothT);

            SetTargetPosition(rightHandTarget, CorrectPosition(currentRight));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
            ApplyWristRotations();
            if (forceManualHandshape) UpdateFingerRotations();

            yield return null;
        }

        // Maintien infini à la position finale pour permettre l'édition en temps réel
        while (true)
        {
            SetTargetPosition(rightHandTarget, CorrectPosition(rightHandEnd));
            if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
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
    }

    private void UpdateFingerRotations()
    {
        if (avatarRoot == null) return;

        // Application exclusive sur la main droite
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