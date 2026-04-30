using AutoMapper;
using Entities;
using DTOs;
using Repositories;
namespace Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserPasswordService _userPasswordService;
        private readonly IMapper _mapper;
        private readonly IJwtService _jwtService;
        
        public UserService(IUserRepository userRepository, IMapper mapper, IUserPasswordService userPasswordService, IJwtService jwtService)
        {
            _userRepository = userRepository;
            _userPasswordService = userPasswordService;
            _mapper = mapper;
            _jwtService = jwtService;
        }
        public async Task<bool> IsExistsUserById(int id)
        {
            return await _userRepository.IsExistsUserById(id);
        }
        public bool CheckUser(int id)
        {
            return true;
        }
        public async Task<List<UserDTO>> GetUsers()
        {
            List<User> users = await _userRepository.GetUsers();
            List<UserDTO> usersDTO = _mapper.Map<List<User>, List<UserDTO>>(users);
            return usersDTO;
        }
        public async Task<UserDTO> GetUserById(int id)
        {
            User? user= await _userRepository.GetUserById(id);
            if (user == null)
                return null;
            UserDTO userDTO = _mapper.Map<User, UserDTO>(user);
            return userDTO;
        }
        public async Task<(UserDTO user, string token)> AddUser(UserRegisterDTO newUser)
        {
            User userRegister = _mapper.Map<UserRegisterDTO, User>(newUser);
            // Set default role to "User" if not specified
            if (string.IsNullOrEmpty(userRegister.Role))
            {
                userRegister.Role = "User";
            }
            User user = await _userRepository.AddUser(userRegister);
            UserDTO userDTO = _mapper.Map<User, UserDTO>(user);
            
            // Generate JWT token for auto-login after registration
            string token = _jwtService.GenerateToken(userDTO);
            
            return (userDTO, token);
        }
        public async Task<(UserDTO user, string token)> LogIn(UserLoginDTO existUser)
        {
            User loginUser = _mapper.Map<UserLoginDTO,User>(existUser);
            User? user = await _userRepository.LogIn(loginUser);
            if (user == null)
                return (null, null);
            UserDTO userDTO = _mapper.Map<User, UserDTO>(user);
            
            // Generate JWT token after successful login
            string token = _jwtService.GenerateToken(userDTO);
            
            return (userDTO, token);
        }

        public async Task UpdateUser(int id, UserRegisterDTO updateUser)
        {
            User user = _mapper.Map<UserRegisterDTO, User>(updateUser);
            await _userRepository.UpdateUser(user);
        }
    }
}
