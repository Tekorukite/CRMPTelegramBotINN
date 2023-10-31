namespace CRMPTelegramBotINN
{
    enum UserState
    {
        Base,
        Search,
        ExtendedSearch,
        Egrul
    }

    internal class UserManager
    {
        internal List<TelegramUser> Users { get; }
        public UserManager()
        {
            Users = new List<TelegramUser>();
        }
        public void AddUser(TelegramUser user) { Users.Add(user); }
        public TelegramUser? GetById(long id)
        {
            foreach (var user in Users)
            {
                if (user.Id == id) return user;
            }
            return null;
        }
    }
    internal class TelegramUser : Telegram.Bot.Types.User
    {
        internal Queue<string> Requests;
        internal UserState State { get; set; }
        public TelegramUser(Telegram.Bot.Types.User user)
        {
            this.Requests = new Queue<string>();
            this.Id = user.Id;
            this.IsBot = user.IsBot;
            this.FirstName = user.FirstName;
            this.State = UserState.Base;
        }

        public void AddRequest(string request)
        {
            if (this.Requests.Contains(request)) { return; }
            if (this.Requests.Count > 5) { this.Requests.Dequeue(); }
            Requests.Enqueue(request);
        }

        public bool RequestsContains(string pendingRequest)
        {
            foreach (string request in Requests)
            {
                if (request == pendingRequest)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
