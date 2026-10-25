using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;

public class WPFPrefs : UnityEngine.Object
{
    private static CryptoUtility m_crypto = new CryptoUtility("3b91A049Ca7HvSjhxT35");

    public static void WriteGhostPlayerData(string filename, GhostPlayer gp)
    {
        try
        {
            byte[] bytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                WriteGhostPlayerXml(memoryStream, gp);
                bytes = memoryStream.ToArray();
            }

            File.WriteAllBytes(Application.persistentDataPath + "/" + filename, bytes);
        }
        catch (Exception)
        {
        }
    }

    public static GhostPlayer ReadGhostPlayerData(string filename)
    {
        GhostPlayer result = new GhostPlayer();
        try
        {
            byte[] bytes = File.ReadAllBytes(Application.persistentDataPath + "/" + filename);
            XmlDocument document = LoadXml(bytes);
            XmlElement root = document.DocumentElement;
            XmlNode coords = ((root != null) ? root.SelectSingleNode("Coords") : null);
            if (coords != null)
            {
                foreach (XmlNode node in coords.ChildNodes)
                {
                    XmlElement element = node as XmlElement;
                    if (element != null && element.Name == "Coord")
                    {
                        result.AddPosition(GetFloat(element, "x"), GetFloat(element, "y"), GetFloat(element, "z"));
                    }
                }
            }
        }
        catch (Exception)
        {
        }

        return result;
    }

    public static string ContraptionFileName(string levelName)
    {
        return BitConverter.ToString(CryptoUtility.ComputeHash(Encoding.UTF8.GetBytes(levelName))).Substring(0, 30).Replace("-", string.Empty) + ".contraption";
    }

    public static void SaveContraptionDataset(string levelName, ContraptionDataset cds)
    {
        if (!INSettings.GetBool(INFeature.NewContraptionData))
        {
            SaveOriginalContraptionDataset(INUnity.DataPath + "/contraptions", levelName, cds);
        }
        else
        {
            INContraptionDataManager.Instance.SaveContraptionData(levelName, cds);
        }
    }

    public static void SaveOriginalContraptionDataset(string directory, string levelName, ContraptionDataset cds)
    {
        try
        {
            byte[] clearTextBytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                WriteContraptionDatasetXml(memoryStream, cds);
                clearTextBytes = memoryStream.ToArray();
            }

            byte[] array = m_crypto.Encrypt(clearTextBytes);
            string text = ContraptionFileName(levelName);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(directory + "/" + text, array);
        }
        catch (Exception)
        {
        }
    }

    public static void SaveLevelBluePrint(string levelName, ContraptionDataset cds, bool isSuperBlueprint = false, int superBluePrintIndex = 0)
    {
        try
        {
            byte[] bytes;
            using (MemoryStream memoryStream = new MemoryStream())
            {
                WriteContraptionDatasetXml(memoryStream, cds);
                bytes = memoryStream.ToArray();
            }

            string path = ((!isSuperBlueprint) ? "Data/Contraptions" : "Data/SuperContraptions");
            string path2 = levelName + "_contraption" + ((!isSuperBlueprint) ? string.Empty : $"_{superBluePrintIndex + 1:00}") + ".xml";
            string directory = Path.Combine(Application.dataPath, path);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, path2), bytes);
        }
        catch (Exception)
        {
        }
    }

    public static ContraptionDataset LoadContraptionDataset(string levelName)
    {
        if (!INSettings.GetBool(INFeature.NewContraptionData))
        {
            return LoadOriginalContraptionDataset(INUnity.DataPath + "/contraptions", levelName);
        }

        return INContraptionDataManager.Instance.LoadContraptionData(levelName);
    }

    public static ContraptionDataset LoadOriginalContraptionDataset(string directory, string levelName)
    {
        ContraptionDataset result = new ContraptionDataset();
        try
        {
            string text = ContraptionFileName(levelName);
            byte[] array = File.ReadAllBytes(directory + "/" + text);
            result = ParseContraptionDataset(m_crypto.Decrypt(array, 0));
        }
        catch (Exception)
        {
        }

        return result;
    }

    public static ContraptionDataset LoadContraptionDataset(TextAsset textAsset)
    {
        ContraptionDataset result = new ContraptionDataset();
        try
        {
            if (textAsset != null)
            {
                result = ParseContraptionDataset(textAsset.bytes);
            }
        }
        catch (Exception)
        {
        }

        return result;
    }

    private static XmlDocument LoadXml(byte[] bytes)
    {
        XmlDocument document = new XmlDocument();
        using (MemoryStream stream = new MemoryStream(bytes, writable: false))
        {
            document.Load(stream);
        }

        return document;
    }

    private static ContraptionDataset ParseContraptionDataset(byte[] bytes)
    {
        ContraptionDataset cds = new ContraptionDataset();
        XmlDocument document = LoadXml(bytes);
        XmlElement root = document.DocumentElement;
        XmlNode list = ((root != null) ? root.SelectSingleNode("ContraptionDatasetList") : null);
        if (list == null)
        {
            return cds;
        }

        foreach (XmlNode node in list.ChildNodes)
        {
            XmlElement element = node as XmlElement;
            if (element == null || element.Name != "ContraptionDatasetUnit")
            {
                continue;
            }

            cds.AddPart(GetInt(element, "x"), GetInt(element, "y"), GetInt(element, "partType"), GetInt(element, "customPartIndex"), (BasePart.GridRotation)GetInt(element, "rot"), GetBool(element, "flipped"), GetFloat(element, "offsetX"), GetFloat(element, "offsetY"));
        }

        return cds;
    }

    private static void WriteContraptionDatasetXml(Stream stream, ContraptionDataset cds)
    {
        XmlWriterSettings settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true
        };

        using (XmlWriter writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("ContraptionDataset");
            writer.WriteStartElement("ContraptionDatasetList");
            if (cds != null && cds.ContraptionDatasetList != null)
            {
                foreach (ContraptionDataset.ContraptionDatasetUnit unit in cds.ContraptionDatasetList)
                {
                    writer.WriteStartElement("ContraptionDatasetUnit");
                    writer.WriteAttributeString("x", unit.x.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("y", unit.y.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("partType", unit.partType.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("customPartIndex", unit.customPartIndex.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("rot", unit.rot.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("flipped", unit.flipped ? "true" : "false");
                    writer.WriteAttributeString("offsetX", unit.offsetX.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("offsetY", unit.offsetY.ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
        }
    }

    private static void WriteGhostPlayerXml(Stream stream, GhostPlayer ghostPlayer)
    {
        XmlWriterSettings settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true
        };

        using (XmlWriter writer = XmlWriter.Create(stream, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("GhostPlayer");
            writer.WriteStartElement("Coords");
            if (ghostPlayer != null && ghostPlayer.PositionData != null)
            {
                foreach (GhostPlayer.Coord coord in ghostPlayer.PositionData)
                {
                    writer.WriteStartElement("Coord");
                    writer.WriteAttributeString("x", coord.x.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("y", coord.y.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("z", coord.z.ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
        }
    }

    private static int GetInt(XmlElement element, string name)
    {
        int result;
        return int.TryParse(element.GetAttribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : 0;
    }

    private static float GetFloat(XmlElement element, string name)
    {
        float result;
        return float.TryParse(element.GetAttribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? result : 0f;
    }

    private static bool GetBool(XmlElement element, string name)
    {
        bool result;
        return bool.TryParse(element.GetAttribute(name), out result) && result;
    }
}
