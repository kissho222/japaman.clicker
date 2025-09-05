using UnityEngine;
using System.Collections;

public class CharacterSwallowAnimator : MonoBehaviour
{
    [Header("���ݍ��݃A�j���[�V�����ݒ�")]
    public float swallowIntensity = 20f;        // �㉺�̗h��̋���
    public float swallowSpeed = 3f;             // ���ݍ��݂̑��x
    public int swallowCount = 5;                // ���ݍ��݉�

    [Header("�ΏۃI�u�W�F�N�g")]
    public Transform characterTransform;        // �L�����N�^�[��Transform

    private Vector3 originalPosition;
    private bool isSwallowing = false;
    private Coroutine swallowCoroutine;

    public static CharacterSwallowAnimator Instance { get; private set; }

    private void Awake()
    {
        // ������ �����S��Instance�Ǘ� ������
        if (Instance != null && Instance != this)
        {
            // ���ɑ��̃C���X�^���X�����݂���ꍇ�A���̃R���|�[�l���g�𖳌���
            Debug.LogWarning("CharacterSwallowAnimator�̏d�������o�B���̃R���|�[�l���g�𖳌������܂�: " + gameObject.name);
            this.enabled = false;
            return;
        }
        Instance = this;

        // �L�����N�^�[��Transform���ݒ肳��Ă��Ȃ��ꍇ�A�������g���g�p
        if (characterTransform == null)
        {
            characterTransform = transform;
        }

        // ���̈ʒu���L�^
        originalPosition = characterTransform.localPosition;

        Debug.Log("CharacterSwallowAnimator Instance�ݒ芮��: " + gameObject.name);
    }

    private void OnDestroy()
    {
        // ���̃C���X�^���X���j������鎞�AInstance���N���A
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("CharacterSwallowAnimator Instance ���N���A���܂���");
        }
    }

    /// <summary>
    /// ���ݍ��݃A�j���[�V�����J�n
    /// </summary>
    public void StartSwallowAnimation()
    {
        if (!this.enabled) return;

        // ������ ���ɃA�j���[�V�������̏ꍇ�͌p�����Ď��s ������
        if (isSwallowing)
        {
            Debug.Log("���Ɉ��ݍ��݃A�j���[�V�������̂��߁A�p�����s");
            return;
        }

        Debug.Log("�L�����N�^�[���ݍ��݃A�j���[�V�����J�n");
        swallowCoroutine = StartCoroutine(SwallowAnimationCoroutine());
    }

    /// <summary>
    /// ���ݍ��݃A�j���[�V������~
    /// </summary>
    public void StopSwallowAnimation()
    {
        if (!this.enabled) return;

        // ������ �����ɂ͒�~�����A���݂̃A�j���[�V���������������� ������
        if (isSwallowing && swallowCoroutine != null)
        {
            Debug.Log("���ݍ��݃A�j���[�V�����I���v���i���݂̃A�j���[�V����������ɒ�~�j");
            StartCoroutine(DelayedStop());
        }
    }

    /// <summary>
    /// �x����~����
    /// </summary>
    private IEnumerator DelayedStop()
    {
        // ���݂̃A�j���[�V�������I���܂ŏ����҂�
        yield return new WaitForSeconds(0.2f);

        if (swallowCoroutine != null)
        {
            StopCoroutine(swallowCoroutine);
            swallowCoroutine = null;
        }

        isSwallowing = false;

        // ���̈ʒu�ɖ߂�
        if (characterTransform != null)
        {
            StartCoroutine(ReturnToOriginalPosition());
        }

        Debug.Log("�L�����N�^�[���ݍ��݃A�j���[�V�����I��");
    }

    /// <summary>
    /// ���ݍ��݃A�j���[�V�����̃��C���R���[�`��
    /// </summary>
    private IEnumerator SwallowAnimationCoroutine()
    {
        if (!this.enabled) yield break;

        isSwallowing = true;

        for (int i = 0; i < swallowCount; i++)
        {
            if (!this.enabled || !isSwallowing) yield break;

            // ���ɓ����i���ݍ��ݏ����j
            yield return StartCoroutine(MoveToPosition(originalPosition + Vector3.down * swallowIntensity, 0.15f));

            // �����҂�
            yield return new WaitForSeconds(0.1f);

            // ��ɓ����i���ݍ��݁j
            yield return StartCoroutine(MoveToPosition(originalPosition + Vector3.up * swallowIntensity * 0.5f, 0.1f));

            // ���̈ʒu�ɖ߂�
            yield return StartCoroutine(MoveToPosition(originalPosition, 0.2f));

            // ���̈��ݍ��݂܂ł̊Ԋu
            yield return new WaitForSeconds(1f / swallowSpeed);
        }

        isSwallowing = false;
        Debug.Log("���ݍ��݃A�j���[�V��������");
    }

    /// <summary>
    /// �w��ʒu�ւ̈ړ��A�j���[�V����
    /// </summary>
    private IEnumerator MoveToPosition(Vector3 targetPosition, float duration)
    {
        if (!this.enabled || characterTransform == null) yield break;

        Vector3 startPosition = characterTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration && this.enabled)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // �X���[�Y�Ȉړ��i�C�[�W���O�j
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            if (characterTransform != null)
            {
                characterTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, easedT);
            }

            yield return null;
        }

        if (characterTransform != null)
        {
            characterTransform.localPosition = targetPosition;
        }
    }

    /// <summary>
    /// ���̈ʒu�ɖ߂�A�j���[�V����
    /// </summary>
    private IEnumerator ReturnToOriginalPosition()
    {
        yield return StartCoroutine(MoveToPosition(originalPosition, 0.3f));
    }

    /// <summary>
    /// ���ݍ��ݒ����ǂ���
    /// </summary>
    public bool IsSwallowing()
    {
        return isSwallowing && this.enabled;
    }

    /// <summary>
    /// �ݒ�����A���^�C���ŕύX
    /// </summary>
    public void SetSwallowSettings(float intensity, float speed, int count)
    {
        if (!this.enabled) return;

        swallowIntensity = intensity;
        swallowSpeed = speed;
        swallowCount = count;
    }
}