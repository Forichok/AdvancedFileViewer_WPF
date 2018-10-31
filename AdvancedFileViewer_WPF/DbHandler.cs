using System.Data.Entity.Migrations;
using System.Linq;

namespace AdvancedFileViewer_WPF
{
    static class DbHandler
    {
        public static Users GetUserInfo(string name, string password)
        {
            using (var context = new UsersContext())
            {
                var foundUser = context.Users.Where((user) => (user.Name == name && user.Password == password));

                return foundUser.FirstOrDefault();
            }
        }

        public static bool IsExist(string name)
        {
            using (var context = new UsersContext())
            {
                var foundUser = context.Users.Where((user) => (user.Name == name));
                if (foundUser.Count() != 0) return true;
            }
            return false;
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
