using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Dtos;
using SchoolAdmission.Api.Exceptions;
using SchoolAdmission.Api.Models.Entities;
using SchoolAdmission.Api.Models.Enums;

namespace SchoolAdmission.Api.Services;

public class AdmissionService : IAdmissionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdmissionService> _logger;

    // In-memory fallback concurrency locks for non-relational test environments
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ClassLocks = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> HoldLocks = new();

    public AdmissionService(
        ApplicationDbContext dbContext,
        IConfiguration configuration,
        ILogger<AdmissionService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    private int GetHoldDurationSeconds()
    {
        var envValue = _configuration["HOLD_DURATION_SECONDS"];
        if (int.TryParse(envValue, out var seconds) && seconds > 0)
        {
            return seconds;
        }
        return 600; // default 10 minutes (600 seconds)
    }

    public async Task<CreateClassResponse> CreateClassAsync(CreateClassRequest request)
    {
        var id = "c_" + Guid.NewGuid().ToString("N")[..8];
        var schoolClass = new SchoolClass
        {
            Id = id,
            Name = request.Name.Trim(),
            TotalSeats = request.TotalSeats,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Classes.Add(schoolClass);
        await _dbContext.SaveChangesAsync();

        return new CreateClassResponse(schoolClass.Id, schoolClass.Name, schoolClass.TotalSeats);
    }

    public async Task<IEnumerable<ClassDetailResponse>> GetAllClassesAsync()
    {
        var now = DateTime.UtcNow;
        var classes = await _dbContext.Classes
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();

        var result = new List<ClassDetailResponse>();

        foreach (var c in classes)
        {
            var confirmedSeats = await _dbContext.Holds
                .CountAsync(h => h.ClassId == c.Id && h.Status == HoldStatus.CONFIRMED);

            var heldSeats = await _dbContext.Holds
                .CountAsync(h => h.ClassId == c.Id && h.Status == HoldStatus.HELD && h.ExpiresAt > now);

            var availableSeats = Math.Max(0, c.TotalSeats - (confirmedSeats + heldSeats));

            result.Add(new ClassDetailResponse(
                c.Id,
                c.Name,
                c.TotalSeats,
                heldSeats,
                confirmedSeats,
                availableSeats
            ));
        }

        return result;
    }

    public async Task<ClassDetailResponse> GetClassByIdAsync(string classId)
    {
        var now = DateTime.UtcNow;
        var c = await _dbContext.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == classId);

        if (c == null)
        {
            throw new NotFoundException($"Class with ID '{classId}' not found.");
        }

        var confirmedSeats = await _dbContext.Holds
            .CountAsync(h => h.ClassId == c.Id && h.Status == HoldStatus.CONFIRMED);

        var heldSeats = await _dbContext.Holds
            .CountAsync(h => h.ClassId == c.Id && h.Status == HoldStatus.HELD && h.ExpiresAt > now);

        var availableSeats = Math.Max(0, c.TotalSeats - (confirmedSeats + heldSeats));

        return new ClassDetailResponse(
            c.Id,
            c.Name,
            c.TotalSeats,
            heldSeats,
            confirmedSeats,
            availableSeats
        );
    }

    public async Task<CreateHoldResponse> CreateHoldAsync(string classId, string studentId)
    {
        var isPostgres = _dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
        var sem = !isPostgres ? ClassLocks.GetOrAdd(classId, _ => new SemaphoreSlim(1, 1)) : null;

        if (sem != null)
        {
            await sem.WaitAsync();
        }

        try
        {
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            if (isPostgres)
            {
                transaction = await _dbContext.Database.BeginTransactionAsync();
            }

            try
            {
                var now = DateTime.UtcNow;
                var holdDuration = GetHoldDurationSeconds();

                // 1. Pessimistic lock row in PostgreSQL or query entity in-memory
                SchoolClass? schoolClass;
                if (isPostgres)
                {
                    schoolClass = await _dbContext.Classes
                        .FromSqlInterpolated($"SELECT * FROM classes WHERE id = {classId} FOR UPDATE")
                        .FirstOrDefaultAsync();
                }
                else
                {
                    schoolClass = await _dbContext.Classes.FirstOrDefaultAsync(c => c.Id == classId);
                }

                if (schoolClass == null)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    throw new NotFoundException($"Class with ID '{classId}' not found.");
                }

                // 2. Rule 3: A student cannot have more than one active hold or confirmed admission in the same class
                var existingActive = await _dbContext.Holds
                    .AnyAsync(h => h.ClassId == classId &&
                                   h.StudentId == studentId &&
                                   (h.Status == HoldStatus.CONFIRMED || (h.Status == HoldStatus.HELD && h.ExpiresAt > now)));

                if (existingActive)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    throw new ConflictException("You already have an active hold or confirmed admission in this class.");
                }

                // 3. Rule 1: Under no circumstance can held + confirmed exceed totalSeats
                var confirmedSeats = await _dbContext.Holds
                    .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.CONFIRMED);

                var heldSeats = await _dbContext.Holds
                    .CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.HELD && h.ExpiresAt > now);

                var availableSeats = schoolClass.TotalSeats - (confirmedSeats + heldSeats);

                if (availableSeats <= 0)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    throw new ConflictException("No seats available in this class.");
                }

                // 4. Create new hold
                var holdId = "h_" + Guid.NewGuid().ToString("N")[..8];
                var expiresAt = now.AddSeconds(holdDuration);

                var newHold = new SeatHold
                {
                    Id = holdId,
                    ClassId = classId,
                    StudentId = studentId,
                    Status = HoldStatus.HELD,
                    ExpiresAt = expiresAt,
                    CreatedAt = now
                };

                _dbContext.Holds.Add(newHold);
                await _dbContext.SaveChangesAsync();

                if (transaction != null)
                {
                    await transaction.CommitAsync();
                }

                return new CreateHoldResponse(
                    newHold.Id,
                    newHold.ClassId,
                    newHold.StudentId,
                    newHold.Status.ToString(),
                    newHold.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                );
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw;
            }
        }
        finally
        {
            sem?.Release();
        }
    }

    public async Task<HoldStatusResponse> GetHoldByIdAsync(string holdId)
    {
        var now = DateTime.UtcNow;
        var hold = await _dbContext.Holds
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == holdId);

        if (hold == null)
        {
            throw new NotFoundException($"Hold with ID '{holdId}' not found.");
        }

        var status = hold.Status.ToString();
        if (hold.Status == HoldStatus.HELD && hold.ExpiresAt <= now)
        {
            status = HoldStatus.EXPIRED.ToString();
        }

        return new HoldStatusResponse(
            hold.Id,
            hold.ClassId,
            status,
            hold.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        );
    }

    public async Task<ConfirmHoldResponse> ConfirmHoldAsync(string holdId, string studentId)
    {
        var isPostgres = _dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
        var sem = !isPostgres ? HoldLocks.GetOrAdd(holdId, _ => new SemaphoreSlim(1, 1)) : null;

        if (sem != null)
        {
            await sem.WaitAsync();
        }

        try
        {
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            if (isPostgres)
            {
                transaction = await _dbContext.Database.BeginTransactionAsync();
            }

            try
            {
                var now = DateTime.UtcNow;

                SeatHold? hold;
                if (isPostgres)
                {
                    hold = await _dbContext.Holds
                        .FromSqlInterpolated($"SELECT * FROM holds WHERE id = {holdId} FOR UPDATE")
                        .FirstOrDefaultAsync();
                }
                else
                {
                    hold = await _dbContext.Holds.FirstOrDefaultAsync(h => h.Id == holdId);
                }

                if (hold == null)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    throw new NotFoundException($"Hold with ID '{holdId}' not found.");
                }

                // Rule 4: Only the student themselves can confirm their own hold
                if (hold.StudentId != studentId)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    throw new ForbiddenException("You can only confirm your own hold.");
                }

                // Rule 5: Idempotency - If confirm is called multiple times, confirmedSeats only increases once
                if (hold.Status == HoldStatus.CONFIRMED)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    return new ConfirmHoldResponse(hold.Id, HoldStatus.CONFIRMED.ToString());
                }

                // Rule 4: Only unexpired HELD holds can be confirmed
                if (hold.Status == HoldStatus.EXPIRED || (hold.Status == HoldStatus.HELD && hold.ExpiresAt <= now))
                {
                    hold.Status = HoldStatus.EXPIRED;
                    await _dbContext.SaveChangesAsync();
                    if (transaction != null) await transaction.CommitAsync();
                    throw new BadRequestException("Hold has expired and cannot be confirmed.");
                }

                if (hold.Status == HoldStatus.CANCELLED)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    throw new BadRequestException("Cancelled hold cannot be confirmed.");
                }

                hold.Status = HoldStatus.CONFIRMED;
                hold.ConfirmedAt = now;

                await _dbContext.SaveChangesAsync();
                if (transaction != null) await transaction.CommitAsync();

                return new ConfirmHoldResponse(hold.Id, HoldStatus.CONFIRMED.ToString());
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw;
            }
        }
        finally
        {
            sem?.Release();
        }
    }

    public async Task CancelHoldAsync(string holdId, string studentId)
    {
        var isPostgres = _dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
        var sem = !isPostgres ? HoldLocks.GetOrAdd(holdId, _ => new SemaphoreSlim(1, 1)) : null;

        if (sem != null)
        {
            await sem.WaitAsync();
        }

        try
        {
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            if (isPostgres)
            {
                transaction = await _dbContext.Database.BeginTransactionAsync();
            }

            try
            {
                SeatHold? hold;
                if (isPostgres)
                {
                    hold = await _dbContext.Holds
                        .FromSqlInterpolated($"SELECT * FROM holds WHERE id = {holdId} FOR UPDATE")
                        .FirstOrDefaultAsync();
                }
                else
                {
                    hold = await _dbContext.Holds.FirstOrDefaultAsync(h => h.Id == holdId);
                }

                if (hold == null)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    throw new NotFoundException($"Hold with ID '{holdId}' not found.");
                }

                if (hold.StudentId != studentId)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    throw new ForbiddenException("You can only cancel your own hold.");
                }

                if (hold.Status == HoldStatus.CONFIRMED)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    throw new BadRequestException("Confirmed admissions cannot be cancelled.");
                }

                if (hold.Status == HoldStatus.CANCELLED)
                {
                    if (transaction != null) await transaction.RollbackAsync();
                    return;
                }

                hold.Status = HoldStatus.CANCELLED;
                await _dbContext.SaveChangesAsync();
                if (transaction != null) await transaction.CommitAsync();
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                }
                throw;
            }
        }
        finally
        {
            sem?.Release();
        }
    }

    public async Task<IEnumerable<StudentHoldItemResponse>> GetStudentHoldsAsync(string studentId)
    {
        var now = DateTime.UtcNow;
        var holds = await _dbContext.Holds
            .Include(h => h.SchoolClass)
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        return holds.Select(h =>
        {
            var status = h.Status.ToString();
            if (h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
            {
                status = HoldStatus.EXPIRED.ToString();
            }

            return new StudentHoldItemResponse(
                h.Id,
                h.ClassId,
                h.SchoolClass?.Name ?? "Unknown Class",
                status,
                h.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            );
        });
    }

    public async Task CleanExpiredHoldsAsync()
    {
        var now = DateTime.UtcNow;
        try
        {
            var expiredHolds = await _dbContext.Holds
                .Where(h => h.Status == HoldStatus.HELD && h.ExpiresAt <= now)
                .ToListAsync();

            if (expiredHolds.Any())
            {
                foreach (var hold in expiredHolds)
                {
                    hold.Status = HoldStatus.EXPIRED;
                }
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Background sweeper marked {Count} expired holds.", expiredHolds.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while cleaning expired holds.");
        }
    }
}
