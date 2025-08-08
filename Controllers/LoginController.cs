using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using HospitalAPI.Models;
using HospitalAPI.Repository;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace HospitalAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : ControllerBase
    {

        private readonly ILogger<LoginController> _logger;

        public LoginController(ILogger<LoginController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        private string GenerateJwtToken(string username)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("RDF"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
              issuer: "IssuerServer",
              audience: "AudienceServer",
              claims: claims,
              expires: DateTime.UtcNow,
              signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        [Authorize]
        [HttpGet(Name = "Login")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(GetResponse))]
        public IActionResult Get(string username, string password)
        {
            try
            {
                using (var context = new HospitalDbContext())
                {
                    try
                    {
                        var user = context.doctors.FirstOrDefault(x => x.Username == username && x.Password == password);
                        if (user == null)
                            return BadRequest(new GetResponse()
                            {
                                Status = "KO",
                                Message = "Doctor doesn't exists"
                            });
                        else
                            return Ok(new GetResponse()
                            {
                                Status = "OK",
                                Message = $"{username} logged succesfully"
                            });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                        return BadRequest(new GetResponse()
                        {
                            Status = "KO",
                            Message = ex.Message
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return BadRequest(new GetResponse()
                {
                    Status = "KO",
                    Message = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("AddUser", Name = "AddUser")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponsePostCreateUser))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ResponsePostCreateUser))]
        public IActionResult CreateDoctor(Doctor doctor)
        {
            try
            {
                using (var context = new HospitalDbContext())
                {
                    var newDoctor = new Doctor()
                    {
                        Name = doctor.Name,
                        Surname = doctor.Surname,
                        Username = doctor.Username,
                        Password = doctor.Password,
                        Speciality = doctor.Speciality,
                        Phone = doctor.Phone,
                        Department = doctor.Department,
                        Admin = doctor.Admin
                    };
                    try
                    {
                        var alresdyExist = context.doctors.FirstOrDefault(x => x.Username == newDoctor.Username);
                        if (alresdyExist != null)
                            return BadRequest(new ResponsePostCreateUser()
                            {
                                Status = "KO",
                                Username = newDoctor.Username,
                                Message = "already exists in our database"
                            });

                        context.doctors.Add(newDoctor);
                        context.SaveChanges();

                        var response = new ResponsePostCreateUser()
                        {
                            Status = "OK",
                            Username = newDoctor.Username,
                            Message = $"Succesfully created a new Doctor"
                        };
                        return Ok(response);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                        var response = new ResponsePostCreateUser()
                        {
                            Status = "KO",
                            Username = newDoctor.Username,
                            Message = ex.Message
                        };

                        return BadRequest(response);
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                return BadRequest(new ResponsePostCreateUser()
                {
                    Status = "KO",
                    Username = doctor.Name,
                    Message = ex.Message
                });
            }
        }
    }
}

