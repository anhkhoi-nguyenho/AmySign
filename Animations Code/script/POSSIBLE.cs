using System.Collections;
using UnityEngine;

/// <summary>
/// Script Unity pour le signe POSSIBLE en LSF.
/// Deux mains en poing, paumes vers le sol, bras en diagonale.
/// Les mains descendent et remontent deux fois (mouvement de tape vers le centre).
/// </summary>
public class POSSIBLE : MonoBehaviour
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
    public float duration = 1.6f;
    public float globalSpeedMultiplier = 1.0f;

    [Header("Ajustement global")]
    public Vector3 positionOffset = Vector3.zero;
    public float positionScale = 1.0f;

    [Header("Positions main droite")]
    [Tooltip("Position haute (départ et entre les deux tapes)")]
    public Vector3 rightHandHigh = new Vector3(0.13f, 1.05f, 0.20f);
    [Tooltip("Position basse (rapprochée du centre)")]
    public Vector3 rightHandLow = new Vector3(0.10f, 0.95f, 0.22f);
    public Vector3 rightElbowPos = new Vector3(0.32f, 0.85f, 0.00f);

    [Header("Positions main gauche (symétrique)")]
    public Vector3 leftHandHigh = new Vector3(-0.13f, 1.05f, 0.20f);
    public Vector3 leftHandLow = new Vector3(-0.10f, 0.95f, 0.22f);
    public Vector3 leftElbowPos = new Vector3(-0.32f, 0.85f, 0.00f);

    [Header("Poignets — paumes vers le sol, inclinés vers le milieu")]
    public bool applyWristRotation = true;
    public Vector3 rightWristEuler = new Vector3(-90f, -180f, 20f);
    public Vector3 leftWristEuler = new Vector3(-90f, 0f, -20f);
    public bool useLocalRotation = true;

    [Header("Doigts — poing fermé")]
    public bool forceManualHandshape = true;

    [Header("Index DROITE — replié")]
    public Vector3 rightIndexPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightIndexPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightIndexPh3 = new Vector3(70f, 0f, 0f);

    [Header("Index GAUCHE — replié (inversé)")]
    public Vector3 leftIndexPh1 = new Vector3(-80f, 0f, 0f);
    public Vector3 leftIndexPh2 = new Vector3(-90f, 0f, 0f);
    public Vector3 leftIndexPh3 = new Vector3(-70f, 0f, 0f);

    [Header("Majeur DROITE — replié")]
    public Vector3 rightMiddlePh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightMiddlePh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightMiddlePh3 = new Vector3(70f, 0f, 0f);

    [Header("Majeur GAUCHE — replié (inversé)")]
    public Vector3 leftMiddlePh1 = new Vector3(-80f, 0f, 0f);
    public Vector3 leftMiddlePh2 = new Vector3(-90f, 0f, 0f);
    public Vector3 leftMiddlePh3 = new Vector3(-70f, 0f, 0f);

    [Header("Annulaire DROITE — replié")]
    public Vector3 rightRingPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightRingPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightRingPh3 = new Vector3(70f, 0f, 0f);

    [Header("Annulaire GAUCHE — replié (inversé)")]
    public Vector3 leftRingPh1 = new Vector3(-80f, 0f, 0f);
    public Vector3 leftRingPh2 = new Vector3(-90f, 0f, 0f);
    public Vector3 leftRingPh3 = new Vector3(-70f, 0f, 0f);

    [Header("Auriculaire DROITE — replié")]
    public Vector3 rightPinkyPh1 = new Vector3(80f, 0f, 0f);
    public Vector3 rightPinkyPh2 = new Vector3(90f, 0f, 0f);
    public Vector3 rightPinkyPh3 = new Vector3(70f, 0f, 0f);

    [Header("Auriculaire GAUCHE — replié (inversé)")]
    public Vector3 leftPinkyPh1 = new Vector3(-80f, 0f, 0f);
    public Vector3 leftPinkyPh2 = new Vector3(-90f, 0f, 0f);
    public Vector3 leftPinkyPh3 = new Vector3(-70f, 0f, 0f);

    [Header("Pouce DROITE — posé sur les doigts repliés")]
    public Vector3 rightThumbPh1 = new Vector3(40f, 20f, 50f);
    public Vector3 rightThumbPh2 = new Vector3(40f, 0f, 30f);
    public Vector3 rightThumbPh3 = new Vector3(20f, 0f, 0f);

    [Header("Pouce GAUCHE — miroir")]
    public Vector3 leftThumbPh1 = new Vector3(-40f, -20f, -50f);
    public Vector3 leftThumbPh2 = new Vector3(-40f, 0f, -30f);
    public Vector3 leftThumbPh3 = new Vector3(-20f, 0f, 0f);

    [Header("Regard")]
    public Vector3 gazeTarget = new Vector3(0f, 1.15f, 1.7f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine currentCoroutine;

    private void Start()
    {
        if (playOnStart) PlayPossible();
    }

    public void PlayPossible()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(PlayPossibleCoroutine());
    }

    private IEnumerator PlayPossibleCoroutine()
    {
        if (rightHandTarget == null || leftHandTarget == null) { Debug.LogError("[POSSIBLE] Targets non assignés."); yield break; }
        if (targetPositionSpace == TargetPositionSpace.RelativeToAvatarRoot && avatarRoot == null) { Debug.LogError("[POSSIBLE] AvatarRoot requis."); yield break; }

        if (showDebugLogs) Debug.Log("[POSSIBLE] Démarrage : 2 tapes vers le centre.");

        ApplyGaze();

        Vector3[] rightPath = new Vector3[] { rightHandHigh, rightHandLow, rightHandHigh, rightHandLow };
        Vector3[] leftPath  = new Vector3[] { leftHandHigh,  leftHandLow,  leftHandHigh,  leftHandLow  };

        int segmentCount = rightPath.Length - 1;
        float safeSpeed = Mathf.Max(0.01f, globalSpeedMultiplier);
        float realDuration = duration / safeSpeed;
        float segmentDuration = realDuration / segmentCount;

        SetTargetPosition(rightHandTarget, CorrectPosition(rightPath[0]));
        SetTargetPosition(leftHandTarget, CorrectPosition(leftPath[0]));
        if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
        if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));
        ApplyWristRotations();
        if (forceManualHandshape) UpdateFingerRotations();

        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 rightStart = rightPath[i];
            Vector3 rightEnd = rightPath[i + 1];
            Vector3 leftStart = leftPath[i];
            Vector3 leftEnd = leftPath[i + 1];

            if (showDebugLogs)
            {
                string phaseName = (i % 2 == 0) ? "descente" : "remontée";
                Debug.Log("[POSSIBLE] Phase " + (i + 1) + " : " + phaseName);
            }

            float elapsed = 0f;
            while (elapsed < segmentDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / segmentDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                Vector3 currentRight = Vector3.Lerp(rightStart, rightEnd, smoothT);
                Vector3 currentLeft = Vector3.Lerp(leftStart, leftEnd, smoothT);

                SetTargetPosition(rightHandTarget, CorrectPosition(currentRight));
                SetTargetPosition(leftHandTarget, CorrectPosition(currentLeft));
                if (rightElbowHint != null) SetTargetPosition(rightElbowHint, CorrectPosition(rightElbowPos));
                if (leftElbowHint != null) SetTargetPosition(leftElbowHint, CorrectPosition(leftElbowPos));
                ApplyWristRotations();
                if (forceManualHandshape) UpdateFingerRotations();

                yield return null;
            }
        }

        while (true)
        {
            SetTargetPosition(rightHandTarget, CorrectPosition(rightHandLow));
            SetTargetPosition(leftHandTarget, CorrectPosition(leftHandLow));
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

        // Main gauche (valeurs séparées)
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
