using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("ContraptionDataset")]
public class ContraptionDataset
{
    public class ContraptionDatasetUnit
    {
        [XmlAttribute("x")]
        public int x;
        [XmlAttribute("y")]
        public int y;
        [XmlAttribute("partType")]
        public int partType;
        [XmlAttribute("customPartIndex")]
        public int customPartIndex;
        [XmlAttribute("rot")]
        public int rot;
        [XmlAttribute("flipped")]
        public bool flipped;
        [XmlAttribute("offsetX")]
        public float offsetX;
        [XmlAttribute("offsetY")]
        public float offsetY;
        public ContraptionDatasetUnit()
        {
        }

        public ContraptionDatasetUnit(int x, int y, int partType, int customPartIndex, int rot, bool flipped, float offsetx = 0f, float offsety = 0f)
        {
            this.x = x;
            this.y = y;
            this.partType = partType;
            this.customPartIndex = customPartIndex;
            this.rot = rot;
            this.flipped = flipped;
            this.offsetX = offsetX;
            this.offsetY = offsetY;
        }
    }

    [XmlArray("ContraptionDatasetList")]
    [XmlArrayItem("ContraptionDatasetUnit")]
    protected List<ContraptionDatasetUnit> m_contraptionDataSet = new List<ContraptionDatasetUnit>();
    public List<ContraptionDatasetUnit> ContraptionDatasetList => m_contraptionDataSet;

    public void AddPart(int x, int y, int partType, int customPartIndex, BasePart.GridRotation rotation, bool flipped, float offsetx, float offsety)
    {
        ContraptionDatasetUnit contraptionDatasetUnit = new ContraptionDatasetUnit
        {
            x = x,
            y = y,
            partType = partType,
            customPartIndex = customPartIndex,
            rot = (int)rotation,
            flipped = flipped,
            offsetX = offsetx,
            offsetY = offsety
        };
        m_contraptionDataSet.Add(contraptionDatasetUnit);
    }

    public void AddPart(int x, int y, int partType, int customPartIndex, int rotation, bool flipped, out ContraptionDatasetUnit unit, float offsetx, float offsety)
    {
        ContraptionDatasetUnit contraptionDatasetUnit = new ContraptionDatasetUnit
        {
            x = x,
            y = y,
            partType = partType,
            customPartIndex = customPartIndex,
            rot = rotation,
            flipped = flipped,
            offsetX = offsetx,
            offsetY = offsety
        };
        m_contraptionDataSet.Add(contraptionDatasetUnit);
        unit = contraptionDatasetUnit;
    }
}