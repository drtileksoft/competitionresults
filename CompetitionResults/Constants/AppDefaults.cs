namespace CompetitionResults.Constants
{
    public static class AppDefaults
    {
        public const string Domain = "bladethrowers.cz";
        public const string DefaultOrigin = "https://www." + Domain;
        public const string AdminEmail = "admin@" + Domain;
        public const string ManagerEmail = "manager@" + Domain;
        public const string UserEmail = "user@" + Domain;
        public const string GuestEmail = "guest@" + Domain;
        public const string DefaultPassword = "Xxxxxxxxxxx_1";
        public const string GuestPassword = "Guest_1";
        public const int MaxSignalRMessageSize = 1024000; // 1 MB
    }
}
