using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using spotifyapp.Models;

namespace spotifyapp.Interfaces
{
    public interface ITokenService
    {
        public string CreateToken(User user);
    }
}