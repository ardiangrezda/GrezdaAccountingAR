using Microsoft.EntityFrameworkCore;
using Accounting.Data;
using Accounting.Models;

namespace Accounting.Services
{
    public class SubjectService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public SubjectService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<Subject>> GetAllSubjectsAsync(bool includeInactive = false)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Subjects.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query
                .OrderBy(s => s.Code)
                .ToListAsync();
        }

        public async Task<Subject?> GetSubjectByIdAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Subjects
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<List<Subject>> SearchSubjectsAsync(string searchTerm, bool includeInactive = false)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllSubjectsAsync(includeInactive);

            using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Subjects.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query
                .Where(s => s.Code.Contains(searchTerm) ||
                           s.SubjectName.Contains(searchTerm) ||
                           s.Description.Contains(searchTerm) ||
                           (s.NUI != null && s.NUI.Contains(searchTerm)) ||
                           (s.FiscalNr != null && s.FiscalNr.Contains(searchTerm)) ||
                           (s.ContactPerson != null && s.ContactPerson.Contains(searchTerm)))
                .OrderBy(s => s.Code)
                .ToListAsync();
        }

        public async Task<List<Subject>> GetBuyersAsync(bool includeInactive = false)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Subjects.Where(s => s.IsBuyer);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query.OrderBy(s => s.SubjectName).ToListAsync();
        }

        public async Task<List<Subject>> GetSuppliersAsync(bool includeInactive = false)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Subjects.Where(s => s.IsFurnitor);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query.OrderBy(s => s.SubjectName).ToListAsync();
        }

        public async Task<Subject> CreateSubjectAsync(Subject subject)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            subject.CreatedAt = DateTime.UtcNow;
            subject.UpdatedAt = null;

            context.Subjects.Add(subject);
            await context.SaveChangesAsync();
            return subject;
        }

        public async Task<bool> UpdateSubjectAsync(Subject subject)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var existingSubject = await context.Subjects.FindAsync(subject.Id);
            if (existingSubject == null) return false;

            existingSubject.Code = subject.Code;
            existingSubject.Description = subject.Description;
            existingSubject.SubjectName = subject.SubjectName;
            existingSubject.NUI = subject.NUI;
            existingSubject.FiscalNr = subject.FiscalNr;
            existingSubject.Address = subject.Address;
            existingSubject.ContactPerson = subject.ContactPerson;
            existingSubject.Telephone = subject.Telephone;
            existingSubject.Email = subject.Email;
            existingSubject.Website = subject.Website;
            existingSubject.IsBuyer = subject.IsBuyer;
            existingSubject.IsFurnitor = subject.IsFurnitor;
            existingSubject.IsBuyerForCashOnly = subject.IsBuyerForCashOnly;
            existingSubject.IsSubjectWithoutVAT = subject.IsSubjectWithoutVAT;
            existingSubject.IsSubjectNoExcise = subject.IsSubjectNoExcise;
            existingSubject.IsFarmer = subject.IsFarmer;
            existingSubject.IsInternationalSubject = subject.IsInternationalSubject;
            existingSubject.IsActive = subject.IsActive;
            existingSubject.AccountsReceivable = subject.AccountsReceivable;
            existingSubject.AccountsPayable = subject.AccountsPayable;
            existingSubject.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSubjectAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var subject = await context.Subjects.FindAsync(id);
            if (subject == null) return false;

            subject.IsActive = false;
            subject.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreSubjectAsync(int id)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var subject = await context.Subjects.FindAsync(id);
            if (subject == null) return false;

            subject.IsActive = true;
            subject.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CheckCodeExistsAsync(string code, int? excludeId = null)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Subjects.Where(s => s.Code == code);

            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<List<Subject>> GetInternationalSubjectsAsync(bool includeInactive = false)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Subjects.Where(s => s.IsInternationalSubject);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query.OrderBy(s => s.SubjectName).ToListAsync();
        }

        public async Task<List<Subject>> GetVATExemptSubjectsAsync(bool includeInactive = false)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Subjects.Where(s => s.IsSubjectWithoutVAT);

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            return await query.OrderBy(s => s.SubjectName).ToListAsync();
        }

        public async Task<Subject?> GetSubjectByCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                return null;

            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Subjects
                .FirstOrDefaultAsync(s => s.Code == code && s.IsActive);
        }

        public async Task<List<Subject>> SearchBuyersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<Subject>();

            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Subjects
                .Where(s => s.IsBuyer && 
                           s.IsActive && 
                           (s.SubjectName.Contains(searchTerm) || 
                            s.Code.Contains(searchTerm)))
                .OrderBy(s => s.SubjectName)
                .Take(10)
                .ToListAsync();
        }
    }
}