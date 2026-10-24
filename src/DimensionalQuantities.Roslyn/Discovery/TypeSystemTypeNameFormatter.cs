namespace DimensionalQuantities.Roslyn.Discovery;

internal static class TypeSystemTypeNameFormatter
{
	public static string Format(Type type)
	{
		string name = $"global::{type.Namespace}.{type.Name.Split('`')[0]}";
		return type.IsConstructedGenericType
			? $"{name}<{string.Join(", ", type.GetGenericArguments().Select(Format))}>" : name;
	}
}
