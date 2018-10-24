﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static void UpdateUserInfo(Users newUserInfo)
        {
            using (var context = new UsersContext())
            {
                var user = new Users
                {
                    Name = newUserInfo.Name,
                    CurrentDirectory = newUserInfo.CurrentDirectory,
                    Password = newUserInfo.Password
                    
                };


                if (user != null)
                {
                    user.CurrentDirectory = newUserInfo.CurrentDirectory;


                    //_context.Entry(foundUser).State = EntityState.Modified;
 //                   context.Users.Attach(foundUser);
                    
                    context.Users.AddOrUpdate(user);
                   // context.Entry(user).State = EntityState.Modified;  
                    context.SaveChanges();
                    //db.Entry(payment).State = EntityState.Modified;
                }
            }
        }
    }
}
