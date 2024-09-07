using FitEdit.Model.Extensions;
using Dynastream.Fit;

namespace FitEdit.Data.Fit;

public class MessageFactory
{
  /// <summary>
  /// Map global mesg num to Mesg
  /// </summary>
  public static Dictionary<ushort, Type> Types = TypeExtensions
    .DerivativesOf<Mesg>()
    .ToDictionary(t => ((Mesg)Activator.CreateInstance(t)).Num, t => t);

  public static Dictionary<Type, ushort> MesgNums = Types.Reverse();

  public static T Create<T>() where T : Mesg => (T)Create(Profile.GetMesg(MesgNums[typeof(T)]));
  public static T Create<T>(Stream stream, MesgDefinition def) where T : Mesg => (T)Create(new Mesg(stream, def));

  /// <summary>
  /// Convert general Mesg to specific e.g. LapMesg
  /// </summary>
  public static Mesg Create(Mesg mesg) => Types.ContainsKey(mesg.Num) 
    ? (Mesg)Activator.CreateInstance(Types[mesg.Num], mesg) 
    : mesg;
}