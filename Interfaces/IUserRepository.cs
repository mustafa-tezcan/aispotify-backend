using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using spotifyapp.Models;

namespace spotifyapp.Interfaces
{
    public interface IUserRepository
    {
        Task CreateAsync(User user);
        Task<User?> GetBySpotifyIdAsync(string spotfyId);
        Task UpdateAsync(User user);

        Task<User?> GetByIdAsync(int userId);
    }
}