using AutoDealerSphere.Server.Services;
using AutoDealerSphere.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoDealerSphere.Server.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UserController : ControllerBase
	{
		private readonly SQLDBContext? _context;

		public UserController(SQLDBContext context)
		{
			_context ??= context;
		}

		[HttpGet]
		public async Task<IList<User>> GetUsers()
		{
			List<User> users = new List<User>();
			if (_context != null)
			{
				users = await _context.Users.ToListAsync();
			}
			return users;
		}

		[HttpPost]
		public async Task<IList<User>> GetUsers(UserSearch item)
		{
			List<User> users = new List<User>();
			if (_context != null)
			{
				var sql = _context.Users as IQueryable<User>;
				if (0 < item.Id) sql = sql.Where(a => a.Id == item.Id);
				if (!string.IsNullOrEmpty(item.Name)) sql = sql.Where(a => a.Name.Contains(item.Name));
				if (!string.IsNullOrEmpty(item.Email)) sql = sql.Where(a => a.Email.Contains(item.Email));
				users = await sql.ToListAsync();
			}
			return users;
		}

		[HttpGet("{id}")]
		public async Task<User> GetUser(int id)
		{
			User? user = null;
			if (_context != null)
			{
				user ??= await _context.Users.FindAsync(id);
			}
			return user;
		}

		[Route("login")]
		[HttpPost]
		public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
		{
			if (_context == null)
			{
				return new LoginResponse { Success = false, ErrorMessage = "データベース接続エラー" };
			}

			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
			if (user == null || !PasswordHashService.VerifyPassword(request.Password, user.Password))
			{
				return new LoginResponse { Success = false, ErrorMessage = "メールアドレスまたはパスワードが正しくありません。" };
			}

			return new LoginResponse { Success = true, User = user };
		}

		[Route("add")]
		[HttpPost]
		public async Task<IActionResult> SaveUser(User user)
		{
			if (_context?.Users == null)
			{
				return Problem("Entity set 'SQLDBContext.User' is null.");
			}
			if (user != null)
			{
				// パスワードをハッシュ化
				user.Password = PasswordHashService.HashPassword(user.Password);
				_context.Add(user);
				await _context.SaveChangesAsync();
				return Ok("Saved Successfully!!");
			}
			return NoContent();
		}

		[Route("update")]
		[HttpPost]
		public async Task<IActionResult> UpdateUser(User user)
		{
			if (null == _context) return NotFound();
			
			// 既存のユーザー情報を取得
			var existingUser = await _context.Users.FindAsync(user.Id);
			if (existingUser == null) return NotFound();
			
			// パスワードが変更された場合のみハッシュ化
			if (user.Password != existingUser.Password)
			{
				user.Password = PasswordHashService.HashPassword(user.Password);
			}
			
			_context.Entry(existingUser).State = EntityState.Detached;
			_context.Entry(user).State = EntityState.Modified;
			
			try
			{
				await _context.SaveChangesAsync();
				return Ok("Updated Successfully!!");
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!UserExists(user.Id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteUser(int id)
		{
			if (_context?.Users == null)
			{
				return NotFound();
			}
			var user = await _context.Users.FindAsync(id);
			if (user == null)
			{
				return NotFound();
			}
			_context.Users.Remove(user);
			await _context.SaveChangesAsync();
			return NoContent();
		}

		private bool UserExists(int id)
		{
			if (null == _context) return false;
			return (_context.Users?.Any(e => e.Id == id)).GetValueOrDefault();
		}
	}
}
