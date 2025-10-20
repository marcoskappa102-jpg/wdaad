using MMOServer.Models;

namespace MMOServer.Server
{
    public class LoginManager
    {
        private static LoginManager? instance;
        public static LoginManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new LoginManager();
                return instance;
            }
        }

        public LoginResponse Login(string username, string password)
        {
            var accountId = DatabaseHandler.Instance.ValidateLogin(username, password);
            
            if (accountId > 0)
            {
                var characters = DatabaseHandler.Instance.GetCharacters(accountId);
                
                return new LoginResponse
                {
                    success = true,
                    message = "Login successful",
                    accountId = accountId,
                    characters = characters
                };
            }
            
            return new LoginResponse
            {
                success = false,
                message = "Invalid username or password"
            };
        }

        public bool Register(string username, string password)
        {
            return DatabaseHandler.Instance.CreateAccount(username, password);
        }
    }

    public class LoginResponse
    {
        public bool success { get; set; }
        public string message { get; set; } = "";
        public int accountId { get; set; }
        public List<Character> characters { get; set; } = new List<Character>();
    }
}