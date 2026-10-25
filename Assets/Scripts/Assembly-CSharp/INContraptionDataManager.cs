using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public class INContraptionDataManager
{
    public class ContraptionData
    {
        /// <summary>
        /// Basically a line in csv, refers to basepart
        /// </summary>
        public struct Unit
        {
            public int Type;
            public int Index;
            public int X;
            public int Y;
            public int Rotation;
            public int Flipped;
            public float offsetX;
            public float offsetY;
            public Unit(int type, int index, int x, int y, int rotation, int flipped, float offsetx, float offsety)
            {
                Type = type;
                Index = index;
                X = x;
                Y = y;
                Rotation = rotation;
                Flipped = flipped;
                offsetX = offsetx;
                offsetY = offsety;
            }
        }

        public Unit[] items;
        public string metadata;
        public ContraptionData() : this(0)
        {
        }

        public ContraptionData(int count)
        {
            if (count == 0)
            {
                items = Array.Empty<Unit>();
            }
            else
            {
                items = new Unit[count];
            }
        }

        public static ContraptionData Create(ContraptionDataset contraptionDataset)
        {
            List<ContraptionDataset.ContraptionDatasetUnit> contraptionDatasetList = contraptionDataset.ContraptionDatasetList;
            int count = contraptionDatasetList.Count;
            ContraptionData contraptionData = new ContraptionData(count);
            for (int i = 0; i < count; i++)
            {
                ContraptionDataset.ContraptionDatasetUnit contraptionDatasetUnit = contraptionDatasetList[i];
                contraptionData.items[i] = new Unit((int)((BasePart.PartType)contraptionDatasetUnit.partType).ToSortedPartType(), contraptionDatasetUnit.customPartIndex, contraptionDatasetUnit.x, contraptionDatasetUnit.y, contraptionDatasetUnit.rot, System.Convert.ToInt32(contraptionDatasetUnit.flipped), contraptionDatasetUnit.offsetX, contraptionDatasetUnit.offsetY);
            }

            return contraptionData;
        }

        public ContraptionDataset Convert()
        {
            ContraptionDataset contraptionDataset = new ContraptionDataset();
            Unit[] array = items;
            for (int i = 0; i < array.Length; i++)
            {
                Unit unit = array[i];
                contraptionDataset.AddPart(unit.X, unit.Y, (int)((SortedPartType)unit.Type).ToPartType(), unit.Index, (BasePart.GridRotation)unit.Rotation, System.Convert.ToBoolean(unit.Flipped), unit.offsetX, unit.offsetY); //IMPORTANT PART
            }

            return contraptionDataset;
        }
    }

    private StringBuilder m_builder;
    public string DataDirectory { get; private set; }
    public static INContraptionDataManager Instance { get; private set; }
    public static ContraptionDataSettings Settings => INUserSettings.Instance.ContraptionDataSettings;

    public static void Create()
    {
        INContraptionDataManager iNContraptionDataManager = new INContraptionDataManager();
        iNContraptionDataManager.Initialize();
        Instance = iNContraptionDataManager;
    }

    public static void SetContraptionData()
    {
        if (INSettings.GetBool(INFeature.NewContraptionData))
        {
            Create();
        }
        else
        {
            Instance = null;
        }
    }

    public void Initialize()
    {
        int versionType = INSettings.VersionType;
        m_builder = new StringBuilder();
        DataDirectory = INUnity.DataPath + "/contraptions" + ((versionType == 2) ? "A" : (versionType == 1) ? "O" : (versionType == 0) ? "" : "B");
        Directory.CreateDirectory(DataDirectory);
    }

    public ContraptionDataset LoadContraptionData(string levelName)
    {
        string dataDirectory = DataDirectory;
        if (!Settings.Enabled)
        {
            return WPFPrefs.LoadOriginalContraptionDataset(dataDirectory, levelName);
        }

        string text = dataDirectory + "/" + WPFPrefs.ContraptionFileName(levelName);
        string path = dataDirectory + "/" + levelName;
        ContraptionDataset result;
        if (!File.Exists(path))
        {
            result = ((!File.Exists(text)) ? new ContraptionDataset() : WPFPrefs.LoadOriginalContraptionDataset(dataDirectory, levelName));
        }
        else
        {
            TryLoadAndConvert(path, out result);
        }

        if (File.Exists(text))
        {
            if (Settings.BackupOriginalData)
            {
                BackupFile(text, text.Replace(".contraption", ".bak"));
            }
            else
            {
                File.Delete(text);
            }
        }

        return result;
    }

    public void SaveContraptionData(string levelName, ContraptionDataset data)
    {
        string dataDirectory = DataDirectory;
        if (!Settings.Enabled)
        {
            WPFPrefs.SaveOriginalContraptionDataset(dataDirectory, levelName, data);
            return;
        }

        string text = dataDirectory + "/" + levelName;
        if (File.Exists(text) && Settings.BackupData)
        {
            BackupFile(text, text + ".bak");
        }

        Save(text, ContraptionData.Create(data));
        if (Settings.SaveAsOriginalData)
        {
            WPFPrefs.SaveOriginalContraptionDataset(dataDirectory, levelName, data);
        }
    }

    private bool TryLoadAndConvert(string path, out ContraptionDataset result)
    {
        try
        {
            result = Load(path).Convert();
            return true;
        }
        catch
        {
            result = new ContraptionDataset();
            return false;
        }
    }

    private bool TryLoad(string path, out ContraptionData result)
    {
        try
        {
            result = LoadCSVFile(path);
            return true;
        }
        catch
        {
            result = new ContraptionData();
            return false;
        }
    }

    public ContraptionData Load(string path)
    {
        switch (Settings.LoadFormat)
        {
            case ContraptionDataSettings.SerializationFormat.ALL:
            {
                if (TryLoadCSVFile(path, out ContraptionData result))
                {
                    return result;
                }

                return LoadJSONFile(path);
            }

            case ContraptionDataSettings.SerializationFormat.CSV:
                return LoadCSVFile(path);
            case ContraptionDataSettings.SerializationFormat.JSON:
                return LoadJSONFile(path);
            default:
                return new ContraptionData();
        }
    }

    private ContraptionData LoadCSVFile(string path)
    {
        using (StreamReader streamReader = new StreamReader(path))
        {
            string[] lines = streamReader.ReadToEnd().Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> datas = new List<string>
            {
            };
            List<string> metadatas = new List<string>
            {
            };
            foreach (string s in lines)
            {
                if (s[0] != '#' || s.Substring(0, 1 - 0) != "//")
                {
                    datas.Add(s);
                }

                if (s[0] == '#')
                {
                    metadatas.Add(s);
                }
            }

            int num = datas.Count();
            ContraptionData contraptionData = new ContraptionData(num);
            contraptionData.metadata +=
                from s in metadatas
                select s;
            for (int i = 0; i < num; i++)
            {
                string[] array2 = datas[i].Split(new char[2] { ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                ContraptionData.Unit unit = default(ContraptionData.Unit);
                unit.Type = int.Parse(array2[0]);
                unit.Index = int.Parse(array2[1]);
                unit.X = int.Parse(array2[2]);
                unit.Y = int.Parse(array2[3]);
                unit.Rotation = int.Parse(array2[4]);
                unit.Flipped = int.Parse(array2[5]);
                unit.offsetX = !float.TryParse(array2[6], NumberStyles.Float, CultureInfo.InvariantCulture, out unit.offsetX) ? unit.offsetX = 0f : unit.offsetY *= 1f;
                unit.offsetY = !float.TryParse(array2[7], NumberStyles.Float, CultureInfo.InvariantCulture, out unit.offsetY) ? unit.offsetY = 0f : unit.offsetY *= 1f;
                contraptionData.items[i] = unit;
            }

            return contraptionData;
        }
    }

    private bool TryLoadCSVFile(string path, out ContraptionData result)
    {
        result = null;
        using (StreamReader streamReader = new StreamReader(path))
        {
            string[] lines = streamReader.ReadToEnd().Split(new char[2] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> datas = new List<string>
            {
            };
            List<string> metadatas = new List<string>
            {
            };
            foreach (string s in lines)
            {
                if (s[0] != '#' || s.Substring(0, 1 - 0) != "//")
                {
                    datas.Add(s);
                }

                if (s[0] == '#')
                {
                    metadatas.Add(s);
                }
            }

            int num = datas.Count;
            ContraptionData contraptionData = new ContraptionData(num);
            contraptionData.metadata +=
                from s in metadatas
                select s;
            for (int i = 0; i < num; i++)
            {
                string[] args = lines[i].Split(new char[2] { ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (args.Length != 6 && args.Length != 8)
                {
                    return false;
                }

                ContraptionData.Unit unit = default(ContraptionData.Unit);
                if (int.TryParse(args[0], out unit.Type) && int.TryParse(args[1], out unit.Index) && int.TryParse(args[2], out unit.X) && int.TryParse(args[3], out unit.Y) && int.TryParse(args[4], out unit.Rotation) && int.TryParse(args[5], out unit.Flipped) && float.TryParse(args[6], NumberStyles.Float, CultureInfo.InvariantCulture, out unit.offsetX) && float.TryParse(args[7], NumberStyles.Float, CultureInfo.InvariantCulture, out unit.offsetY))
                {
                    contraptionData.items[i] = unit;
                    continue;
                }

                return false;
            }

            result = contraptionData;
            return true;
        }
    }

    private ContraptionData LoadJSONFile(string path)
    {
        using (StreamReader reader = new StreamReader(path))
        {
            return INJsonSerializer.Deserialize<ContraptionData>(reader);
        }
    }

    public void Save(string path, ContraptionData data)
    {
        switch (Settings.SaveFormat)
        {
            case ContraptionDataSettings.SerializationFormat.ALL:
            case ContraptionDataSettings.SerializationFormat.CSV:
                SaveCSVFile(path, data);
                break;
            case ContraptionDataSettings.SerializationFormat.JSON:
                SaveJSONFile(path, data);
                break;
        }
    }

    private void SaveCSVFile(string path, ContraptionData data)
    {
        using (StreamWriter streamWriter = new StreamWriter(path))
        {
            StringBuilder builder = m_builder;
            builder.Clear();
            ContraptionData.Unit[] items = data.items;
            builder.Append(data.metadata);
            builder.AppendLine();
            foreach (ContraptionData.Unit unit in items)
            {
                const string separator = ",";
                builder.Append(unit.Type.ToString());
                builder.Append(separator);
                builder.Append(unit.Index.ToString());
                builder.Append(separator);
                builder.Append(unit.X);
                builder.Append(separator);
                builder.Append(unit.Y);
                builder.Append(separator);
                builder.Append(unit.Rotation.ToString());
                builder.Append(separator);
                builder.Append(unit.Flipped.ToString());
                builder.Append(separator);
                builder.Append(unit.offsetX.ToString(CultureInfo.InvariantCulture));
                builder.Append(separator);
                builder.Append(unit.offsetY.ToString(CultureInfo.InvariantCulture));
                builder.AppendLine();
            }

            streamWriter.Write(builder.ToString());
        }
    }

    private void SaveJSONFile(string path, ContraptionData data)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            INJsonSerializer.Serialize(data, writer);
        }
    }

    private static void BackupFile(string srcPath, string destPath)
    {
        if (!File.Exists(srcPath) || string.Equals(srcPath, destPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }

            File.Move(srcPath, destPath);
        }
        catch
        {
        // ignored
        }
    }
}