using UnityEngine;

public class AvatarCustomizationManager : MonoBehaviour
{
    [Header("Avatar")]
    public Transform avatarRoot;

    [Header("Prefabs accessoires")]
    public GameObject frogHatPrefab;
    public GameObject catEarsPrefab;
    public GameObject glassesPrefab;
    public GameObject stethoscopePrefab;

    private GameObject currentFrogHat;
    private GameObject currentCatEars;
    private GameObject currentGlasses;
    private GameObject currentStethoscope;

    public void ToggleFrogHat()
    {
        if (currentFrogHat != null)
        {
            Destroy(currentFrogHat);
            currentFrogHat = null;
        }
        else
        {
            currentFrogHat = AttachAccessory(
                frogHatPrefab,
                "mixamorig:Head",
                new Vector3(-0.008f, 0.123f, 0.061f),
                new Vector3(9.205f, -9.235f, -2.646f),
                new Vector3(1.3659f, 1.1543f, 1.17578f)
            );
        }
    }

    public void ToggleCatEars()
    {
        if (currentCatEars != null)
        {
            Destroy(currentCatEars);
            currentCatEars = null;
        }
        else
        {
            currentCatEars = AttachAccessory(
                catEarsPrefab,
                "mixamorig:Head",
                new Vector3(-0.005f, 0.094f, 0.053f),
                new Vector3(0.012f, -10.00f, 0.114f),
                new Vector3(-0.1466f, 0.1850f, 0.1293f)
            );
        }
    }

    public void ToggleGlasses()
    {
        if (currentGlasses != null)
        {
            Destroy(currentGlasses);
            currentGlasses = null;
        }
        else
        {
            currentGlasses = AttachAccessory(
                glassesPrefab,
                "mixamorig:Head",
                new Vector3(-0.003f, 0.074f, 0.1183f),
                new Vector3(0.002f, 176.85f, -0.114f),
                new Vector3(0.449f, 1.2237f, 12.157f)
            );
        }
    }

    public void ToggleStethoscope()
    {
        if (currentStethoscope != null)
        {
            Destroy(currentStethoscope);
            currentStethoscope = null;
        }
        else
        {
            currentStethoscope = AttachAccessory(
                stethoscopePrefab,
                "mixamorig:Neck",
                new Vector3(-0.045f, -0.206f, 0.18f),
                new Vector3(-31.73f, -8.333f, 0.134f),
                new Vector3(0.0102f, 0.0089f, 0.01456f)
            );
        }
    }

    public void RemoveAllAccessories()
    {
        if (currentFrogHat != null) Destroy(currentFrogHat);
        if (currentCatEars != null) Destroy(currentCatEars);
        if (currentGlasses != null) Destroy(currentGlasses);
        if (currentStethoscope != null) Destroy(currentStethoscope);

        currentFrogHat = null;
        currentCatEars = null;
        currentGlasses = null;
        currentStethoscope = null;
    }

    private GameObject AttachAccessory(
        GameObject prefab,
        string boneName,
        Vector3 localPosition,
        Vector3 localRotation,
        Vector3 localScale
    )
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab accessoire non assigné !");
            return null;
        }

        if (avatarRoot == null)
        {
            Debug.LogError("Avatar Root n'est pas assigné !");
            return null;
        }

        Transform bone = FindChildByName(avatarRoot, boneName);

        if (bone == null)
        {
            Debug.LogError("Os introuvable : " + boneName);
            return null;
        }

        GameObject accessory = Instantiate(prefab, bone);

        accessory.transform.localPosition = localPosition;
        accessory.transform.localEulerAngles = localRotation;
        accessory.transform.localScale = localScale;

        return accessory;
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }
}