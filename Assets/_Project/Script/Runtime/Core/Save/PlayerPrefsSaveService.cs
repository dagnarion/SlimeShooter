using UnityEngine;

public class PlayerPrefsSaveService : ISaveService
{
    public bool HasKey(string key) => PlayerPrefs.HasKey(key);
    public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);
    public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);
    public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
    public void Delete(string key) => PlayerPrefs.DeleteKey(key);
    public void Save() => PlayerPrefs.Save();
}
