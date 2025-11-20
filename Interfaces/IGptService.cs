using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace spotifyapp.Interfaces
{
    public interface IGptService
    {
        Task<string> CreatePlaylistAsync(string prompt);
    }
}