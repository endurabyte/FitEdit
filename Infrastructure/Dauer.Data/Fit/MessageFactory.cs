using Dauer.Model.Extensions;
using Dynastream.Fit;

namespace Dauer.Data.Fit;

public class MessageFactory
{
  public static Dictionary<ushort, Type> Types = TypeExtensions
    .DerivativesOf<Mesg>()
    .ToDictionary(t => ((Mesg)Activator.CreateInstance(t)).Num, t => t);

  public static Dictionary<Type, ushort> MesgNums = Types.Reverse();

  /// <summary>
  /// Convert general Mesg to specific e.g. LapMesg
  /// </summary>
  public static Mesg Create(Mesg mesg) => Types.ContainsKey(mesg.Num) 
    ? (Mesg)Activator.CreateInstance(Types[mesg.Num], mesg) 
    : mesg;
}