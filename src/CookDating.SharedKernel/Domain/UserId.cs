namespace CookDating.SharedKernel.Domain;

public sealed class UserId : ValueObject
{
    public string Value { get; }

    public UserId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("UserId cannot be empty", nameof(value));
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(UserId userId) => userId.Value;
    public static explicit operator UserId(string value) => new(value);
}
