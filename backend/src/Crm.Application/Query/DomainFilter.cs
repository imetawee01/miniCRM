using System.Linq.Expressions;
using System.Text.Json;

namespace Crm.Application.Query;

public enum MetaFieldType { String, Number, Boolean, Date, DateTime, Enum, Guid, Relation }

public record MetaFieldDto(
    string Name,
    string LabelEn,
    string LabelAr,
    string Type,
    IReadOnlyList<string> Operators,
    string? Relation,
    bool Groupable,
    bool Sortable,
    bool Exportable,
    string? RequiredPermission);

/// <summary>Odoo-style domain leaf or combiner. Leaf: [field, op, value]. Combiner: "&amp;" or "|".</summary>
public abstract record DomainNode;

public sealed record DomainLeaf(string Field, string Op, JsonElement? Value) : DomainNode;

public sealed record DomainCombiner(string Op) : DomainNode; // & or |

public static class DomainFilterParser
{
    /// <summary>Parse a JSON domain array, e.g. [["stageCode","=","Qualification"],"&amp;",["isClosed","=",false]].</summary>
    public static IReadOnlyList<DomainNode> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("Domain filter must be a JSON array.");

        var nodes = new List<DomainNode>();
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            if (el.ValueKind == JsonValueKind.String)
            {
                var s = el.GetString()!;
                if (s is "&" or "|" or "!")
                    nodes.Add(new DomainCombiner(s));
                else
                    throw new ArgumentException($"Unknown domain combiner '{s}'.");
            }
            else if (el.ValueKind == JsonValueKind.Array)
            {
                var arr = el.EnumerateArray().ToList();
                if (arr.Count < 2)
                    throw new ArgumentException("Domain leaf needs [field, op, value?].");
                var field = arr[0].GetString() ?? throw new ArgumentException("Field name required.");
                var op = arr[1].GetString() ?? throw new ArgumentException("Operator required.");
                JsonElement? value = arr.Count > 2 ? arr[2].Clone() : null;
                nodes.Add(new DomainLeaf(field, op, value));
            }
            else
                throw new ArgumentException("Invalid domain node.");
        }
        return nodes;
    }
}

public sealed class DomainFilterBuilder<T>
{
    private readonly Dictionary<string, DomainFieldMap<T>> _fields;

    public DomainFilterBuilder(IEnumerable<DomainFieldMap<T>> fields)
        => _fields = fields.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

    public Expression<Func<T, bool>>? Build(IReadOnlyList<DomainNode> nodes)
    {
        if (nodes.Count == 0) return null;

        // Convert polish-ish list of leaves with &/| between them into expression.
        // Spec: [["a","=",1],"&",["b","=",2]] → a AND b. Default AND when combiner omitted.
        var stack = new Stack<Expression>();
        string? pendingCombiner = null;

        foreach (var node in nodes)
        {
            if (node is DomainCombiner c)
            {
                pendingCombiner = c.Op;
                continue;
            }

            if (node is DomainLeaf leaf)
            {
                if (!_fields.TryGetValue(leaf.Field, out var map))
                    throw new ArgumentException($"Field '{leaf.Field}' is not filterable.");
                var expr = map.Build(leaf.Op, leaf.Value);
                if (stack.Count == 0)
                    stack.Push(expr);
                else
                {
                    var left = stack.Pop();
                    var op = pendingCombiner ?? "&";
                    pendingCombiner = null;
                    stack.Push(op == "|"
                        ? Expression.OrElse(left, expr)
                        : Expression.AndAlso(left, expr));
                }
            }
        }

        if (stack.Count != 1)
            throw new ArgumentException("Invalid domain expression.");

        var param = Expression.Parameter(typeof(T), "x");
        var body = new ParameterReplacer(param).Visit(stack.Pop());
        return Expression.Lambda<Func<T, bool>>(body!, param);
    }

    private sealed class ParameterReplacer(ParameterExpression param) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => param;
    }
}

public sealed class DomainFieldMap<T>
{
    public required string Name { get; init; }
    public required MetaFieldType Type { get; init; }
    public required Func<Expression, Expression> Accessor { get; init; } // takes ParameterExpression → member
    public ParameterExpression Parameter { get; init; } = Expression.Parameter(typeof(T), "x");

    public Expression Build(string op, JsonElement? value)
    {
        var left = Accessor(Parameter);
        var o = op.Trim().ToLowerInvariant();

        return o switch
        {
            "=" or "==" => Expression.Equal(left, Constant(left.Type, value)),
            "!=" or "<>" => Expression.NotEqual(left, Constant(left.Type, value)),
            ">" => Expression.GreaterThan(left, Constant(left.Type, value)),
            ">=" => Expression.GreaterThanOrEqual(left, Constant(left.Type, value)),
            "<" => Expression.LessThan(left, Constant(left.Type, value)),
            "<=" => Expression.LessThanOrEqual(left, Constant(left.Type, value)),
            "like" or "ilike" => BuildLike(left, value, o == "ilike"),
            "in" => BuildIn(left, value, not: false),
            "not in" => BuildIn(left, value, not: true),
            "is null" => BuildNull(left, isNull: true),
            "is not null" => BuildNull(left, isNull: false),
            "between" => BuildBetween(left, value),
            _ => throw new ArgumentException($"Operator '{op}' is not supported for '{Name}'.")
        };
    }

    private static Expression BuildNull(Expression left, bool isNull)
    {
        var underlying = Nullable.GetUnderlyingType(left.Type);
        if (left.Type == typeof(string))
        {
            Expression empty = Expression.Equal(left, Expression.Constant(null, typeof(string)));
            return isNull ? empty : Expression.Not(empty);
        }
        if (underlying is not null || !left.Type.IsValueType)
        {
            var eq = Expression.Equal(left, Expression.Constant(null, left.Type));
            return isNull ? eq : Expression.Not(eq);
        }
        throw new ArgumentException("is null not applicable to non-nullable value type.");
    }

    private static Expression BuildLike(Expression left, JsonElement? value, bool ignoreCase)
    {
        var needle = value?.GetString() ?? "";
        var constExpr = Expression.Constant(ignoreCase ? needle.ToLower() : needle);
        Expression target = left;
        if (ignoreCase)
        {
            var toLower = typeof(string).GetMethod(nameof(string.ToLower), System.Type.EmptyTypes)!;
            target = Expression.Call(left, toLower);
        }
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
        // null-safe: (left != null) && left.Contains(...)
        var notNull = Expression.NotEqual(left, Expression.Constant(null, typeof(string)));
        return Expression.AndAlso(notNull, Expression.Call(target, contains, constExpr));
    }

    private static Expression BuildIn(Expression left, JsonElement? value, bool not)
    {
        if (value is null || value.Value.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("'in' requires an array value.");
        var elemType = Nullable.GetUnderlyingType(left.Type) ?? left.Type;
        var listType = typeof(List<>).MakeGenericType(elemType);
        var list = Activator.CreateInstance(listType)!;
        var add = listType.GetMethod("Add")!;
        foreach (var item in value.Value.EnumerateArray())
            add.Invoke(list, [ConvertValue(elemType, item)]);
        var contains = listType.GetMethod("Contains", [elemType])!;
        Expression call;
        if (Nullable.GetUnderlyingType(left.Type) is not null)
        {
            var hasValue = Expression.Property(left, "HasValue");
            var val = Expression.Property(left, "Value");
            call = Expression.AndAlso(hasValue, Expression.Call(Expression.Constant(list), contains, val));
        }
        else
            call = Expression.Call(Expression.Constant(list), contains, left);
        return not ? Expression.Not(call) : call;
    }

    private static Expression BuildBetween(Expression left, JsonElement? value)
    {
        if (value is null || value.Value.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("'between' requires [from,to].");
        var arr = value.Value.EnumerateArray().ToList();
        if (arr.Count != 2) throw new ArgumentException("'between' requires exactly two values.");
        var lo = Constant(left.Type, arr[0]);
        var hi = Constant(left.Type, arr[1]);
        return Expression.AndAlso(Expression.GreaterThanOrEqual(left, lo), Expression.LessThanOrEqual(left, hi));
    }

    private static Expression Constant(Type type, JsonElement? value)
    {
        var converted = ConvertValue(type, value);
        return Expression.Constant(converted, type);
    }

    private static object? ConvertValue(Type type, JsonElement? value)
    {
        if (value is null || value.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return null;

        var target = Nullable.GetUnderlyingType(type) ?? type;
        var el = value.Value;

        if (target == typeof(string)) return el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        if (target == typeof(bool)) return el.ValueKind == JsonValueKind.True || (el.ValueKind == JsonValueKind.String && bool.Parse(el.GetString()!)) || (el.ValueKind == JsonValueKind.Number && el.GetInt32() != 0);
        if (target == typeof(Guid)) return el.ValueKind == JsonValueKind.String ? Guid.Parse(el.GetString()!) : throw new ArgumentException("Guid expected.");
        if (target == typeof(decimal)) return el.ValueKind == JsonValueKind.Number ? el.GetDecimal() : decimal.Parse(el.GetString()!);
        if (target == typeof(int)) return el.ValueKind == JsonValueKind.Number ? el.GetInt32() : int.Parse(el.GetString()!);
        if (target == typeof(long)) return el.ValueKind == JsonValueKind.Number ? el.GetInt64() : long.Parse(el.GetString()!);
        if (target == typeof(double)) return el.ValueKind == JsonValueKind.Number ? el.GetDouble() : double.Parse(el.GetString()!);
        if (target == typeof(DateTime))
        {
            var s = el.ValueKind == JsonValueKind.String ? el.GetString()! : el.ToString();
            return DateTime.Parse(s, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }
        if (target.IsEnum)
        {
            var s = el.ValueKind == JsonValueKind.String ? el.GetString()! : el.ToString();
            return Enum.Parse(target, s, ignoreCase: true);
        }
        throw new ArgumentException($"Cannot convert value for type {target.Name}.");
    }
}
