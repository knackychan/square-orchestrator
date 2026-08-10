namespace Square.Domain.Primitives;


public readonly record struct ProjectId : IStrongId<ProjectId>, IComparable<ProjectId>
{
    private readonly string? value;

    private ProjectId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "prj";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out ProjectId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new ProjectId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static ProjectId Parse(string text)
    {
        return TryParse(text, out ProjectId result)
            ? result
            : throw new FormatException($"Invalid ProjectId value '{text}'.");
    }

    public static ProjectId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(ProjectId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct RequestId : IStrongId<RequestId>, IComparable<RequestId>
{
    private readonly string? value;

    private RequestId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "req";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out RequestId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new RequestId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static RequestId Parse(string text)
    {
        return TryParse(text, out RequestId result)
            ? result
            : throw new FormatException($"Invalid RequestId value '{text}'.");
    }

    public static RequestId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(RequestId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct TaskId : IStrongId<TaskId>, IComparable<TaskId>
{
    private readonly string? value;

    private TaskId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "tsk";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out TaskId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new TaskId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static TaskId Parse(string text)
    {
        return TryParse(text, out TaskId result)
            ? result
            : throw new FormatException($"Invalid TaskId value '{text}'.");
    }

    public static TaskId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(TaskId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct AttemptId : IStrongId<AttemptId>, IComparable<AttemptId>
{
    private readonly string? value;

    private AttemptId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "att";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out AttemptId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new AttemptId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static AttemptId Parse(string text)
    {
        return TryParse(text, out AttemptId result)
            ? result
            : throw new FormatException($"Invalid AttemptId value '{text}'.");
    }

    public static AttemptId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(AttemptId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct TerminalId : IStrongId<TerminalId>, IComparable<TerminalId>
{
    private readonly string? value;

    private TerminalId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "trm";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out TerminalId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new TerminalId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static TerminalId Parse(string text)
    {
        return TryParse(text, out TerminalId result)
            ? result
            : throw new FormatException($"Invalid TerminalId value '{text}'.");
    }

    public static TerminalId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(TerminalId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct ArtifactId : IStrongId<ArtifactId>, IComparable<ArtifactId>
{
    private readonly string? value;

    private ArtifactId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "art";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out ArtifactId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new ArtifactId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static ArtifactId Parse(string text)
    {
        return TryParse(text, out ArtifactId result)
            ? result
            : throw new FormatException($"Invalid ArtifactId value '{text}'.");
    }

    public static ArtifactId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(ArtifactId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct GateId : IStrongId<GateId>, IComparable<GateId>
{
    private readonly string? value;

    private GateId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "gat";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out GateId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new GateId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static GateId Parse(string text)
    {
        return TryParse(text, out GateId result)
            ? result
            : throw new FormatException($"Invalid GateId value '{text}'.");
    }

    public static GateId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(GateId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct InteractionId : IStrongId<InteractionId>, IComparable<InteractionId>
{
    private readonly string? value;

    private InteractionId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "int";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out InteractionId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new InteractionId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static InteractionId Parse(string text)
    {
        return TryParse(text, out InteractionId result)
            ? result
            : throw new FormatException($"Invalid InteractionId value '{text}'.");
    }

    public static InteractionId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(InteractionId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct RouteId : IStrongId<RouteId>, IComparable<RouteId>
{
    private readonly string? value;

    private RouteId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "rte";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out RouteId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new RouteId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static RouteId Parse(string text)
    {
        return TryParse(text, out RouteId result)
            ? result
            : throw new FormatException($"Invalid RouteId value '{text}'.");
    }

    public static RouteId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(RouteId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct SpecialistId : IStrongId<SpecialistId>, IComparable<SpecialistId>
{
    private readonly string? value;

    private SpecialistId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "spc";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out SpecialistId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new SpecialistId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static SpecialistId Parse(string text)
    {
        return TryParse(text, out SpecialistId result)
            ? result
            : throw new FormatException($"Invalid SpecialistId value '{text}'.");
    }

    public static SpecialistId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(SpecialistId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct SkillId : IStrongId<SkillId>, IComparable<SkillId>
{
    private readonly string? value;

    private SkillId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "skl";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out SkillId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new SkillId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static SkillId Parse(string text)
    {
        return TryParse(text, out SkillId result)
            ? result
            : throw new FormatException($"Invalid SkillId value '{text}'.");
    }

    public static SkillId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(SkillId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct EventId : IStrongId<EventId>, IComparable<EventId>
{
    private readonly string? value;

    private EventId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "evt";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out EventId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new EventId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static EventId Parse(string text)
    {
        return TryParse(text, out EventId result)
            ? result
            : throw new FormatException($"Invalid EventId value '{text}'.");
    }

    public static EventId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(EventId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct ReceiptId : IStrongId<ReceiptId>, IComparable<ReceiptId>
{
    private readonly string? value;

    private ReceiptId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "rcp";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out ReceiptId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new ReceiptId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static ReceiptId Parse(string text)
    {
        return TryParse(text, out ReceiptId result)
            ? result
            : throw new FormatException($"Invalid ReceiptId value '{text}'.");
    }

    public static ReceiptId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(ReceiptId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}


public readonly record struct CorrelationId : IStrongId<CorrelationId>, IComparable<CorrelationId>
{
    private readonly string? value;

    private CorrelationId(string value)
    {
        this.value = value;
    }

    public static string Prefix => "cor";
    public string Value => value ?? string.Empty;

    public static bool TryParse(string? text, out CorrelationId result)
    {
        if (StrongIdText.TryNormalize(text, Prefix, out string canonical))
        {
            result = new CorrelationId(canonical);
            return true;
        }
        result = default;
        return false;
    }

    public static CorrelationId Parse(string text)
    {
        return TryParse(text, out CorrelationId result)
            ? result
            : throw new FormatException($"Invalid CorrelationId value '{text}'.");
    }

    public static CorrelationId FromCanonical(string canonicalValue) => Parse(canonicalValue);
    public int CompareTo(CorrelationId other) => StringComparer.Ordinal.Compare(Value, other.Value);
    public override string ToString() => Value;
}
