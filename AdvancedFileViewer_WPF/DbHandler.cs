using System.Data.Entity.Migrations;
using System.Linq;

namespace AdvancedFileViewer_WPF
{
    static class DbHandler
    {
        //private static readonly UsersContext _context = new UsersContext();

        public static Users GetUserInfo(string name, string password)
        {
            using (var context = new UsersContext())
            {
                var foundUser = context.Users.Where((user) => (user.Name == name && user.Password == password));

                return foundUser.FirstOrDefault();
            }
        }

        public static void AddOrUpdateUserInfo(Users newUserInfo)
        {
            using (var context = new UsersContext())
            {
                context.Users.AddOrUpdate(newUserInfo);
                context.SaveChanges();
            }
        }
    }
}
