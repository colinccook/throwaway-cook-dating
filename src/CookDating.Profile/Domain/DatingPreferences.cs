using CookDating.SharedKernel.Domain;

namespace CookDating.Profile.Domain;

public class DatingPreferences : ValueObject
{
    public Gender? PreferredGender { get; private set; }
    public int MinAge { get; private set; }
    public int MaxAge { get; private set; }
    public int MaxDistanceKm { get; private set; }

    public DatingPreferences(Gender? preferredGender, int minAge, int maxAge, int maxDistanceKm)
    {
        if (minAge < 18) throw new ArgumentException("Minimum age must be at least 18", nameof(minAge));
        if (maxAge < minAge) throw new ArgumentException("Max age must be >= min age", nameof(maxAge));
        if (maxDistanceKm <= 0) throw new ArgumentException("Max distance must be positive", nameof(maxDistanceKm));

        PreferredGender = preferredGender;
        MinAge = minAge;
        MaxAge = maxAge;
        MaxDistanceKm = maxDistanceKm;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return PreferredGender;
        yield return MinAge;
        yield return MaxAge;
        yield return MaxDistanceKm;
    }
}
