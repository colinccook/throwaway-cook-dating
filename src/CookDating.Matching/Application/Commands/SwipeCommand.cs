using CookDating.Matching.Domain;

namespace CookDating.Matching.Application.Commands;

public record SwipeCommand(
    string UserId,
    string TargetUserId,
    SwipeDirection Direction
);
