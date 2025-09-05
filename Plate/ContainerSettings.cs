using UnityEngine;

[System.Serializable]
public enum ContainerType
{
    SmallPlate,           // �X�e�[�W1-2: �����Ȃ��M
    LargePlate,           // �X�e�[�W3-4: �傫�Ȃ��M
    Basin,                // �X�e�[�W5-6: ���炢
    EmptyPool,            // �X�e�[�W7-8: ��̃v�[��
    GiantStoneVessel,     // �X�e�[�W9-10: ����Ȑ΂̊�
    VolcanoCrater,        // �X�e�[�W11-12: �ΎR�̉Ό�
    SpaceFloatingVessel   // �X�e�[�W13-15: �F���ɕ����Ԋ�i�̌��ōŏI�j
}

[System.Serializable]
public class ContainerData
{
    [Header("��̊�{���")]
    public ContainerType containerType;
    public string containerName;
    public GameObject containerPrefab;

    [Header("�W���p�܂�z�u�ݒ�")]
    [Range(0.1f, 2.0f)]
    public float sizeMultiplier = 0.8f;      // ��̃T�C�Y�ɑ΂���g�p�͈�

    [Range(0.0f, 0.5f)]
    public float centerBias = 0.1f;          // �������x����

    [Range(0.3f, 1.0f)]
    public float maxRadius = 0.8f;           // �ő�z�u���a

    [Header("�����ݒ�")]
    public float minDropHeight = 300f;       // �ŏ��������x�i�����ݒ�j
    public float maxDropHeight = 500f;       // �ő嗎�����x�i�����ݒ�j

    [Header("�z�u�p�^�[��")]
    public bool useCircularPattern = true;   // �~�`�z�u
    public bool useStackingPattern = false;  // �ςݏグ�z�u
    public float stackingHeight = 20f;       // �ςݏグ���̍����Ԋu

    [Header("����ݒ�")]
    public Vector2 centerOffset = Vector2.zero;  // ���S�ʒu�̃I�t�Z�b�g
    public bool useGravityFall = true;           // �d�͗������g�p
}

public class ContainerSettings : MonoBehaviour
{
    [Header("��f�[�^")]
    public ContainerData[] containerDatabase;

    [Header("���݂̊�")]
    public ContainerType currentContainer = ContainerType.SmallPlate;

    public static ContainerSettings Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        InitializeContainerDatabase();
    }

    private void InitializeContainerDatabase()
    {
        containerDatabase = new ContainerData[] {
            // �X�e�[�W1-2: �����Ȃ��M
            new ContainerData {
                containerType = ContainerType.SmallPlate,
                containerName = "�����Ȃ��M",
                sizeMultiplier = 0.8f,
                centerBias = 0.1f,
                maxRadius = 0.7f,
                minDropHeight = 300f,  // ������ 150f �� 300f �ɕύX ������
                maxDropHeight = 450f,  // ������ 250f �� 450f �ɕύX ������
                useCircularPattern = true,
                useStackingPattern = false
            },
            
            // �X�e�[�W3-4: �傫�Ȃ��M
            new ContainerData {
                containerType = ContainerType.LargePlate,
                containerName = "�傫�Ȃ��M",
                sizeMultiplier = 0.9f,
                centerBias = 0.15f,
                maxRadius = 0.8f,
                minDropHeight = 350f,  // ������ 180f �� 350f �ɕύX ������
                maxDropHeight = 500f,  // ������ 280f �� 500f �ɕύX ������
                useCircularPattern = true,
                useStackingPattern = false
            },
            
            // �X�e�[�W5-6: ���炢
            new ContainerData {
                containerType = ContainerType.Basin,
                containerName = "���炢",
                sizeMultiplier = 0.7f,
                centerBias = 0.05f,
                maxRadius = 0.6f,
                minDropHeight = 400f,  // ������ 200f �� 400f �ɕύX ������
                maxDropHeight = 550f,  // ������ 320f �� 550f �ɕύX ������
                useCircularPattern = true,
                useStackingPattern = true,
                stackingHeight = 15f
            },
            
            // �X�e�[�W7-8: ��̃v�[��
            new ContainerData {
                containerType = ContainerType.EmptyPool,
                containerName = "��̃v�[��",
                sizeMultiplier = 0.9f,
                centerBias = 0.2f,
                maxRadius = 0.8f,
                minDropHeight = 450f,  // ������ 250f �� 450f �ɕύX ������
                maxDropHeight = 600f,  // ������ 350f �� 600f �ɕύX ������
                useCircularPattern = true,
                useStackingPattern = false,
                centerOffset = new Vector2(0, -5f)
            },
            
            // �X�e�[�W9-10: ����Ȑ΂̊�
            new ContainerData {
                containerType = ContainerType.GiantStoneVessel,
                containerName = "����Ȑ΂̊�",
                sizeMultiplier = 1.0f,
                centerBias = 0.25f,
                maxRadius = 0.9f,
                minDropHeight = 500f,  // ������ 280f �� 500f �ɕύX ������
                maxDropHeight = 650f,  // ������ 400f �� 650f �ɕύX ������
                useCircularPattern = true,
                useStackingPattern = false,
                centerOffset = new Vector2(0, -10f)
            },
            
            // �X�e�[�W11-12: �ΎR�̉Ό�
            new ContainerData {
                containerType = ContainerType.VolcanoCrater,
                containerName = "�ΎR�̉Ό�",
                sizeMultiplier = 0.8f,
                centerBias = 0.15f,
                maxRadius = 0.7f,
                minDropHeight = 550f,  // ������ 300f �� 550f �ɕύX ������
                maxDropHeight = 700f,  // ������ 450f �� 700f �ɕύX ������
                useCircularPattern = true,
                useStackingPattern = true,
                stackingHeight = 25f,
                centerOffset = new Vector2(0, -15f)
            },
            
            // �X�e�[�W13-15: �F���ɕ����Ԋ�
            new ContainerData {
                containerType = ContainerType.SpaceFloatingVessel,
                containerName = "�F���ɕ����Ԋ�",
                sizeMultiplier = 1.0f,
                centerBias = 0.1f,
                maxRadius = 0.8f,
                minDropHeight = 600f,  // ������ 350f �� 600f �ɕύX ������
                maxDropHeight = 800f,  // ������ 500f �� 800f �ɕύX ������
                useCircularPattern = true,
                useStackingPattern = false,
                useGravityFall = false,
                centerOffset = Vector2.zero
            }
        };

        Debug.Log("��f�[�^�x�[�X�����������i�̌��ŁE�������ʒu�j: " + containerDatabase.Length + "���");
    }

    // ���݂̊�̃f�[�^���擾
    public ContainerData GetCurrentContainerData()
    {
        foreach (var container in containerDatabase)
        {
            if (container.containerType == currentContainer)
            {
                return container;
            }
        }
        return containerDatabase[0];  // �f�t�H���g
    }

    // ���ύX
    public void ChangeContainer(ContainerType newContainer)
    {
        currentContainer = newContainer;
        var data = GetCurrentContainerData();
        Debug.Log("���ύX: " + data.containerName);

        // ClickManager�ɒʒm
        var clickManager = FindFirstObjectByType<ClickManager>();
        if (clickManager != null)
        {
            clickManager.OnContainerChanged();
        }
    }

    // �X�e�[�W�ɉ����Ċ�������ύX�i�̌���1-15�X�e�[�W�j
    public void UpdateContainerForStage(int stage)
    {
        ContainerType newContainer = ContainerType.SmallPlate;

        if (stage <= 2) newContainer = ContainerType.SmallPlate;
        else if (stage <= 4) newContainer = ContainerType.LargePlate;
        else if (stage <= 6) newContainer = ContainerType.Basin;
        else if (stage <= 8) newContainer = ContainerType.EmptyPool;
        else if (stage <= 10) newContainer = ContainerType.GiantStoneVessel;
        else if (stage <= 12) newContainer = ContainerType.VolcanoCrater;
        else newContainer = ContainerType.SpaceFloatingVessel;  // �X�e�[�W13-15

        if (newContainer != currentContainer)
        {
            ChangeContainer(newContainer);
        }
    }

    // �̌��ł̍ŏI�X�e�[�W���`�F�b�N
    public bool IsLastStageOfDemo(int stage)
    {
        return stage >= 15;
    }

    // �̌��ŏI�����b�Z�[�W
    public void ShowDemoEndMessage()
    {
        Debug.Log("�̌��ŏI���I�����͊��S�ł�...");
        // �����ōw���U��UI��\��
    }
}