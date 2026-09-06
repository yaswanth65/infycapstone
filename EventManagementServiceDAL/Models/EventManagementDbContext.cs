using Microsoft.EntityFrameworkCore;

namespace EventManagementServiceDAL.Models;

public partial class EventManagementDbContext : DbContext
{
   public EventManagementDbContext()
   {
   }

   public EventManagementDbContext(DbContextOptions<EventManagementDbContext> options)
       : base(options)
   {
   }

   public virtual DbSet<AttendanceRecord> AttendanceRecords { get; set; }
   public virtual DbSet<AuditRecord> AuditRecords { get; set; }
   public virtual DbSet<Event> Events { get; set; }
   public virtual DbSet<EventStatusHistory> EventStatusHistories { get; set; }
   public virtual DbSet<Notification> Notifications { get; set; }
   public virtual DbSet<Registration> Registrations { get; set; }
   public virtual DbSet<RegistrationRequest> RegistrationRequests { get; set; }
   public virtual DbSet<Role> Roles { get; set; }
   public virtual DbSet<User> Users { get; set; }
   public virtual DbSet<WaitlistEntry> WaitlistEntries { get; set; }

   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
   {
       if (!optionsBuilder.IsConfigured)
       {
           optionsBuilder.UseSqlServer("Data Source=localhost;Initial Catalog=EventManagementDB;Integrated Security=true;TrustServerCertificate=true");
       }
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       modelBuilder.Entity<AttendanceRecord>(entity =>
       {
           entity.HasIndex(e => e.RegistrationId, "UQ__Attendan__6EF58811CEEF5B67").IsUnique();

           entity.Property(e => e.AttendanceStatus)
               .HasMaxLength(20)
               .IsUnicode(false);
           entity.Property(e => e.CorrectedAtUtc).HasPrecision(0);
           entity.Property(e => e.CorrectionReason).HasMaxLength(400);
           entity.Property(e => e.FinalizedAtUtc).HasPrecision(0);
           entity.Property(e => e.RecordedAtUtc)
               .HasPrecision(0)
               .HasDefaultValueSql("(sysutcdatetime())");
           entity.Property(e => e.RevisionNo).HasDefaultValue(1);

           entity.HasOne(d => d.CorrectedByUser).WithMany(p => p.AttendanceRecordCorrectedByUsers).HasForeignKey(d => d.CorrectedByUserId);
           entity.HasOne(d => d.RecordedByUser).WithMany(p => p.AttendanceRecordRecordedByUsers)
               .HasForeignKey(d => d.RecordedByUserId)
               .OnDelete(DeleteBehavior.ClientSetNull);
           entity.HasOne(d => d.Registration).WithOne(p => p.AttendanceRecord)
               .HasForeignKey<AttendanceRecord>(d => d.RegistrationId)
               .OnDelete(DeleteBehavior.ClientSetNull);
       });

       modelBuilder.Entity<AuditRecord>(entity =>
       {
           entity.HasIndex(e => new { e.ActorUserId, e.CreatedAtUtc }, "IX_AuditRecords_ActorUserId").IsDescending(false, true);
           entity.HasIndex(e => e.CreatedAtUtc, "IX_AuditRecords_CreatedAtUtc").IsDescending();
           entity.HasIndex(e => new { e.EventId, e.CreatedAtUtc }, "IX_AuditRecords_EventId").IsDescending(false, true);

           entity.Property(e => e.ActionType)
               .HasMaxLength(40)
               .IsUnicode(false);
           entity.Property(e => e.CreatedAtUtc)
               .HasPrecision(0)
               .HasDefaultValueSql("(sysutcdatetime())");
           entity.Property(e => e.IpAddress).HasMaxLength(45);
           entity.Property(e => e.Outcome)
               .HasMaxLength(20)
               .IsUnicode(false);
           entity.Property(e => e.TargetEntity)
               .HasMaxLength(50)
               .IsUnicode(false);

           entity.HasOne(d => d.ActorUser).WithMany(p => p.AuditRecords).HasForeignKey(d => d.ActorUserId);
           entity.HasOne(d => d.Event).WithMany(p => p.AuditRecords).HasForeignKey(d => d.EventId);
       });

       modelBuilder.Entity<Event>(entity =>
       {
           entity.HasIndex(e => e.EndAtUtc, "IX_Events_EndAtUtc");
           entity.HasIndex(e => new { e.OrganizerUserId, e.Status }, "IX_Events_OrganizerUserId");
           entity.HasIndex(e => e.StartAtUtc, "IX_Events_StartAtUtc");
           entity.HasIndex(e => new { e.Status, e.StartAtUtc }, "IX_Events_Status_StartAtUtc");

           entity.Property(e => e.CancelledAtUtc).HasPrecision(0);
           entity.Property(e => e.ClosedAtUtc).HasPrecision(0);
           entity.Property(e => e.CreatedAtUtc)
               .HasPrecision(0)
               .HasDefaultValueSql("(sysutcdatetime())");
           entity.Property(e => e.EndAtUtc).HasPrecision(0);
           entity.Property(e => e.PublishedAtUtc).HasPrecision(0);
           entity.Property(e => e.RegistrationCloseAtUtc).HasPrecision(0);
           entity.Property(e => e.RegistrationOpenAtUtc).HasPrecision(0);
           entity.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();
           entity.Property(e => e.StartAtUtc).HasPrecision(0);
           entity.Property(e => e.Status)
               .HasMaxLength(20)
               .IsUnicode(false)
               .HasDefaultValue("Draft");
           entity.Property(e => e.Title).HasMaxLength(200);
           entity.Property(e => e.UpdatedAtUtc).HasPrecision(0);
           entity.Property(e => e.Venue).HasMaxLength(250);

           entity.HasOne(d => d.OrganizerUser).WithMany(p => p.Events)
               .HasForeignKey(d => d.OrganizerUserId)
               .OnDelete(DeleteBehavior.ClientSetNull);
       });

       modelBuilder.Entity<EventStatusHistory>(entity =>
       {
           entity.ToTable("EventStatusHistory");
           entity.HasIndex(e => e.ChangedAtUtc, "IX_EventStatusHistory_ChangedAtUtc").IsDescending();
           entity.HasIndex(e => e.EventId, "IX_EventStatusHistory_EventId");

           entity.Property(e => e.ChangedAtUtc)
               .HasPrecision(0)
               .HasDefaultValueSql("(sysutcdatetime())");
           entity.Property(e => e.FromStatus)
               .HasMaxLength(20)
               .IsUnicode(false);
           entity.Property(e => e.Remarks).HasMaxLength(500);
           entity.Property(e => e.ToStatus)
               .HasMaxLength(20)
               .IsUnicode(false);

           entity.HasOne(d => d.ChangedByUser).WithMany(p => p.EventStatusHistories)
               .HasForeignKey(d => d.ChangedByUserId)
               .OnDelete(DeleteBehavior.ClientSetNull);
           entity.HasOne(d => d.Event).WithMany(p => p.EventStatusHistories)
               .HasForeignKey(d => d.EventId)
               .OnDelete(DeleteBehavior.ClientSetNull);
       });

       modelBuilder.Entity<Notification>(entity =>
       {
           entity.HasIndex(e => new { e.RecipientUserId, e.DeliveryStatus, e.CreatedAtUtc }, "IX_Notifications_RecipientUserId_DeliveryStatus_CreatedAtUtc").IsDescending(false, false, true);

           entity.Property(e => e.CreatedAtUtc)
               .HasPrecision(0)
               .HasDefaultValueSql("(sysutcdatetime())");
           entity.Property(e => e.DeliveryStatus)
               .HasMaxLength(20)
               .IsUnicode(false)
               .HasDefaultValue("Pending");
           entity.Property(e => e.Message).HasMaxLength(1000);
           entity.Property(e => e.NotificationType)
               .HasMaxLength(40)
               .IsUnicode(false);
           entity.Property(e => e.ReadAtUtc).HasPrecision(0);
           entity.Property(e => e.ScheduledAtUtc).HasPrecision(0);
           entity.Property(e => e.SentAtUtc).HasPrecision(0);
           entity.Property(e => e.Title).HasMaxLength(200);

           entity.HasOne(d => d.RecipientUser).WithMany(p => p.Notifications)
               .HasForeignKey(d => d.RecipientUserId)
               .OnDelete(DeleteBehavior.ClientSetNull);
           entity.HasOne(d => d.RelatedEvent).WithMany(p => p.Notifications).HasForeignKey(d => d.RelatedEventId);
           entity.HasOne(d => d.RelatedRegistration).WithMany(p => p.Notifications).HasForeignKey(d => d.RelatedRegistrationId);
           entity.HasOne(d => d.RelatedRequest).WithMany(p => p.Notifications).HasForeignKey(d => d.RelatedRequestId);
       });

       modelBuilder.Entity<Registration>(entity =>
       {
           entity.HasIndex(e => new { e.AttendeeUserId, e.RegistrationStatus }, "IX_Registrations_AttendeeUserId");
           entity.HasIndex(e => new { e.EventId, e.RegistrationStatus }, "IX_Registrations_EventId_Status");
           entity.HasIndex(e => e.RegistrationStatus, "IX_Registrations_RegistrationStatus");
           entity.HasIndex(e => new { e.EventId, e.AttendeeUserId }, "UX_Registrations_Event_Attendee_Confirmed")
               .IsUnique()
               .HasFilter("([RegistrationStatus]='Confirmed')");

           entity.Property(e => e.CancelReason).HasMaxLength(300);
           entity.Property(e => e.CancelledAtUtc).HasPrecision(0);
           entity.Property(e => e.RegisteredAtUtc)
               .HasPrecision(0)
               .HasDefaultValueSql("(sysutcdatetime())");
           entity.Property(e => e.RegistrationStatus)
               .HasMaxLength(20)
               .IsUnicode(false)
               .HasDefaultValue("Confirmed");
           entity.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();
           entity.Property(e => e.Source)
               .HasMaxLength(20)
               .IsUnicode(false);

           entity.HasOne(d => d.AttendeeUser).WithMany(p => p.RegistrationAttendeeUsers)
               .HasForeignKey(d => d.AttendeeUserId)
               .OnDelete(DeleteBehavior.ClientSetNull);
           entity.HasOne(d => d.CancelledByUser).WithMany(p => p.RegistrationCancelledByUsers).HasForeignKey(d => d.CancelledByUserId);
           entity.HasOne(d => d.Event).WithMany(p => p.Registrations)
               .HasForeignKey(d => d.EventId)
               .OnDelete(DeleteBehavior.ClientSetNull);
           entity.HasOne(d => d.RegistrationRequest).WithMany(p => p.Registrations).HasForeignKey(d => d.RegistrationRequestId);
       });

       modelBuilder.Entity<RegistrationRequest>(entity =>
       {
           entity.HasIndex(e => new { e.EventId, e.RequestStatus }, "IX_RegistrationRequests_EventId");
           entity.HasIndex(e => e.RequestStatus, "IX_RegistrationRequests_RequestStatus");
           entity.HasIndex(e => e.LinkedRegistrationId, "UQ__Registra__DC196ACBDB11336B").IsUnique();
           entity.HasIndex(e => new { e.EventId, e.AttendeeUserId }, "UX_RegistrationRequests_Event_Attendee_Pending")
               .IsUnique()
               .HasFilter("([RequestStatus]='Pending')");

           entity.Property(e => e.RequestStatus)
               .HasMaxLength(20)
               .IsUnicode(false);
           entity.Property(e => e.RequestType)
               .HasMaxLength(20)
               .IsUnicode(false)
               .HasDefaultValue("OnBehalf");
           entity.Property(e => e.RequestedAtUtc)
               .HasPrecision(0)
               .HasDefaultValueSql("(sysutcdatetime())");
           entity.Property(e => e.RespondedAtUtc).HasPrecision(0);
           entity.Property(e => e.ResponseComment).HasMaxLength(500);

           entity.HasOne(d => d.AttendeeUser).WithMany(p => p.RegistrationRequestAttendeeUsers)
               .HasForeignKey(d => d.AttendeeUserId)
               .OnDelete(DeleteBehavior.ClientSetNull);
           entity.HasOne(d => d.Event).WithMany(p => p.RegistrationRequests)
               .HasForeignKey(d => d.EventId)
               .OnDelete(DeleteBehavior.ClientSetNull);
           entity.HasOne(d => d.RequestedByUser).WithMany(p => p.RegistrationRequestRequestedByUsers)
               .HasForeignKey(d => d.RequestedByUserId)
               .OnDelete(DeleteBehavior.ClientSetNull);
       });

       modelBuilder.Entity<Role>(entity =>
       {
           entity.HasIndex(e => e.RoleName, "UQ_Roles_RoleName").IsUnique();
           entity.Property(e => e.CreatedAtUtc)
               .HasPrecision(0)
               .HasDefaultValueSql("(sysutcdatetime())");
           entity.Property(e => e.IsActive).HasDefaultValue(true);
           entity.Property(e => e.RoleName).HasMaxLength(50);
       });

       modelBuilder.Entity<User>(entity =>
       {
           entity.HasIndex(e => new { e.RoleId, e.IsActive }, "IX_Users_RoleId_IsActive");
           entity.HasIndex(e => e.UserName, "IX_Users_UserName");
           entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();
           entity.HasIndex(e => e.UserName, "UQ_Users_UserName").IsUnique();
           entity.HasIndex(e => e.Email, "UX_Users_Email_Active")
               .IsUnique()
               .HasFilter("([IsActive]=(1))");

           entity.Property(e => e.CreatedAtUtc)
               .HasPrecision(0)
               .HasDefaultValueSql("(sysutcdatetime())");
           entity.Property(e => e.DeactivatedAtUtc).HasPrecision(0);
           entity.Property(e => e.DisplayName).HasMaxLength(150);
           entity.Property(e => e.Email).HasMaxLength(256);
           entity.Property(e => e.IsActive).HasDefaultValue(true);
           entity.Property(e => e.PasswordHash).HasMaxLength(256);
           entity.Property(e => e.PasswordSalt).HasMaxLength(128);
           entity.Property(e => e.PhoneNumber).HasMaxLength(20);
           entity.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();
           entity.Property(e => e.UpdatedAtUtc).HasPrecision(0);
           entity.Property(e => e.UserName).HasMaxLength(100);

           entity.HasOne(d => d.Role).WithMany(p => p.Users)
               .HasForeignKey(d => d.RoleId)
               .OnDelete(DeleteBehavior.ClientSetNull);
       });

       modelBuilder.Entity<WaitlistEntry>(entity =>
       {
           entity.HasIndex(e => new { e.EventId, e.WaitlistStatus }, "IX_WaitlistEntries_EventId_Status");
           entity.HasIndex(e => new { e.EventId, e.AttendeeUserId }, "UX_WaitlistEntries_Event_Attendee_Waiting")
               .IsUnique()
               .HasFilter("([WaitlistStatus]='Waiting')");

           entity.Property(e => e.PromotedAtUtc).HasPrecision(0);
           entity.Property(e => e.QueuedAtUtc)
               .HasPrecision(0)
               .HasDefaultValueSql("(sysutcdatetime())");
           entity.Property(e => e.RemovedAtUtc).HasPrecision(0);
           entity.Property(e => e.RemovedReason).HasMaxLength(300);
           entity.Property(e => e.WaitlistStatus)
               .HasMaxLength(20)
               .IsUnicode(false)
               .HasDefaultValue("Waiting");

           entity.HasOne(d => d.AttendeeUser).WithMany(p => p.WaitlistEntries)
               .HasForeignKey(d => d.AttendeeUserId)
               .OnDelete(DeleteBehavior.ClientSetNull);
           entity.HasOne(d => d.Event).WithMany(p => p.WaitlistEntries)
               .HasForeignKey(d => d.EventId)
               .OnDelete(DeleteBehavior.ClientSetNull);
           entity.HasOne(d => d.PromotedToRegistration).WithMany(p => p.WaitlistEntries).HasForeignKey(d => d.PromotedToRegistrationId);
       });

       OnModelCreatingPartial(modelBuilder);
   }

   partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
