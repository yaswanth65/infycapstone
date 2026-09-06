namespace EventManagementSystemServiceLayer.Constants
{
   public static class AuditActionTypes
   {
       public const string UserCreated = "UserCreated";
       public const string UserUpdated = "UserUpdated";
       public const string UserDeactivated = "UserDeactivated";
       public const string RoleAssigned = "RoleAssigned";

       public const string Login = "Login";
       public const string Logout = "Logout";
       public const string LoginFailed = "LoginFailed";

       public const string EventCreated = "EventCreated";
       public const string EventPublished = "EventPublished";
       public const string EventCancelled = "EventCancelled";
       public const string EventClosed = "EventClosed";

       public const string RegistrationCreated = "RegistrationCreated";
       public const string RegistrationCancelled = "RegistrationCancelled";

       public const string RegistrationRequestCreated = "RegistrationRequestCreated";
       public const string RegistrationRequestAccepted = "RegistrationRequestAccepted";
       public const string RegistrationRequestRejected = "RegistrationRequestRejected";

       public const string WaitlistEntryAdded = "WaitlistEntryAdded";
       public const string WaitlistEntryPromoted = "WaitlistEntryPromoted";
       public const string WaitlistEntryRemoved = "WaitlistEntryRemoved";

       public const string AttendanceRecorded = "AttendanceRecorded";
       public const string AttendanceCorrected = "AttendanceCorrected";

       public const string AuditLogExported = "AuditLogExported";
       public const string ReportGenerated = "ReportGenerated";
   }
}
