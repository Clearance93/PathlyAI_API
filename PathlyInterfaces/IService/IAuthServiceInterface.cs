using Pathly_DTOs;

namespace PathlyInterfaces.IService
{
    public interface IAuthServiceInterface
    {
        Task<ResponseUserDto> AddNewUserAsync(UserDto dto);

        Task<ResponseUserDto> AuthenticateTheUserAsync(LoginDto dto);
    }
}