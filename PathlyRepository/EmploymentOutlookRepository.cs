using Pathly_Data;
using Pathly_Models;
using PathlyInterfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace PathlyRepository
{
    public class EmploymentOutlookRepository : GenericRepository<EmploymentOutlook>, IEmploymentOutlookRepositoryInterface
    {
        public EmploymentOutlookRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
