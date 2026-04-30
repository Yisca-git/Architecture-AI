using Entities;
using DTOs;
using Microsoft.AspNetCore.Mvc;
using Repositories;
using Services;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using EventDressRental.Attributes; 

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EventDressRental.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserPasswordService _userPasswordService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, IUserPasswordService userPasswordService, ILogger<UsersController> logger)
        {
            _logger = logger;
            _userService = userService;
            _userPasswordService = userPasswordService;
        }

        // GET: api/<UsersController>
        [HttpGet]
        [AuthorizeRoles("Admin")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> Get()
        {
            List<UserDTO> users = await _userService.GetUsers();
            if(users.Count()==0)
                return NoContent();
            return Ok(users);
        }

        // GET api/<UsersController>/5
        [HttpGet("{id}")]
        [AuthorizeRoles("Admin", "User")]
        public async Task<ActionResult<UserDTO>> GetUserId(int id)
        {
            UserDTO user = await _userService.GetUserById(id);
            return user != null ? Ok(user) : NotFound();
        }

        // POST api/<UsersController>
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<UserDTO>> AddUser([FromBody] UserRegisterDTO newUser)
        {
            int passwordScore = _userPasswordService.CheckPassword(newUser.Password);
            if(passwordScore < 2)
            {
                _logger.LogWarning("Registration failed: weak password for {FirstName} {LastName}", newUser.FirstName, newUser.LastName);
                return BadRequest("Password is not strong enough");
            }
            var (user, token) = await _userService.AddUser(newUser);
            
            // Set HTTP-only cookie with JWT token
            SetJwtCookie(token);
            
            // Remove password from response for security
            var userResponse = user with { Password = "" };
            
            _logger.LogInformation("User registered successfully: {FirstName} {LastName}", user.FirstName, user.LastName);
            return CreatedAtAction(nameof(GetUserId), new { Id = user.Id }, userResponse);
        }
        // POST api/<UsersController>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDTO>> LogIn([FromBody] UserLoginDTO existingUser)
        {
            _logger.LogInformation("Login attempt for {FirstName} {LastName}", existingUser.FirstName, existingUser.LastName);
            var (user, token) = await _userService.LogIn(existingUser);
            if(user == null)
            {
                _logger.LogWarning("Login failed for {FirstName} {LastName}", existingUser.FirstName, existingUser.LastName);
                return Unauthorized("user name or password are wrong");
            }
            
            // Set HTTP-only cookie with JWT token
            SetJwtCookie(token);
            
            // Remove password from response for security
            var userResponse = user with { Password = "" };
            
            _logger.LogInformation("Login succeeded for user {UserId} {Email}", user.Id, user.Email);
            return Ok(userResponse);
        }
        // PUT api/<UsersController>/5
        [HttpPut("{id}")]
        [AuthorizeRoles("Admin", "User")]
        public async Task<IActionResult> Put(int id, [FromBody] UserRegisterDTO updateUser)
        {
            if (await _userService.IsExistsUserById(id) == false)
            {
                _logger.LogWarning("User update failed: user {UserId} not found", id);
                return NotFound(id);
            }
            int passwordScore = _userPasswordService.CheckPassword(updateUser.Password);
            if (passwordScore < 2)
            {
                _logger.LogWarning("User update failed: weak password for user {UserId}", id);
                return BadRequest("Password is not strong enough");
            }
            await _userService.UpdateUser(id, updateUser);
            _logger.LogInformation("User updated successfully: {UserId}", id);
            return Ok();
        }

        // POST api/<UsersController>/logout
        [HttpPost("logout")]
        [AllowAnonymous]
        public IActionResult Logout()
        {
            // Delete the JWT cookie
            Response.Cookies.Delete("jwtToken");
            _logger.LogInformation("User logged out");
            return Ok(new { message = "Logged out successfully" });
        }

        // Helper method to set JWT cookie
        private void SetJwtCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,        // Prevents JavaScript access (XSS protection)
                Secure = true,          // Only sent over HTTPS
                SameSite = SameSiteMode.Strict, // CSRF protection
                Expires = DateTimeOffset.UtcNow.AddMinutes(60) // Match token expiry
            };
            Response.Cookies.Append("jwtToken", token, cookieOptions);
        }
    }
}
