public interface ISaveService
{
    bool HasKey(string key);
    int GetInt(string key, int defaultValue = 0);
    void SetInt(string key, int value);
    string GetString(string key, string defaultValue = "");
    void SetString(string key, string value);
    void Delete(string key);
    void Save();
}
