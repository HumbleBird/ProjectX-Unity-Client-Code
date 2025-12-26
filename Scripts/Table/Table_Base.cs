using UnityEngine;

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Table_Base
{
    string GetTablePath()
    {
        if (Application.isEditor)
            return System.Environment.CurrentDirectory.Replace("\\", "/") + "/Assets";
        else if (Application.isMobilePlatform || Application.isConsolePlatform)
            return Application.persistentDataPath + "/Assets";
        else
            return Application.streamingAssetsPath;
    }

    protected void Load_Binary<T>(string _strName, ref T _obj)
    {
        var b = new BinaryFormatter();

        b.AssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple;

        TextAsset asset = Resources.Load("Table_" + _strName) as TextAsset;

        Stream stream = new MemoryStream(asset.bytes);

        _obj = (T)b.Deserialize(stream);

        stream.Close();
    }

    protected void Save_Binary(string _strName, object _obj)
    {
        string strpath = GetTablePath() + "/Table/Resources/" + "Table_" + _strName + ".txt";

        var b = new BinaryFormatter();
        Stream stream = File.Open(strpath, FileMode.OpenOrCreate, FileAccess.Write);
        b.Serialize(stream, _obj);
        stream.Close();

    }

    protected CSVReader GetCSVReader(string _strName)
    {
        string strExt = ".csv";

        FileStream file = new FileStream("./Document/" + _strName + strExt,
            FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        StreamReader stream = new StreamReader(file, System.Text.Encoding.UTF8);
        CSVReader reader = new CSVReader();
        reader.parse(stream.ReadToEnd(), false, 1);
        stream.Close();

        return reader;
    }

}
