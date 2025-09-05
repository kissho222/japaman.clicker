using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;

public class SaveManager : MonoBehaviour
{
    [Header("セーブ設定")]
    public int maxSaveSlots = 20;          // 最大セーブスロット数

    private static SaveManager instance;
    public static SaveManager Instance => instance;

    // セーブファイルのパス
    private string saveDirectoryPath;
    private const string SAVE_FILE_PREFIX = "kemono_save_";
    private const string SAVE_FILE_EXTENSION = ".json";

    // セーブデータのキャッシュ
    private Dictionary<int, SaveData> loadedSaveData = new Dictionary<int, SaveData>();

    void Awake()
    {
        // 🔧 修正: DontDestroyOnLoadの適切な管理
        if (instance == null)
        {
            instance = this;

            // 🔧 重要: 親を null に設定してからDontDestroyOnLoad
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
            InitializeSaveSystem();

            Debug.Log("💾 SaveManager を初期化しました");
        }
        else if (instance != this)
        {
            // 🔧 重要: 重複インスタンスの適切な削除
            Debug.LogWarning($"💾 SaveManager の重複インスタンスを削除: {gameObject.name}");
            Destroy(gameObject);
        }
    }
    void OnDestroy()
    {
        // 🔧 インスタンス参照のクリア
        if (instance == this)
        {
            instance = null;
            Debug.Log("💾 SaveManager インスタンスをクリアしました");
        }
    }

    void InitializeSaveSystem()
    {
        // セーブディレクトリの設定
        saveDirectoryPath = Path.Combine(Application.persistentDataPath, "SaveData");

        // ディレクトリが存在しない場合は作成
        if (!Directory.Exists(saveDirectoryPath))
        {
            Directory.CreateDirectory(saveDirectoryPath);
        }

        LoadAllSaveData();
    }

    // 全セーブデータを読み込み
    void LoadAllSaveData()
    {
        loadedSaveData.Clear();

        for (int slotIndex = 0; slotIndex < maxSaveSlots; slotIndex++)
        {
            SaveData data = LoadSaveData(slotIndex);
            if (data != null)
            {
                loadedSaveData[slotIndex] = data;
            }
        }
    }

    // セーブデータを保存
    public bool SaveGame(int slotIndex, SaveData saveData)
    {
        try
        {
            string filePath = GetSaveFilePath(slotIndex);
            string jsonData = JsonUtility.ToJson(saveData, true);

            File.WriteAllText(filePath, jsonData);

            // キャッシュも更新
            loadedSaveData[slotIndex] = saveData;

            Debug.Log($"ゲームをスロット{slotIndex}に保存しました: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"セーブに失敗しました: {e.Message}");
            return false;
        }
    }

    // セーブデータを読み込み
    public SaveData LoadSaveData(int slotIndex)
    {
        try
        {
            string filePath = GetSaveFilePath(slotIndex);

            if (!File.Exists(filePath))
            {
                return null;
            }

            string jsonData = File.ReadAllText(filePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);

            return saveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"ロードに失敗しました: {e.Message}");
            return null;
        }
    }

    // セーブデータを削除
    public bool DeleteSaveData(int slotIndex)
    {
        try
        {
            string filePath = GetSaveFilePath(slotIndex);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);

                // キャッシュからも削除
                if (loadedSaveData.ContainsKey(slotIndex))
                {
                    loadedSaveData.Remove(slotIndex);
                }

                Debug.Log($"スロット{slotIndex}のセーブデータを削除しました");
                return true;
            }

            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"セーブデータの削除に失敗しました: {e.Message}");
            return false;
        }
    }

    // 現在のゲーム状態からセーブデータを作成
    public SaveData CreateSaveDataFromCurrentGame()
    {
        SaveData saveData = new SaveData();

        // GameManagerから現在の状態を取得
        if (GameManager.Instance != null)
        {
            saveData.currentStage = GameManager.Instance.currentStage;
            // 前日のジャパまん数は別途設定する必要があります
        }

        // 必要に応じて他の管理クラスからデータを取得
        saveData.saveDateTime = DateTime.Now;

        return saveData;
    }

    // セーブスロットが使用済みかチェック
    public bool IsSlotUsed(int slotIndex)
    {
        return loadedSaveData.ContainsKey(slotIndex);
    }

    // セーブスロットの情報を取得
    public SaveData GetSlotData(int slotIndex)
    {
        return loadedSaveData.ContainsKey(slotIndex) ? loadedSaveData[slotIndex] : null;
    }

    // 全セーブスロットの情報を取得
    public Dictionary<int, SaveData> GetAllSaveSlots()
    {
        return new Dictionary<int, SaveData>(loadedSaveData);
    }

    // セーブファイルのパスを取得
    string GetSaveFilePath(int slotIndex)
    {
        string fileName = $"{SAVE_FILE_PREFIX}{slotIndex:D2}{SAVE_FILE_EXTENSION}";
        return Path.Combine(saveDirectoryPath, fileName);
    }

    // セーブファイルの総サイズを取得（デバッグ用）
    public long GetTotalSaveFileSize()
    {
        long totalSize = 0;

        for (int i = 0; i < maxSaveSlots; i++)
        {
            string filePath = GetSaveFilePath(i);
            if (File.Exists(filePath))
            {
                totalSize += new FileInfo(filePath).Length;
            }
        }

        return totalSize;
    }
    /// <summary>
    /// デバッグ用: 全スロット状況表示
    /// </summary>
    [ContextMenu("🔍 全セーブスロット状況")]
    public void DebugAllSaveSlots()
    {
        Debug.Log("=== 🔍 全セーブスロット状況 ===");

        for (int i = 0; i < maxSaveSlots; i++)
        {
            bool used = IsSlotUsed(i);
            Debug.Log($"スロット{i:D2}: {(used ? "使用中" : "空き")}");

            if (used)
            {
                SaveData data = LoadSaveData(i);
                if (data != null)
                {
                    Debug.Log($"  - ステージ: {data.currentStage}");
                    Debug.Log($"  - 日時: {data.GetSaveDateTimeString()}");
                }
            }
        }

        // 中断セーブスロット（999）の確認
        bool suspend999 = IsSlotUsed(999);
        Debug.Log($"中断セーブ(999): {(suspend999 ? "使用中" : "空き")}");

        if (suspend999)
        {
            SaveData suspendData = LoadSaveData(999);
            if (suspendData != null)
            {
                Debug.Log($"  - ステージ: {suspendData.currentStage}");
                Debug.Log($"  - 日時: {suspendData.GetSaveDateTimeString()}");
            }
        }

        Debug.Log("=== 確認完了 ===");
    }
}