using CookDating.Profile.Domain;

namespace CookDating.Profile.Application.Commands;

public record SetLookingStatusCommand(
    string UserId,
    LookingStatus Status
);
