using SmreaderAPI.Application.DTOs;

namespace SmreaderAPI.Application.Interfaces;

public interface IUserService
{
    Task<ApiResponse<TokenResponseDto>> RegisterAsync(RegisterDto dto);
    Task<ApiResponse<TokenResponseDto>> LoginAsync(LoginDto dto);
    Task<ApiResponse<TokenResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto);
    Task<ApiResponse<UserDto>> GetByIdAsync(int id);
    Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync();
    Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}
