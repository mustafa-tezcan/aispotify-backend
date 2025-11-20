using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using spotifyapp.Data;
using spotifyapp.Interfaces;
using spotifyapp.Models;

namespace spotifyapp.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDBContext _context;

        public UserRepository(ApplicationDBContext context)
        {
            _context = context;
        }


        public async Task CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetBySpotifyIdAsync(string spotifyId)
        {
            return await _context.Users
            .FirstOrDefaultAsync(u => u.SpotifyId == spotifyId);
        }


        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

    }
}