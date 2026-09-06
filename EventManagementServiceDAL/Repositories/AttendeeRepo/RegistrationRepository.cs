using System.Data;
using EventManagementServiceDAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace EventManagementServiceDAL.Repositories.AttendeeRepo;

public sealed class RegistrationRepository : IRegistrationRepository
{
   private const string ConfirmedStatus = "Confirmed";
   private const string CancelledStatus = "Cancelled";
   private const string WaitingStatus = "Waiting";
   private const string PromotedStatus = "Promoted";
   private const string PublishedStatus = "Published";

   private readonly EventManagementDbContext _db;
   private readonly ILogger<RegistrationRepository> _logger;

   public RegistrationRepository(EventManagementDbContext db, ILogger<RegistrationRepository> logger)
   {
       _db = db;
       _logger = logger;
   }

   public async Task<RegistrationResult> RegisterAttendeeWithCapacityCheckAsync(
       long eventId,
       long attendeeUserId,
       string source,
       CancellationToken cancellationToken = default)
   {
       await using IDbContextTransaction tx = await _db.Database
           .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

       try
       {
           var lockedEvent = await _db.Events
               .FromSqlRaw("SELECT * FROM dbo.Events WITH (UPDLOCK, HOLDLOCK, ROWLOCK) WHERE EventId = {0}", eventId)
               .AsTracking()
               .FirstOrDefaultAsync(cancellationToken);

           if (lockedEvent is null || lockedEvent.Status != PublishedStatus)
           {
               await tx.RollbackAsync(cancellationToken);
               return new RegistrationResult
               {
                   Outcome = RegistrationOutcome.EventNotAvailable,
                   Message = "Event is not available for registration."
               };
           }

           var duplicateReg = await _db.Registrations
               .AnyAsync(r => r.EventId == eventId
                           && r.AttendeeUserId == attendeeUserId
                           && r.RegistrationStatus == ConfirmedStatus, cancellationToken);
           if (duplicateReg)
           {
               await tx.RollbackAsync(cancellationToken);
               return new RegistrationResult
               {
                   Outcome = RegistrationOutcome.DuplicateRegistration,
                   Message = "Attendee is already registered for this event."
               };
           }

           var confirmedCount = await _db.Registrations
               .CountAsync(r => r.EventId == eventId && r.RegistrationStatus == ConfirmedStatus, cancellationToken);

           if (confirmedCount < lockedEvent.Capacity)
           {
               var reg = new Registration
               {
                   EventId = eventId,
                   AttendeeUserId = attendeeUserId,
                   RegistrationStatus = ConfirmedStatus,
                   Source = source,
                   RegisteredAtUtc = DateTime.UtcNow
               };
               _db.Registrations.Add(reg);
               await _db.SaveChangesAsync(cancellationToken);
               await tx.CommitAsync(cancellationToken);

               return new RegistrationResult
               {
                   Outcome = RegistrationOutcome.Confirmed,
                   RegistrationId = reg.RegistrationId
               };
           }

           var duplicateWait = await _db.WaitlistEntries
               .AnyAsync(w => w.EventId == eventId
                           && w.AttendeeUserId == attendeeUserId
                           && w.WaitlistStatus == WaitingStatus, cancellationToken);
           if (duplicateWait)
           {
               await tx.RollbackAsync(cancellationToken);
               return new RegistrationResult
               {
                   Outcome = RegistrationOutcome.DuplicateWaitlist,
                   Message = "Attendee already has an active waitlist entry for this event."
               };
           }

           var wait = new WaitlistEntry
           {
               EventId = eventId,
               AttendeeUserId = attendeeUserId,
               WaitlistStatus = WaitingStatus,
               QueuedAtUtc = DateTime.UtcNow
           };
           _db.WaitlistEntries.Add(wait);
           await _db.SaveChangesAsync(cancellationToken);

           var position = await _db.WaitlistEntries
               .CountAsync(w => w.EventId == eventId
                             && w.WaitlistStatus == WaitingStatus
                             && w.QueuedAtUtc <= wait.QueuedAtUtc, cancellationToken);

           await tx.CommitAsync(cancellationToken);
           return new RegistrationResult
           {
               Outcome = RegistrationOutcome.Waitlisted,
               WaitlistEntryId = wait.WaitlistEntryId,
               WaitlistPosition = position
           };
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Registration failed for event {EventId} attendee {AttendeeId}.", eventId, attendeeUserId);
           try { await tx.RollbackAsync(cancellationToken); } catch { }
           throw;
       }
   }

   public async Task<CancellationResult> CancelRegistrationAndPromoteAsync(
       long registrationId,
       long attendeeUserId,
       CancellationToken cancellationToken = default)
   {
       await using IDbContextTransaction tx = await _db.Database
           .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

       try
       {
           var reg = await _db.Registrations
               .FirstOrDefaultAsync(r => r.RegistrationId == registrationId, cancellationToken);

           if (reg is null || reg.AttendeeUserId != attendeeUserId)
           {
               await tx.RollbackAsync(cancellationToken);
               return new CancellationResult { Cancelled = false, Reason = "Registration not found." };
           }

           if (reg.RegistrationStatus != ConfirmedStatus)
           {
               await tx.RollbackAsync(cancellationToken);
               return new CancellationResult { Cancelled = false, Reason = "Registration is not in a cancellable state." };
           }

           var lockedEvent = await _db.Events
               .FromSqlRaw("SELECT * FROM dbo.Events WITH (UPDLOCK, HOLDLOCK, ROWLOCK) WHERE EventId = {0}", reg.EventId)
               .AsTracking()
               .FirstAsync(cancellationToken);

           if (lockedEvent.StartAtUtc <= DateTime.UtcNow)
           {
               await tx.RollbackAsync(cancellationToken);
               return new CancellationResult { Cancelled = false, Reason = "Event already started; cancellation not allowed." };
           }

           var now = DateTime.UtcNow;
           reg.RegistrationStatus = CancelledStatus;
           reg.CancelledAtUtc = now;
           reg.CancelledByUserId = attendeeUserId;

           await _db.SaveChangesAsync(cancellationToken);

           var confirmedCount = await _db.Registrations
               .CountAsync(r => r.EventId == reg.EventId && r.RegistrationStatus == ConfirmedStatus, cancellationToken);

           long? promotedRegId = null;
           long? promotedUserId = null;
           long? promotedFromWait = null;

           if (confirmedCount < lockedEvent.Capacity)
           {
               var nextWait = await _db.WaitlistEntries
                   .Where(w => w.EventId == reg.EventId && w.WaitlistStatus == WaitingStatus)
                   .OrderBy(w => w.QueuedAtUtc)
                   .ThenBy(w => w.WaitlistEntryId)
                   .FirstOrDefaultAsync(cancellationToken);

               if (nextWait is not null)
               {
                   var newReg = new Registration
                   {
                       EventId = reg.EventId,
                       AttendeeUserId = nextWait.AttendeeUserId,
                       RegistrationStatus = ConfirmedStatus,
                       Source = "WaitlistPromotion",
                       RegisteredAtUtc = now
                   };
                   _db.Registrations.Add(newReg);
                   await _db.SaveChangesAsync(cancellationToken);

                   nextWait.WaitlistStatus = PromotedStatus;
                   nextWait.PromotedAtUtc = now;
                   nextWait.PromotedToRegistrationId = newReg.RegistrationId;
                   await _db.SaveChangesAsync(cancellationToken);

                   promotedRegId = newReg.RegistrationId;
                   promotedUserId = newReg.AttendeeUserId;
                   promotedFromWait = nextWait.WaitlistEntryId;
               }
           }

           await tx.CommitAsync(cancellationToken);
           return new CancellationResult
           {
               Cancelled = true,
               PromotedRegistrationId = promotedRegId,
               PromotedAttendeeUserId = promotedUserId,
               PromotedFromWaitlistEntryId = promotedFromWait
           };
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Cancellation failed for registration {RegistrationId}.", registrationId);
           try { await tx.RollbackAsync(cancellationToken); } catch { }
           throw;
       }
   }

   public async Task<Registration?> GetRegistrationAsync(long registrationId, CancellationToken cancellationToken = default)
   {
       try
       {
return await _db.Registrations
                .AsNoTracking()
                .Include(r => r.Event)
                .Include(r => r.AttendeeUser)
                .FirstOrDefaultAsync(r => r.RegistrationId == registrationId, cancellationToken);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Failed to load registration {RegistrationId}.", registrationId);
           throw;
       }
   }

   public async Task<IReadOnlyList<Registration>> GetRegistrationsForAttendeeAsync(long attendeeUserId, CancellationToken cancellationToken = default)
   {
       try
       {
           return await _db.Registrations
               .AsNoTracking()
               .Include(r => r.Event)
               .Include(r => r.AttendanceRecord)
               .Where(r => r.AttendeeUserId == attendeeUserId)
               .OrderByDescending(r => r.RegisteredAtUtc)
               .ToListAsync(cancellationToken);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Failed to load registrations for attendee {AttendeeId}.", attendeeUserId);
           throw;
       }
   }

   public async Task<IReadOnlyList<WaitlistEntry>> GetActiveWaitlistForAttendeeAsync(long attendeeUserId, CancellationToken cancellationToken = default)
   {
       try
       {
           return await _db.WaitlistEntries
               .AsNoTracking()
               .Include(w => w.Event)
               .Where(w => w.AttendeeUserId == attendeeUserId && w.WaitlistStatus == WaitingStatus)
               .OrderBy(w => w.QueuedAtUtc)
               .ToListAsync(cancellationToken);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Failed to load waitlist entries for attendee {AttendeeId}.", attendeeUserId);
           throw;
       }
   }

   public async Task<IReadOnlyList<Registration>> GetRegistrationsForEventAsync(long eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.Registrations
                .AsNoTracking()
                .Include(r => r.AttendeeUser)
                .Include(r => r.AttendanceRecord)
                .Where(r => r.EventId == eventId && r.RegistrationStatus == ConfirmedStatus)
                .OrderBy(r => r.RegisteredAtUtc)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load registrations for event {EventId}.", eventId);
            throw;
        }
    }

    public async Task<IReadOnlyList<WaitlistEntry>> GetWaitlistForEventAsync(long eventId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.WaitlistEntries
                .AsNoTracking()
                .Include(w => w.AttendeeUser)
                .Where(w => w.EventId == eventId && w.WaitlistStatus == WaitingStatus)
                .OrderBy(w => w.QueuedAtUtc)
                .ThenBy(w => w.WaitlistEntryId)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load waitlist for event {EventId}.", eventId);
            throw;
        }
    }

    public async Task<WaitlistEntry?> GetWaitlistEntryAsync(long waitlistEntryId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.WaitlistEntries
                .AsNoTracking()
                .Include(w => w.Event)
                .Include(w => w.AttendeeUser)
                .FirstOrDefaultAsync(w => w.WaitlistEntryId == waitlistEntryId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load waitlist entry {WaitlistEntryId}.", waitlistEntryId);
            throw;
        }
    }

    public async Task<int> GetWaitlistPositionAsync(long waitlistEntryId, CancellationToken cancellationToken = default)
   {
       try
       {
           var entry = await _db.WaitlistEntries.AsNoTracking()
               .FirstOrDefaultAsync(w => w.WaitlistEntryId == waitlistEntryId, cancellationToken);
           if (entry is null || entry.WaitlistStatus != WaitingStatus)
           {
               return 0;
           }

           return await _db.WaitlistEntries
               .CountAsync(w => w.EventId == entry.EventId
                             && w.WaitlistStatus == WaitingStatus
                             && w.QueuedAtUtc <= entry.QueuedAtUtc, cancellationToken);
       }
       catch (Exception ex)
       {
           _logger.LogError(ex, "Failed to compute waitlist position for entry {WaitlistEntryId}.", waitlistEntryId);
           throw;
       }
   }
}
