using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedFileViewer_WPF
{
    static class DbHandler
    {
        private static readonly UsersContext _context = new UsersContext();

        public static Users GetUserInfo(string name, string password)
        {
            var foundUser = _context.Users.Where((user) =>( user.Name == name && user.Password == password));
            
            return foundUser.FirstOrDefault();
        }
    }
}
