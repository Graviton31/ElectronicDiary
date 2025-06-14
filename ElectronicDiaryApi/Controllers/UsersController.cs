using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        if (await _context.Users.AnyAsync(u => u.Login == dto.Login))
            return BadRequest("Пользователь с таким логином уже существует");

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

    #endregion

    #region Parent & Child Endpoints

    [HttpGet("search-parents")]
    public async Task<IActionResult> SearchParents([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return BadRequest("Минимум 2 символа для поиска");

        var parents = await _context.Users
            .Where(u => u.Role == "родитель" && u.IsDelete != true &&
                       (u.Name.Contains(query) ||
                        u.Surname.Contains(query) ||
                        u.Patronymic.Contains(query) ||
                        u.Phone.Contains(query)))
            .Select(u => new ParentSearchResultDto
            {
                Id = u.IdUser,
                FullName = $"{u.Surname} {u.Name} {u.Patronymic}",
                Phone = u.Phone
            })
            .Take(10)
            .ToListAsync();

        return Ok(parents);
    }

    [HttpPost("register-parent")]
    public async Task<IActionResult> RegisterParent([FromBody] RegisterParentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.Users.AnyAsync(u => u.Login == dto.Login))
            return BadRequest("Пользователь с таким логином уже существует");

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
        if (await _context.Users.AnyAsync(u => u.Login == dto.Login))
            return BadRequest("Пользователь с таким логином уже существует");

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

    #endregion
}