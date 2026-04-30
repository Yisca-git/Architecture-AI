using DTOs;

namespace Services
{
    public interface IJwtService
    {
        string GenerateToken(UserDTO user);
    }
}
