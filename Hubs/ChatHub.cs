using Cua.Models;
using Microsoft.AspNetCore.SignalR;
using System;
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

        public async Task SendMessage(string groupName, string user, string message)
        {
            SaveMessage(Int32.Parse(groupName), user, message);
            await Clients.Group(groupName).SendAsync("ReceiveMessage", user, message);
        }

        public Task JoinGroup(string groupName)
        {
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

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