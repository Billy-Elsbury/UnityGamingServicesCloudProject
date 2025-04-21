using System.Collections.Generic;

[System.Serializable] // Required for JsonUtility
public class BookData
{
    public int Id; // Use public fields for JsonUtility
    public string Title;
    public string ISBN;
    public List<string> BookAuthors;
}