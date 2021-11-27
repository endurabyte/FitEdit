using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dauer.Model.Extensions
{
  public static class TypeExtensions
  {
    public static List<Type> DerivativesOf<T>() => typeof(T).Derivatives();
    public static List<Type> Derivatives(this Type t) => DerivativesOf(t, Assembly.GetAssembly(t));
    public static List<Type> DerivativesOf(Type t, Assembly assembly) => assembly
      .GetTypes()
      .Where(type => type != t && t.IsAssignableFrom(type)).ToList();
  }
}