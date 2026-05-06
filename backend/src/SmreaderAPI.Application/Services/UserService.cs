using SmreaderAPI.Application.DTOs;
using SmreaderAPI.Application.Interfaces;

namespace SmreaderAPI.Application.Services;

public class UserService : IUserService
{
    public Task<ApiResponse<TokenResponseDto>> RegisterAsync(RegisterDto dto) => throw new NotImplementedException();
    public Task<ApiResponse<TokenResponseDto>> LoginAsync(LoginDto dto) => throw new NotImplementedException();
    public Task<ApiResponse<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto) => throw new NotImplementedException();
    public Task<ApiResponse<UserDto>> GetByIdAsync(int id) => throw new NotImplementedException();
    public Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync() => throw new NotImplementedException();
    public Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserDto dto) => throw new NotImplementedException();
    public Task<ApiResponse<bool>> DeleteAsync(int id) => throw new NotImplementedException();
}
