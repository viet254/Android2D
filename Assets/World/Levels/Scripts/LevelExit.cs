using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
[DisallowMultipleComponent]
public sealed class LevelExit : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private GameObject interactionPrompt;

    [Header("Activation Animation")]
    [SerializeField] private Animator portalAnimator;
    [SerializeField] private string activationStateName = "Teleport";
    [SerializeField, Min(0.5f)] private float activationTimeout = 3f;

    private readonly HashSet<Collider2D> playerColliders = new HashSet<Collider2D>();
    private PlayerController playerInRange;
    private SpriteRenderer portalRenderer;
    private Sprite idleSprite;
    private Coroutine activationRoutine;
    private bool isTriggered;

    public bool CanInteract => playerInRange != null && !isTriggered;

    private void Awake()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger != null && !trigger.isTrigger)
            Debug.LogError("[LevelExit] Collider2D must be configured as a trigger.", this);

        if (levelManager == null)
            levelManager = FindAnyObjectByType<LevelManager>();

        if (portalAnimator == null)
            portalAnimator = GetComponent<Animator>();
        portalRenderer = GetComponent<SpriteRenderer>();
        idleSprite = portalRenderer != null ? portalRenderer.sprite : null;

        if (portalAnimator != null)
            portalAnimator.enabled = false;

        SetPromptVisible(false);
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    private void Update()
    {
        if (CanInteract && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            Interact();
    }
#endif

    public void Interact()
    {
        if (!CanInteract)
            return;

        if (levelManager == null)
        {
            Debug.LogError("[LevelExit] No LevelManager is assigned or active.", this);
            return;
        }

        isTriggered = true;
        SetPromptVisible(false);
        activationRoutine = StartCoroutine(PlayActivationAndLoad());
    }

    private IEnumerator PlayActivationAndLoad()
    {
        if (portalAnimator != null && !string.IsNullOrWhiteSpace(activationStateName))
        {
            int shortStateHash = Animator.StringToHash(activationStateName);
            int fullStateHash = Animator.StringToHash($"Base Layer.{activationStateName}");
            int playableStateHash = portalAnimator.HasState(0, shortStateHash)
                ? shortStateHash
                : fullStateHash;
            if (portalAnimator.HasState(0, playableStateHash))
            {
                portalAnimator.enabled = true;
                portalAnimator.Play(playableStateHash, 0, 0f);
                portalAnimator.Update(0f);

                float deadline = Time.unscaledTime + Mathf.Max(0.5f, activationTimeout);
                while (Time.unscaledTime < deadline)
                {
                    AnimatorStateInfo state = portalAnimator.GetCurrentAnimatorStateInfo(0);
                    if ((state.shortNameHash == shortStateHash || state.fullPathHash == fullStateHash)
                        && state.normalizedTime >= 1f)
                    {
                        break;
                    }

                    yield return null;
                }
            }
            else
            {
                Debug.LogError(
                    $"[LevelExit] Animator has no state named '{activationStateName}' on Base Layer.",
                    this);
            }
        }
        else
        {
            Debug.LogWarning("[LevelExit] No portal Animator or activation state is configured.", this);
        }

        activationRoutine = null;
        if (levelManager.LoadNextLevel())
            yield break;

        ResetAfterFailedTransition();
    }

    private void ResetAfterFailedTransition()
    {
        isTriggered = false;
        if (portalAnimator != null)
            portalAnimator.enabled = false;
        if (portalRenderer != null && idleSprite != null)
            portalRenderer.sprite = idleSprite;
        SetPromptVisible(playerInRange != null);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || (playerInRange != null && playerInRange != player))
            return;

        playerInRange = player;
        playerColliders.Add(other);
        SetPromptVisible(!isTriggered);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!playerColliders.Remove(other) || playerColliders.Count > 0)
            return;

        playerInRange = null;
        SetPromptVisible(false);
    }

    private void OnDisable()
    {
        if (activationRoutine != null)
            StopCoroutine(activationRoutine);
        activationRoutine = null;
        playerColliders.Clear();
        playerInRange = null;
        isTriggered = false;
        if (portalAnimator != null)
            portalAnimator.enabled = false;
        SetPromptVisible(false);
    }

    private void OnValidate()
    {
        activationTimeout = Mathf.Max(0.5f, activationTimeout);
        if (portalAnimator == null)
            portalAnimator = GetComponent<Animator>();
    }

    private void SetPromptVisible(bool visible)
    {
        if (interactionPrompt != null && interactionPrompt.activeSelf != visible)
            interactionPrompt.SetActive(visible);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.35f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, 0f));
    }
}
