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
