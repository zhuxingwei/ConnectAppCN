using System.Collections.Generic;
using ConnectApp.Models.Model;

namespace ConnectApp.Models.ViewModel {
    public class PersonalScreenViewModel {
        public bool isLoggedIn;
        public string userId;
        public string userFullName;
        public Dictionary<string, User> userDict;
    }
}