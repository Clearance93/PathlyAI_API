using Microsoft.EntityFrameworkCore;
using Pathly_Data;
using Pathly_Models;
using Pathly_Interfaces;

namespace PathlyRepository
{
    public class ExtractedAcademicRecordRepository : GenericRepository<ExtractedAcademicRecord>, ExtractedAcademicRecordInterfaceRepository
    {
        private readonly ApplicationDbContext _context;

        public ExtractedAcademicRecordRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<ExtractedAcademicRecord?> GetByIdWithHistoryAsync(Guid id)
        {
            return await _context.ExtractedAcademicRecords
                .Include(r => r.Subjects)
                .Include(r => r.AcademicPeriods)
                    .ThenInclude(p => p.Subjects)
                .FirstOrDefaultAsync(r => r.ExtractionAcademicRecordId == id);
        }

        public async Task<ExtractedAcademicRecord?> GetLatestForUserAsync(string applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                return null;
            }

            return await _context.ExtractedAcademicRecords
                .Include(r => r.Subjects)
                .Include(r => r.AcademicPeriods)
                    .ThenInclude(p => p.Subjects)
                .Where(r => r.ApplicationUserId == applicationUserId)
                .OrderByDescending(r => r.ExtractedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ExtractedAcademicRecord>> GetAllForUserAsync(string applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                return new List<ExtractedAcademicRecord>();
            }

            return await _context.ExtractedAcademicRecords
                .AsNoTracking()
                .Where(r => r.ApplicationUserId == applicationUserId)
                .OrderByDescending(r => r.ExtractedAt)
                .ToListAsync();
        }

        public async Task<List<ExtractedAcademicRecord>> GetAllWithHistoryForUserAsync(string applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(applicationUserId))
            {
                return new List<ExtractedAcademicRecord>();
            }

            return await _context.ExtractedAcademicRecords
                .AsNoTracking()
                .Include(r => r.Subjects)
                .Include(r => r.AcademicPeriods)
                    .ThenInclude(p => p.Subjects)
                .Where(r => r.ApplicationUserId == applicationUserId)
                .OrderBy(r => r.ExtractedAt)
                .ToListAsync();
        }
    }
}
