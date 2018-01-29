using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using PlanningPoker.Data.Constants;
using PlanningPoker.Data.DTOs;

namespace PlanningPoker.Web
{
    public class Poker : HubWithPresence
    {
        public Poker(IUserTracker<Poker> userTracker) : base(userTracker)
        {
        }

        public override async Task OnConnectedAsync()
        {
            var user = await _userTracker.GetUser(Context.Connection);
            var usersOnline = await GetUsersOnline();
            var groupUsersOnline = usersOnline.Where(u => u.GroupId == user.GroupId);

            await Clients.Client(Context.ConnectionId).InvokeAsync(HubEvents.Connected, groupUsersOnline);

            await base.OnConnectedAsync();
        }

        public override async Task OnUsersLeft(UserDetailsDto[] users)
        {
            var user = await _userTracker.GetUser(Context.Connection);
            var usersOnline = await GetUsersOnline();
            var groupUsersOnline = usersOnline.Where(u => u.GroupId == user.GroupId);

            await Clients.Group(user.GroupId).InvokeAsync(HubEvents.Disconnected, groupUsersOnline);

            await base.OnUsersLeft(users);
        }

        public async Task Send(string message)
        {
            var pokerMessage = new PokerMessageDto(Context.Connection.ConnectionId, message);

            var user = await _userTracker.GetUser(Context.Connection);
            await Clients.Group(user.GroupId).InvokeAsync(HubEvents.Send, pokerMessage);
        }

        public async Task JoinUser(string userName)
        {
            var user = await _userTracker.GetUser(Context.Connection);
            user.Name = userName;

            await _userTracker.UpdateUser(Context.Connection, user);
            await Clients.Group(user.GroupId).InvokeAsync(HubEvents.JoinUser, user);
        }

        public async Task NewGame()
        {
            var user = await _userTracker.GetUser(Context.Connection);
            await Clients.Group(user.GroupId).InvokeAsync(HubEvents.NewGame);
        }

        public async Task ShowCards()
        {
            var user = await _userTracker.GetUser(Context.Connection);
            await Clients.Group(user.GroupId).InvokeAsync(HubEvents.ShowCards);
        }

        public async Task JoinGroup(GroupMessageDto groupMessage)
        {
            var user = await _userTracker.GetUser(Context.Connection);
            user.Name = groupMessage.PlayerName;

            var groupId = groupMessage?.GroupId ?? user.GroupId;
            user.GroupId = groupId;

            var usersOnline = await GetUsersOnline();
            var groupUsersOnline = usersOnline.Where(u => u.GroupId == user.GroupId);

            await _userTracker.UpdateUser(Context.Connection, user);
            await Groups.AddAsync(Context.ConnectionId, groupId);
            await Clients.Group(groupId).InvokeAsync(HubEvents.JoinGroup, groupUsersOnline);
            await Clients.Client(Context.ConnectionId).InvokeAsync(HubEvents.UpdateUser, user);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Clients.Group(groupName).InvokeAsync(HubEvents.LeaveGroup, groupName);
            await Groups.RemoveAsync(Context.ConnectionId, groupName);
        }
    }
}