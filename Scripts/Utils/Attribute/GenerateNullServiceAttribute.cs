#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// 인터페이스에 붙이면 Null 구현 자동 생성 대상이 됨
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class GenerateNullServiceAttribute : Attribute
{
}

/// <summary>
/// GenerateNullServiceAttribute가 붙은 인터페이스를 스캔해서
/// Null 구현 클래스를 자동 생성하는 에디터 툴
/// </summary>
public static class NullServiceAutoGenerator
{
    private const string OUTPUT_FOLDER = "Assets/Scripts/NullServices";
    private const string OUTPUT_PATH =
    "Assets/Scripts/NullServices/NullServices.generated.cs";

    [MenuItem("Tools/Null Services/Generate From Attributes")]
    public static void GenerateAll()
    {
        var interfaces = GetTargetInterfaces();
        if (interfaces.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Null Service Generator",
                "No interfaces with [GenerateNullService] found.",
                "OK");
            return;
        }

        var sb = new StringBuilder();

        // using
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using static Define;");
        sb.AppendLine("using Data;");
        sb.AppendLine("using System.Collections;");
        sb.AppendLine();

        // namespace (선택)
        sb.AppendLine("public static class NullServices");
        sb.AppendLine("{");

        foreach (var iface in interfaces)
        {
            sb.AppendLine(GenerateNestedNullClass(iface));
        }

        sb.AppendLine("}");

        System.IO.File.WriteAllText(OUTPUT_PATH, sb.ToString());
        AssetDatabase.Refresh();
    }


    // ----------------------------------------------------------------------

    private static List<Type> GetTargetInterfaces()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .Where(t =>
                t.IsInterface &&
                t.GetCustomAttribute<GenerateNullServiceAttribute>() != null)
            .ToList();
    }


    // ----------------------------------------------------------------------

    private static string GenerateNestedNullClass(Type iface)
    {
        var className = $"Null{iface.Name.Substring(1)}";
        var sb = new StringBuilder();

        sb.AppendLine($"    public sealed class {className} : {iface.Name}");
        sb.AppendLine("    {");
        sb.AppendLine($"        public static readonly {className} Instance = new();");
        sb.AppendLine($"        private {className}() {{ }}");
        sb.AppendLine();

        // events
        foreach (var evt in iface.GetEvents())
        {
            sb.AppendLine(
                $"        public event {evt.EventHandlerType.GetFriendlyName()} {evt.Name}" +
                " { add { } remove { } }");
        }

        if (iface.GetEvents().Length > 0)
            sb.AppendLine();

        // properties
        foreach (var prop in iface.GetProperties())
        {
            bool hasGetter = prop.GetGetMethod() != null;
            bool hasSetter = prop.GetSetMethod() != null;

            var tName = prop.PropertyType.GetFriendlyName();

            if (hasGetter && hasSetter)
            {
                // get; set; => auto-property
                sb.AppendLine($"        public {tName} {prop.Name} {{ get; set; }}");
            }
            else if (hasGetter)
            {
                // get only
                sb.AppendLine($"        public {tName} {prop.Name} => {GetDefaultValue(prop.PropertyType)};");
            }
            else
            {
                // set only (거의 없지만)
                sb.AppendLine($"        public {tName} {prop.Name} {{ set {{ }} }}");
            }
        }

        // methods
        foreach (var method in iface.GetMethods().Where(m => !m.IsSpecialName))
        {
            sb.AppendLine();

            var parameters = method.GetParameters();

            string paramList = string.Join(", ",
                parameters.Select(p =>
                {
                    string paramsPrefix = Attribute.IsDefined(p, typeof(ParamArrayAttribute)) ? "params " : "";
                    string modifier =
                        p.IsOut ? "out " :
                        (p.ParameterType.IsByRef ? "ref " :
                         (p.IsIn ? "in " : ""));

                    var paramType = p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType;
                    return $"{paramsPrefix}{modifier}{paramType.GetFriendlyName()} {p.Name}";
                }));

            sb.Append($"        public {method.ReturnType.GetFriendlyName()} {method.Name}({paramList})");

            bool hasOut = parameters.Any(p => p.IsOut);

            // ✅ out이 있으면 무조건 블록 바디로 생성
            if (method.ReturnType == typeof(void))
            {
                sb.AppendLine();
                sb.AppendLine("        {");
                EmitOutDefaultAssignments(sb, parameters);
                sb.AppendLine("        }");
            }
            else if (hasOut)
            {
                sb.AppendLine();
                sb.AppendLine("        {");
                EmitOutDefaultAssignments(sb, parameters);

                // return default/null
                sb.AppendLine($"            return {GetDefaultValue(method.ReturnType)};");
                sb.AppendLine("        }");
            }
            else
            {
                // out이 없으면 기존대로 간단히
                sb.AppendLine($" => {GetDefaultValue(method.ReturnType)};");
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        return sb.ToString();
    }

    private static string GetDefaultValue(Type type)
    {
        if (type == typeof(void)) return string.Empty;

        // Unity 자주 쓰는 타입
        if (type == typeof(Vector3)) return "Vector3.zero";
        if (type == typeof(Quaternion)) return "Quaternion.identity";

        // string은 null보다 빈 문자열이 안전한 경우가 많음(원치 않으면 null로 바꿔도 됨)
        if (type == typeof(string)) return "\"\"";

        // 배열은 빈 배열
        if (type.IsArray)
        {
            var elem = type.GetElementType();
            return $"System.Array.Empty<{elem.GetFriendlyName()}>()";
        }

        // 제네릭 컬렉션 안전 반환
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();
            var tArg = args.Length > 0 ? args[0] : null;

            // List<T> => new List<T>()
            if (def == typeof(List<>))
                return $"new System.Collections.Generic.List<{tArg.GetFriendlyName()}>()";

            // IReadOnlyList<T> => Array.Empty<T>()
            if (def == typeof(IReadOnlyList<>))
                return $"System.Array.Empty<{tArg.GetFriendlyName()}>()";

            // IEnumerable<T> => Array.Empty<T>()
            if (def == typeof(IEnumerable<>))
                return $"System.Array.Empty<{tArg.GetFriendlyName()}>()";

            // ICollection<T> / IList<T> => new List<T>()
            if (def == typeof(ICollection<>) || def == typeof(IList<>))
                return $"new System.Collections.Generic.List<{tArg.GetFriendlyName()}>()";
        }

        // 값 타입이면 default(T)
        if (type.IsValueType)
            return $"default({type.GetFriendlyName()})";

        // 나머지 참조 타입은 null
        return "null";
    }


    private static void EmitOutDefaultAssignments(StringBuilder sb, ParameterInfo[] parameters)
    {
        foreach (var p in parameters.Where(p => p.IsOut))
        {
            var elementType = p.ParameterType.GetElementType();
            sb.AppendLine($"            {p.Name} = default({elementType.GetFriendlyName()});");
        }
    }

}

/// <summary>
/// 타입 이름을 코드에 쓰기 좋게 변환
/// </summary>
internal static class TypeNameExtensions
{
    public static string GetFriendlyName(this Type type)
    {
        // C# keyword aliases (중요)
        if (type == typeof(void)) return "void";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(sbyte)) return "sbyte";
        if (type == typeof(short)) return "short";
        if (type == typeof(ushort)) return "ushort";
        if (type == typeof(int)) return "int";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(long)) return "long";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(float)) return "float";
        if (type == typeof(double)) return "double";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(char)) return "char";
        if (type == typeof(string)) return "string";
        if (type == typeof(object)) return "object";

        // byref/ref/out 처리
        if (type.IsByRef)
        {
            var element = type.GetElementType();
            return $"{element.GetFriendlyName()}&";
        }

        // 배열
        if (type.IsArray)
        {
            return $"{type.GetElementType().GetFriendlyName()}[]";
        }

        // 제네릭
        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            var args = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyName));
            return $"{name}<{args}>";
        }

        return type.Name;
    }
}

#endif
