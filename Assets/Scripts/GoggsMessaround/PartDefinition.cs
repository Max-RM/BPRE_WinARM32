using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class PartDefinition
{
    private class Checker<T> : IEquatable<Checker<T>>
    {
        public readonly T[] Entries;
        public Checker(T[] entries)
        {
            Entries = entries;
        }

        //assert: target!=null
        public static bool operator ==(T target, Checker<T> checker)
        {
            return checker != null && checker.Entries.Any(entry => entry.Equals(target));
        }

        public static bool operator !=(T target, Checker<T> checker)
        {
            return !(target == checker);
        }

        public bool Equals(Checker<T> other)
        {
            if (ReferenceEquals(null, other))
                return false;
            return ReferenceEquals(this, other) || Equals(Entries, other.Entries);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj.GetType() == this.GetType() && Equals((Checker<T>)obj);
        }

        public override int GetHashCode()
        {
            return (Entries != null ? Entries.GetHashCode() : 0);
        }
    }

    private class LambdaChecker<T> : Checker<T>
    {
        private bool Equals(LambdaChecker<T> other)
        {
            return base.Equals(other) && Equals(truthMachine, other.truthMachine);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return obj.GetType() == this.GetType() && Equals((LambdaChecker<T>)obj);
        }

        public readonly Func<T, bool> truthMachine;
        public LambdaChecker(Func<T, bool> TruthMachine, T[] entries) : base(entries)
        {
            truthMachine = TruthMachine;
        }

        public static bool operator ==(T target, LambdaChecker<T> checker)
        {
            return checker != null && (checker.Entries.Any(entry => entry.Equals(target)) || checker.truthMachine(target));
        }

        public static bool operator !=(T target, LambdaChecker<T> checker)
        {
            return !(target == checker);
        }

        public override int GetHashCode()
        {
            return (Entries != null ? HashCode.Combine(Entries.GetHashCode(), truthMachine != null ? truthMachine.GetHashCode() : 0) : 0);
        }
    }

    private string datas;
    /*
     * INTER-TOPOLOGY-DIMENSION BAD PIGGIES PART FORMAT
     * One line represents one VALUE for one FIELD.
     *   The format is as simple as it could get:
     *     FIELD [OPERATOR] VALUE
     *   The OPERATOR is optional.
     * One part contains 6 VALUEs for 6 different FIELDs along with optional OPERATOR. FIELDs include:
     *
     *   - Part Type ("type")
     *   - Part Skin ("skin")
     *   - Part Type ("x")
     *   - Part Type ("y")
     *   - Part Type ("rot[ation]")
     *   - Part Type ("[is]flip[ped]")
     *
     * Make sure to convert all the characters to lowercase, remove all the underscores, remove all the word "part" and "pos" from the input before parsing
     * One line can optionally contain any of these OPERATORs between field and value, OPERATORs include:
     * "is", "equal[s]", "set", "put", "assign", "=", "==", "===", "=>", "<=", "<<", ">>", "<", ">", "->", "<-", "{", "}"
     * The VALUE depends on the FIELD. But all the fields will accept an integer value. Special VALUEs based on FIELDs are:
     *   - "type": <!-- Part Type Names -->
     *   - "rot[ation]": Decimal Digit suffix with d for degrees or r for radians
     *   - "[is]flip[ped]": true, false, yes, no
     * EXAMPLE
     * <x|partx|xcoord|x|part_x|x_coord> <=|=>|is|equal|equals|==|===|>>|<<|等于> <value>\n
     * <y|party|ycoord|y|part_y|y_coord> <=|=>|is|equal|equals|==|===|>>|<<|等于> <value>
     */
    public PartDefinition(string literal)
    {
        InitializeData(literal);
    }

    public PartDefinition()
    {
    }

    public string rawData
    {
        get => datas;
        private set
        {
            datas = value.Normalize().ToLower();
            ParseData(datas);
        }
    }

    public void InitializeData(string d)
    {
        rawData = d;
    }

    private List<string[]> partDataMatrix = new List<string[]>()
    {
    };
    private static readonly Checker<string> operatorChecker = new Checker<string>(new[] { "is", "equal", "equals", "set", "put", "assign", "=", "==", "===", "=>", "<=", "<<", ">>", "<", ">", "->", "<-", "{", "}", "等于", "设为", "设置为" });
    private static readonly Checker<string> x = new Checker<string>(new[] { "x", "xcoord", "xpos", "partx", "x_coordinate", "x_position", "横坐标", "x_coord", "x_pos", "part_x", "xcoordinate", "xposition", "xp", "x_p", "x坐标", "x位置" });
    private static readonly Checker<string> y = new Checker<string>(new[] { "y", "ycoord", "ypos", "party", "y_coordinate", "y_position", "纵坐标", "y_coord", "y_pos", "part_y", "ycoordinate", "yposition", "yp", "y_p", "y坐标", "y位置" });
    private static readonly Checker<string> color = new Checker<string>(new[] { "color", "skin", "skintype", "colortype", "variations", "variationtype", "color", "skin", "skin_type", "color_type", "variations", "variation_type", "皮肤", "皮肤值", "颜色", "颜色值" });
    private static readonly Checker<string> partType = new Checker<string>(new[] { "parttype", "type", "part_type", "typeindicator", "type_indicator", "typevalue", "type_value", "类型", "部件类型", "部件名", "部件" });
    private static readonly LambdaChecker<string> rotation = new LambdaChecker<string>(s => s.Contains("rot"), Array.Empty<string>());
    private static readonly LambdaChecker<string> flip = new LambdaChecker<string>(s => s.Contains("flip"), Array.Empty<string>());
    private Checker<string>[] paramCheckers = new[]
    {
        x,
        y,
        partType,
        color,
        rotation,
        flip
    };
    private void CheckAndNormalizeParam(ref string s)
    {
        foreach (Checker<string> checker in paramCheckers)
        {
            if (s == checker)
            {
                s = nameof(checker);
            }
        }
    }

    private void CheckAndNormalizeOperator(ref string s)
    {
        if (s == operatorChecker)
        {
            s = "=";
        }
    }

    private BasePart.PartType StringToPartType(string s)
    {
        bool success = Enum.TryParse<SortedPartType>(s, out SortedPartType sortedPartType);
        return success ? sortedPartType.ToPartType() : BasePart.PartType.Unknown;
    }

    private static bool isPartType(string s)
    {
        bool success = Enum.TryParse<SortedPartType>(s, out SortedPartType _);
        return success;
    }

    private void ParseData(string data)
    {
        string[] lines = data.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        foreach (string s in lines)
        {
            lines[index] = s.Trim(new char[] { '\r', ' ' });
            index++;
        }

        index = 0;
        foreach (string line in lines)
        {
            string[] components = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (components.Length != 3)
                continue;
            partDataMatrix.Add(components);
            index++;
        }

        NormalizeWords();
    }

    private void NormalizeWords()
    {
        foreach (string[] setters in partDataMatrix)
        {
            CheckAndNormalizeParam(ref setters[0]);
            CheckAndNormalizeOperator(ref setters[1]);
        }
    }

    public BasePart GetBasePart()
    {
        float x = 0f, y = 0f;
        int color = 0;
        BasePart.PartType partType = BasePart.PartType.Unknown;
        bool flip = false;
        int rotation = 0;
        foreach (string[] line in partDataMatrix)
        {
            switch (line[0])
            {
                case nameof(PartDefinition.x):
                    x = float.Parse(line[3]);
                    break;
                case nameof(PartDefinition.y):
                    y = float.Parse(line[3]);
                    break;
                case nameof(PartDefinition.color):
                    color = int.Parse(line[3]);
                    break;
                case nameof(PartDefinition.partType):
                    partType = StringToPartType(line[3]);
                    break;
                case nameof(PartDefinition.flip):
                    flip = bool.Parse(line[3]);
                    break;
                case nameof(PartDefinition.rotation):
                    rotation = int.Parse(line[3]);
                    break;
            }
        }

        BasePart customPart = WPFMonoBehaviour.gameData.GetCustomPart(partType, color);
        float realX = math.floor(x);
        float offsetX = x - realX;
        float realY = math.floor(y);
        float offsetY = y - realY;
        customPart.m_coordX = (int)realX;
        customPart.m_coordY = (int)realY;
        customPart.offsetX = offsetX;
        customPart.offsetY = offsetY;
        customPart.m_flipped = flip;
        customPart.m_gridRotation = (BasePart.GridRotation)rotation;
        return customPart;
    }

    public BasePart GetBasePart(float x = 0f, float y = 0f, int color = 0, BasePart.PartType partType = BasePart.PartType.Unknown, bool flip = false, int rotation = 0)
    {
        BasePart customPart = WPFMonoBehaviour.gameData.GetCustomPart(partType, color);
        float realX = math.floor(x);
        float offsetX = x - realX;
        float realY = math.floor(y);
        float offsetY = y - realY;
        customPart.m_coordX = (int)realX;
        customPart.m_coordY = (int)realY;
        customPart.offsetX = offsetX;
        customPart.offsetY = offsetY;
        customPart.m_flipped = flip;
        customPart.m_gridRotation = (BasePart.GridRotation)rotation;
        return customPart;
    }
}