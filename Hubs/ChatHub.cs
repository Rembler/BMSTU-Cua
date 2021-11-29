using Cua.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cua.Hubs
{
    public class ChatHub : Hub
    {
        private ApplicationContext db;

        public ChatHub(ApplicationContext applicationContext)
        {
            db = applicationContext;
        }

        public override Task OnConnectedAsync()
        {
            var user = db.HubUsers
                .Include(u => u.HubGroups)
                .SingleOrDefault(u => u.Name == Context.User.Identity.Name);

            if (user == null)
            {
                user = new HubUser()
                {
                    Name = Context.User.Identity.Name
                };
                db.HubUsers.Add(user);
                db.SaveChanges();
            }
            else
            {
                foreach (var item in user.HubGroups)
                {
                    Groups.AddToGroupAsync(Context.ConnectionId, item.Name);
                }
            }
            return base.OnConnectedAsync();
        }

        public async Task SendMessage(string groupName, string user, string message)
        {
            SaveMessage(Int32.Parse(groupName.Substring(groupName.IndexOf("-") + 1)), user, message);
            await Clients.Group(groupName).SendAsync("ReceiveMessage", user, message);
        }

        public async Task SendRemovalWish(string queueId)
        {
            Console.WriteLine("Wish was sent");
            await Clients.All.SendAsync("ReceiveRemovalWish", queueId);
        }

        // public async Task SendNotification(string groupName, string user, string message)
        // {
        //     // SaveMessage(Int32.Parse(groupName), user, message);
        //     await Clients.Group(groupName).SendAsync("ReceiveNotification", user, message);
        // }

        // public Task JoinGroup(string groupName)
        // {
        //     if (groupName == "")
        //         groupName = "user-" + db.Users.FirstOrDefault(u => u.Email == Context.User.Identity.Name).Id;
        //     var group = db.HubGroups.Include(hg => hg.HubUsers).FirstOrDefault(hg => hg.Name == groupName);
        //     if (group == null)
        //     {
        //         group = new HubGroup() { Name = groupName, HubUsers = new List<HubUser>() };
        //         db.HubGroups.Add(group);
        //     }
        //     var user = db.HubUsers.Find(Context.User.Identity.Name);
        //     if (user == null)
        //     {
        //         user = new HubUser() { Name = Context.User.Identity.Name };
        //         db.HubUsers.Add(user);
        //     }
        //     db.HubUsers.Attach(user);
        //     group.HubUsers.Add(user);
        //     db.SaveChanges();
        //     return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        // }

        // public Task LeaveGroup(string groupName)
        // {
        //     var group = db.HubGroups.Include(hg => hg.HubUsers).FirstOrDefault(hg => hg.Name == groupName);
        //     var user = db.HubUsers.Find(Context.User.Identity.Name);
        //     group.HubUsers.Remove(user);
        //     db.SaveChanges();
        //     return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        // }

        private int SaveMessage(int roomId, string userName, string messageBody)
        {
            Room room = db.Rooms.Find(roomId);
            Message newMessage = new Message() {
                Room = room,
                Sender = userName,
                Body = messageBody
            };
            db.Messages.Update(newMessage);
            return db.SaveChanges();
        }
    }
}