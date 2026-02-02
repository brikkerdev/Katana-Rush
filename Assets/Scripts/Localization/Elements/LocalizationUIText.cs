using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizationUIText : MonoBehaviour
{
    [SerializeField] private string key;

    private TextMeshProUGUI textComponent;
    private LocalizationController controller;
    private bool isSubscribed;

    public string Key
    {
        get => key;
        set
        {
            key = value;
            Refresh();
        }
    }

    void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        // Start() runs after ALL Awake() calls are complete
        // This guarantees Singleton is initialized
        Initialize();
    }

    void OnEnable()
    {
        // Handle re-enabling after Start has already run
        if (controller != null)
        {
            Subscribe();
            Refresh();
        }
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    void Initialize()
    {
        controller = LocalizationController.Singleton;

        if (controller == null)
        {
            Debug.LogWarning($"[LocalizationUIText] Controller not found! Key: '{key}' on {gameObject.name}", this);
            textComponent.text = key;
            return;
        }

        Subscribe();
        Refresh();
    }

    void Subscribe()
    {
        if (isSubscribed || controller == null) return;

        controller.LocalizationChangedEvent += Refresh;
        isSubscribed = true;
    }

    void Unsubscribe()
    {
        if (!isSubscribed || controller == null) return;

        controller.LocalizationChangedEvent -= Refresh;
        isSubscribed = false;
    }

    public void Refresh()
    {
        if (textComponent == null) return;

        if (controller == null)
        {
            controller = LocalizationController.Singleton;
        }

        textComponent.text = controller != null
            ? controller.GetText(key)
            : key;
    }
}