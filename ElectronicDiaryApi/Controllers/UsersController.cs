using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace ElectronicDiaryApi.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly ElectronicDiaryContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UsersController(ElectronicDiaryContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    #region Employee Endpoints

    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts()
    {
        var posts = await _context.Posts
            .Select(p => new { p.IdPost, p.PostName })
            .ToListAsync();

        return Ok(posts);
    }

    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromBody] string postName)
    {
        var post = new Post { PostName = postName };
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        return Ok(new { post.IdPost, post.PostName });
    }

    [HttpPost("register-employee")]
    public async Task<IActionResult> RegisterEmployee([FromBody] RegisterEmployeeDto dto)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Login == dto.Login))
            {
                return Conflict(new
                {
                    Message = "Пользователь с таким логином уже существует",
                    Suggestion = "Используйте другой логин или email-адрес"
                });
            }

            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    Message = "Неверные данные",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });

            var user = new User
            {
                Login = dto.Login,
                Name = dto.Name,
                Surname = dto.Surname,
                Patronymic = dto.Patronymic,
                BirthDate = dto.BirthDate,
                Phone = dto.Phone,
                Role = dto.Role,
                IsDelete = false
            };

            user.Password = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var employee = new Employee
            {
                IdEmployee = user.IdUser,
                IdPost = dto.PostId
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return Ok(new { UserId = user.IdUser });
            }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    #endregion

    #region Parent & Child Endpoints

    [HttpGet("search-parents")]
    public async Task<IActionResult> SearchParents([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return BadRequest(new { Message = "Минимум 2 символа для поиска" });

            var parents = await _context.Users
                .Where(u => u.Role == "родитель" && u.IsDelete != true &&
                  (EF.Functions.Like(u.Name, $"%{query}%") ||
                   EF.Functions.Like(u.Surname, $"%{query}%") ||
                   EF.Functions.Like(u.Patronymic, $"%{query}%") ||
                   EF.Functions.Like(u.Phone, $"%{query}%")))
                .Select(u => new
                {
                    id = u.IdUser,
                    fullName = $"{u.Surname} {u.Name} {u.Patronymic}".Trim(),
                    phone = u.Phone,
                    login = u.Login
                })
                .Take(10)
                .ToListAsync();

            return Ok(parents);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Ошибка при поиске родителей" });
        }
    }

    [HttpPost("register-parent")]
    public async Task<IActionResult> RegisterParent([FromBody] RegisterParentDto dto)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Login == dto.Login))
            {
                return Conflict(new
                {
                    Message = "Пользователь с таким логином уже существует",
                    Suggestion = "Используйте другой логин или email-адрес"
                });
            }

            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    Message = "Неверные данные",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });

            var user = new User
            {
                Login = dto.Login,
                Name = dto.Name,
                Surname = dto.Surname,
                Patronymic = dto.Patronymic,
                BirthDate = dto.BirthDate,
                Phone = dto.Phone,
                Role = "родитель",
                IsDelete = false
            };

            user.Password = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var parent = new Parent
            {
                IdParent = user.IdUser,
                Workplace = dto.Workplace
            };

            _context.Parents.Add(parent);
            await _context.SaveChangesAsync();

            return Ok(new { UserId = user.IdUser });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.Message });
        }
    }

    [HttpPost("register-child")]
    public async Task<IActionResult> RegisterChild([FromBody] RegisterChildWithParentsDto dto)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Login == dto.Login))
            {
                return Conflict(new
                {
                    Message = "Пользователь с таким логином уже существует",
                    Suggestion = "Используйте другой логин или email-адрес"
                });
            }

            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    Message = "Неверные данные",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });

            // Создаем пользователя-ребенка
            var user = new User
            {
                Login = dto.Login,
                Name = dto.Name,
                Surname = dto.Surname,
                Patronymic = dto.Patronymic,
                BirthDate = dto.BirthDate,
                Phone = dto.Phone,
                Role = "студент",
                IsDelete = false
            };

            user.Password = _passwordHasher.HashPassword(user, dto.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Создаем запись студента
            var student = new Student
            {
                IdStudent = user.IdUser,
                EducationName = dto.EducationName
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Добавляем связи с родителями
            foreach (var parentId in dto.ParentIds)
            {
                var relation = new StudentsHasParent
                {
                    IdParent = parentId,
                    IdStudent = student.IdStudent,
                    ParentRole = dto.ParentRole
                };
                _context.StudentsHasParents.Add(relation);
            }

            await _context.SaveChangesAsync();

            return Ok(new { UserId = user.IdUser });
            }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("register-parent-with-children")]
    public async Task<IActionResult> RegisterParentWithChildren([FromBody] RegisterParentWithChildrenDto dto)
    {
        try
        {
            // Проверка уникальности логина родителя (без учета регистра)
            if (await _context.Users.AnyAsync(u => u.Login == dto.Login))
            {
                return Conflict(new
                {
                    Message = "Пользователь с таким логином уже существует",
                    Suggestion = "Используйте другой логин или email-адрес"
                });
            }

            // Проверка уникальности логинов детей
            foreach (var childDto in dto.Children)
            {
                if (await _context.Users.AnyAsync(u => u.Login == childDto.Login))
                {
                    return Conflict(new
                    {
                        Message = $"Логин '{childDto.Login}' для ребенка уже занят",
                        Suggestion = "Используйте другой логин"
                    });
                }
            }

            // Создаем родителя
            var parentUser = new User
            {
                Login = dto.Login,
                Name = dto.Name,
                Surname = dto.Surname,
                Patronymic = dto.Patronymic,
                BirthDate = dto.BirthDate,
                Phone = dto.Phone,
                Role = "родитель",
                IsDelete = false
            };
            parentUser.Password = _passwordHasher.HashPassword(parentUser, dto.Password);
            _context.Users.Add(parentUser);
            await _context.SaveChangesAsync();

            // Создаем запись родителя
            var parent = new Parent
            {
                IdParent = parentUser.IdUser,
                Workplace = dto.Workplace
            };
            _context.Parents.Add(parent);

            // Создаем детей
            var childUserIds = new List<int>();
            foreach (var childDto in dto.Children)
            {
                var childUser = new User
                {
                    Login = childDto.Login,
                    Name = childDto.Name,
                    Surname = childDto.Surname,
                    Patronymic = childDto.Patronymic,
                    BirthDate = childDto.BirthDate,
                    Phone = childDto.Phone,
                    Role = "студент",
                    IsDelete = false
                };
                childUser.Password = _passwordHasher.HashPassword(childUser, childDto.Password);
                _context.Users.Add(childUser);
                await _context.SaveChangesAsync();

                var student = new Student
                {
                    IdStudent = childUser.IdUser,
                    EducationName = childDto.EducationName
                };
                _context.Students.Add(student);

                // Добавляем связь с родителем с правильной ролью
                _context.StudentsHasParents.Add(new StudentsHasParent
                {
                    IdParent = parentUser.IdUser,
                    IdStudent = childUser.IdUser,
                    ParentRole = childDto.ParentRole // Используем выбранную роль
                });

                childUserIds.Add(childUser.IdUser);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ParentId = parentUser.IdUser,
                ChildrenIds = childUserIds,
                Message = "Регистрация успешно завершена"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, new
            {
                Message = "Ошибка при регистрации",
                Detailed = ex.Message
            });
        }
    }

    #endregion

    private IActionResult HandleException(Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }

        string errorMessage = ex switch
        {
            DbUpdateException => "Ошибка сохранения данных в базу",
            ArgumentException => ex.Message,
            _ => "Произошла непредвиденная ошибка"
        };

        return BadRequest(new
        {
            Message = errorMessage,
            Detailed = ex.InnerException?.Message
        });
    }
}