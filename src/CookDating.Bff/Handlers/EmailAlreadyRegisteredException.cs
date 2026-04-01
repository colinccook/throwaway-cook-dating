namespace CookDating.Bff.Handlers;

public class EmailAlreadyRegisteredException : Exception
{
    public EmailAlreadyRegisteredException(string email)
        : base($"Email already registered: {email}")
    {
        Email = email;
    }

    public string Email { get; }
}
