#region Copyright
/////////////////////////////////////////////////////////////////////////////////////////////
// Copyright 2023 Garmin International, Inc.
// Licensed under the Flexible and Interoperable Data Transfer (FIT) Protocol License; you
// may not use file except in compliance with the Flexible and Interoperable Data
// Transfer (FIT) Protocol License.
/////////////////////////////////////////////////////////////////////////////////////////////
// ****WARNING****  file is auto-generated!  Do NOT edit file.
// Profile Version = 21.105Release
// Tag = production/release/21.105.00-0-gdc65d24
/////////////////////////////////////////////////////////////////////////////////////////////

#endregion

namespace Dynastream.Fit
{
  public class Field
      : FieldBase
  {
    #region Fields
    private readonly string name_;
    private byte type_;
    private readonly double scale_;
    private readonly double offset_;
    private readonly string units_;
    private readonly Dictionary<string, Subfield> subfieldsByName_ = new();
    public Dictionary<int, Subfield> Subfields { get; } = new(); // aka SubfieldsByIndex
    public Dictionary<int, FieldComponent> Components { get; } = new();
    #endregion

    #region Properties
    public override string Name => name_;
    public byte Num { get; set; }
    public override byte Type => type_;
    public override double Scale => scale_;
    public override double Offset => offset_;
    public override string Units => units_;
    public bool IsAccumulated { get; }
    public Profile.Type ProfileType { get; }
    public bool IsExpandedField { get; set; }
    #endregion

    #region Constructors
    public Field(Field other)
        : base(other)
    {
      if (other == null)
      {
        name_ = "unknown";
        Num = Fit.FieldNumInvalid;
        type_ = 0;
        scale_ = 1f;
        offset_ = 0f;
        units_ = "";
        IsAccumulated = false;
        ProfileType = Profile.Type.Enum;
        IsExpandedField = false;
        return;
      }

      name_ = other.Name;
      Num = other.Num;
      type_ = other.Type;
      scale_ = other.Scale;
      offset_ = other.Offset;
      units_ = other.units_;
      IsAccumulated = other.IsAccumulated;
      ProfileType = other.ProfileType;
      IsExpandedField = other.IsExpandedField;

      foreach (var kvp in other.Subfields)
      {
        int index = kvp.Key;
        Subfield subfield = kvp.Value;
        AddSubfield(new Subfield(subfield), index);
      }

      foreach (var kvp in other.Components)
      {
        int index = kvp.Key;
        FieldComponent component = kvp.Value;
        AddComponent(new FieldComponent(component), index);
      }
    }

    internal Field(string name, byte num, byte type, double scale, double offset, string units, bool accumulated, Profile.Type profileType)
    {
      name_ = name;
      Num = num;
      type_ = type;
      scale_ = scale;
      offset_ = offset;
      units_ = units;
      IsAccumulated = accumulated;
      ProfileType = profileType;
      IsExpandedField = false;
    }

    internal Field(byte num, byte type)
        : this("unknown", num, type, 1.0d, 0.0d, "", false, Profile.Type.NumTypes)
    {
    }
    #endregion

    #region Methods

    public void AddSubfield(Subfield subfield, int index = -1)
    {
      subfieldsByName_[subfield.Name] = subfield;

      if (index < 0) index = Subfields.Count;
      Subfields[index] = subfield;
    }

    public void AddComponent(FieldComponent component, int index = -1)
    {
      if (index < 0) index = Components.Count;
      Components[index] = component;
    }

    internal void SetType(byte value) => type_ = value;

    internal override Subfield GetSubfield(string subfieldName) =>
      subfieldsByName_.TryGetValue(subfieldName, out Subfield subfield) ? subfield : null;

    internal override Subfield GetSubfield(int subfieldIndex) =>
      // SubfieldIndexActiveSubfield and SubfieldIndexMainField
      // will be out of range
      Subfields.TryGetValue(subfieldIndex, out Subfield subfield) ? subfield : null;
    #endregion
  }
} // namespace
